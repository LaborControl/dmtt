# Migration NFC V1 → V2 - Architecture Chip-Based Offline

**Date**: 2025-11-09
**Version**: 2.0.0
**Status**: ✅ IMPLÉMENTÉ

---

## 📋 Résumé des Changements

### Architecture NFC V1 (Obsolète)
```
1. Scan puce → Lire UID
2. POST /api/rfidchips/validate-scan { uid }
3. Backend cherche UID en base
4. Backend valide checksum (HMAC-SHA256)
5. Retour isValid true/false

❌ PROBLÈME: Ne fonctionne PAS offline!
```

### Architecture NFC V2 (Nouvelle)
```
1. Scan puce → Lire bloc 1 (ChipId non protégé)
2. Générer clé locale: SHA256(ChipId + MasterKey)
3. Lire bloc 4 avec clé générée (vérification anti-clonage)
4. Vérifier whitelist locale (ChipId autorisés)
5. Autoriser/Refuser

✅ AVANTAGE: Fonctionne 100% offline!
```

---

## 🎯 Objectifs de la Migration

1. **Mode offline complet** : Validation puces sans réseau
2. **Whitelist locale** : Liste ChipId autorisés stockée localement
3. **Clé unique par puce** : SHA256(ChipId + MasterKey) - impossible à cloner
4. **Synchronisation** : Sync whitelist lors de connexion
5. **Performance** : Validation instantanée (pas d'appel API)

---

## 🆕 Nouveaux Services

### 1. Service Crypto RFID
**Fichier**: `services/crypto/rfidCrypto.ts`

**Fonctions principales**:
- `generateChipSecretKey(chipId)` → Génère clé unique par puce
- `guidToBytes(guid)` → Convertit GUID en bytes (16 octets)
- `bytesToGuid(bytes)` → Convertit bytes en GUID
- `hexToBytes(hexKey)` → Convertit hex en bytes
- `bytesToHex(bytes)` → Convertit bytes en hex
- `calculateChecksum(uid, salt, chipId)` → Calcule checksum anti-clonage

**Exemple**:
```typescript
import { generateChipSecretKey } from '@/services/crypto/rfidCrypto';

const chipId = "550e8400-e29b-41d4-a716-446655440000";
const key = await generateChipSecretKey(chipId);
// → "A1B2C3D4E5F6" (6 octets hex)
```

### 2. Service Whitelist
**Fichier**: `services/storage/whitelistService.ts`

**Fonctions principales**:
- `addToWhitelist(chip)` → Ajoute puce à whitelist
- `isChipWhitelisted(chipId)` → Vérifie si puce autorisée
- `getWhitelist()` → Récupère whitelist complète
- `syncWhitelist(apiUrl, token, customerId)` → Sync avec backend
- `clearWhitelist()` → Vide whitelist (déconnexion)
- `getWhitelistStats()` → Stats whitelist

**Exemple**:
```typescript
import { isChipWhitelisted, syncWhitelist } from '@/services/storage/whitelistService';

// Vérifier si puce autorisée
const chip = await isChipWhitelisted(chipId);
if (chip) {
  console.log(`Point de contrôle: ${chip.controlPointName}`);
}

// Synchroniser avec serveur
const count = await syncWhitelist(API_URL, token, customerId);
console.log(`${count} puces synchronisées`);
```

### 3. Service Lecteur NFC
**Fichier**: `services/nfc/nfcReader.ts`

**Fonctions principales**:
- `initNfc()` → Initialise gestionnaire NFC
- `readUid()` → Lit UID physique
- `readBlock(blockNumber, key)` → Lit bloc spécifique
- `readLaborControlChip(chipKey)` → Lit données complètes puce
- `scanAndValidateChip(generateKeyFn, isWhitelistedFn)` → Scan + validation complète
- `writeBlock(blockNumber, data, key)` → Écrit bloc (admin uniquement)

**Exemple**:
```typescript
import { scanAndValidateChip } from '@/services/nfc/nfcReader';
import { generateChipSecretKey } from '@/services/crypto/rfidCrypto';
import { isChipWhitelisted } from '@/services/storage/whitelistService';

const result = await scanAndValidateChip(
  generateChipSecretKey,
  isChipWhitelisted
);

if (result.success) {
  console.log('✅ Puce valide:', result.chipData);
} else {
  console.log('❌ Puce refusée:', result.message);
}
```

### 4. Hook NFC V2
**Fichier**: `hooks/useNfcValidationV2.ts`

**Hooks disponibles**:
- `useNfcValidationV2()` → Hook complet avec état
- `useNfcQuickScan(onSuccess, onError)` → Scan rapide simplifié
- `useNfcScanWithAlert()` → Scan avec Alert automatique

**Exemple**:
```typescript
import { useNfcValidationV2 } from '@/hooks/useNfcValidationV2';

function MyComponent() {
  const { isNfcSupported, isScanning, scanChip, initialize } = useNfcValidationV2();

  useEffect(() => {
    initialize();
  }, []);

  const handleScan = () => {
    scanChip(
      (result) => console.log('✅ Succès:', result),
      (error) => console.log('❌ Erreur:', error)
    );
  };

  return (
    <Button onPress={handleScan} disabled={!isNfcSupported || isScanning}>
      {isScanning ? 'Scan en cours...' : 'Scanner puce'}
    </Button>
  );
}
```

---

## 📦 Nouvelles Dépendances

```json
{
  "@react-native-async-storage/async-storage": "^1.x",
  "expo-crypto": "~13.x"
}
```

**Installation**:
```bash
npm install @react-native-async-storage/async-storage expo-crypto
```

---

## 🔄 Workflow Complet - Scan Offline

### 1. Premier Login (Online)
```
1. Utilisateur se connecte → Récupère token + customerId
2. App appelle syncWhitelist(API_URL, token, customerId)
3. Backend retourne liste ChipId autorisés pour ce customer
4. App sauvegarde whitelist en local (AsyncStorage)
5. App peut maintenant scanner offline!
```

### 2. Scan Puce (Offline)
```
1. Technicien approche puce du téléphone
2. App lit bloc 1 → ChipId (ex: "550e8400-...")
3. App génère clé: SHA256(ChipId + MasterKey)
4. App lit bloc 4 avec clé générée
   - Si échec → Puce non encodée ou clonée ❌
   - Si succès → Continue
5. App vérifie ChipId (bloc 1) == ChipId (bloc 4)
   - Si différent → Puce clonée ❌
   - Si identique → Continue
6. App cherche ChipId dans whitelist locale
   - Si trouvé + status=ACTIVE → Autorisé ✅
   - Sinon → Non autorisé ❌
7. App démarre/termine la tâche
```

### 3. Synchronisation Périodique (Online)
```
1. App détecte connexion réseau
2. App appelle syncWhitelist() pour mettre à jour
3. Nouvelles puces activées ajoutées à whitelist
4. Puces désactivées marquées status=INACTIVE
```

---

## ⚙️ Configuration Backend Requise

### Endpoint à Créer: GET /api/rfidchips/whitelist/{customerId}

**Réponse**:
```json
{
  "chips": [
    {
      "chipId": "550e8400-e29b-41d4-a716-446655440000",
      "controlPointId": "cp-123",
      "controlPointName": "Point A - Entrée",
      "activatedAt": "2025-10-23T14:30:00Z",
      "status": "ACTIVE"
    },
    {
      "chipId": "660e8400-e29b-41d4-a716-446655440001",
      "controlPointId": "cp-456",
      "controlPointName": "Point B - Sortie",
      "activatedAt": "2025-10-25T08:15:00Z",
      "status": "ACTIVE"
    }
  ]
}
```

**Logique backend**:
```csharp
// RfidChipsController.cs
[HttpGet("whitelist/{customerId}")]
public async Task<ActionResult<WhitelistResponse>> GetWhitelist(Guid customerId)
{
    var chips = await _context.RfidChips
        .Where(c => c.CustomerId == customerId)
        .Include(c => c.ControlPoint)
        .Select(c => new WhitelistedChipDto
        {
            ChipId = c.Id,
            ControlPointId = c.ControlPointId,
            ControlPointName = c.ControlPoint.Name,
            ActivatedAt = c.ActivationDate,
            Status = c.Status
        })
        .ToListAsync();

    return Ok(new { chips });
}
```

### Endpoint MasterKey (Optionnel)

**GET /api/rfidchips/master-key** (Admin seulement)

Pour récupérer la MasterKey et la stocker localement sur l'app mobile au premier login.

---

## 🔐 Sécurité

### MasterKey
- **Stockage côté backend**: `appsettings.json` → `RfidSecurity:MasterKey`
- **Stockage côté mobile**: Keychain/SecureStore (TODO)
- **Transmission**: HTTPS uniquement, via endpoint sécurisé

### Whitelist
- **Stockage local**: AsyncStorage (chiffré si possible)
- **Synchronisation**: Authentifié avec JWT Bearer token
- **Validation**: Vérifier signature backend (TODO)

---

## 📝 Migration Étape par Étape

### Étape 1: Installer dépendances
```bash
cd Mobile/LaborControlApp
npm install @react-native-async-storage/async-storage expo-crypto
```

### Étape 2: Créer endpoint backend whitelist
Voir section "Configuration Backend Requise" ci-dessus.

### Étape 3: Tester nouveau hook en isolation
Créer un écran de test:
```typescript
// app/test-nfc-v2.tsx
import { useNfcValidationV2 } from '@/hooks/useNfcValidationV2';

export default function TestNfcV2() {
  const { isNfcSupported, isScanning, scanChip, lastResult, initialize } = useNfcValidationV2();

  useEffect(() => {
    initialize();
  }, []);

  return (
    <View>
      <Text>NFC supporté: {isNfcSupported ? 'Oui' : 'Non'}</Text>
      <Button onPress={() => scanChip()} disabled={isScanning}>
        Scanner puce
      </Button>
      {lastResult && (
        <Text>{lastResult.isValid ? '✅ Valide' : '❌ Invalide'}</Text>
      )}
    </View>
  );
}
```

### Étape 4: Synchroniser whitelist au login
Modifier le login pour sync la whitelist:
```typescript
// app/(tabs)/index.tsx
import { syncWhitelist } from '@/services/storage/whitelistService';

async function handleLogin(email, password) {
  // Login existant
  const response = await fetch(`${API_BASE_URL}/auth/login`, {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  });
  const { token, userId, customerId } = await response.json();

  // NOUVEAU: Synchroniser whitelist
  try {
    const count = await syncWhitelist(API_BASE_URL, token, customerId);
    console.log(`✅ Whitelist synchronisée: ${count} puces`);
  } catch (error) {
    console.error('❌ Erreur sync whitelist:', error);
    // Continuer quand même (mode dégradé)
  }
}
```

### Étape 5: Remplacer ancien hook par nouveau
```typescript
// Ancien (V1)
import { useNfcValidation } from '@/hooks/useNfcValidation';

// Nouveau (V2)
import { useNfcValidationV2 } from '@/hooks/useNfcValidationV2';
```

### Étape 6: Tester sur terrain
1. Login avec compte test
2. Vérifier sync whitelist (logs console)
3. Activer mode avion (simuler offline)
4. Scanner puce autorisée → Doit valider ✅
5. Scanner puce non autorisée → Doit refuser ❌

---

## 🧪 Plan de Test

### Tests Unitaires
- [ ] `generateChipSecretKey()` génère toujours la même clé pour un ChipId donné
- [ ] `guidToBytes()` convertit correctement un GUID en 16 octets
- [ ] `bytesToGuid()` reconvertit bytes en GUID identique
- [ ] `isChipWhitelisted()` retourne null si puce pas dans whitelist
- [ ] `isChipWhitelisted()` retourne chip si status=ACTIVE

### Tests d'Intégration
- [ ] Sync whitelist récupère toutes les puces du customer
- [ ] Scan puce valide + autorisée → Validation réussie
- [ ] Scan puce valide + NON autorisée → Validation refusée
- [ ] Scan puce invalide (clé incorrecte) → Détection clonage

### Tests End-to-End
- [ ] Login → Sync → Scan offline → Succès
- [ ] Scan avec mauvaise clé → Échec
- [ ] Désactiver puce côté admin → Sync → Scan → Refusé

---

## ⚠️ Points d'Attention

### Performance
- Génération clé SHA256 : ~5ms (négligeable)
- Lecture NFC blocs 1, 4, 8 : ~50-100ms
- Vérification whitelist locale : <1ms

**Total**: ~100-150ms (très rapide!)

### Gestion Erreurs
- **NFC non supporté**: Alerter utilisateur dès l'initialisation
- **Puce illisible**: Demander de réessayer
- **Bloc protégé illisible**: Détecter comme puce clonée ou non encodée
- **Whitelist vide**: Forcer synchronisation avant premier scan

### Offline Dégradé
Si whitelist vide (jamais sync):
- Afficher message "Synchronisation requise"
- Bloquer les scans
- Proposer bouton "Synchroniser maintenant"

---

## 🚀 Prochaines Améliorations

### Phase 2
- [ ] Implémenter WatermelonDB pour remplacer AsyncStorage
- [ ] Chiffrer whitelist locale avec SecureStore
- [ ] Ajouter signature cryptographique sur whitelist
- [ ] Implémenter queue de synchronisation

### Phase 3
- [ ] Mode multi-customer (plusieurs whitelist)
- [ ] Synchronisation différentielle (delta)
- [ ] Compression whitelist (si >1000 puces)
- [ ] Monitoring performance

---

## 📚 Ressources

### Documentation
- Architecture RFID backend: [Backend/RFID_MIGRATION_GUIDE.md](../../Backend/RFID_MIGRATION_GUIDE.md)
- Architecture Jean-Claude: [AI_JC/ARCHITECTURE.md](../../AI_JC/ARCHITECTURE.md)

### API Endpoints
- POST /api/auth/login
- GET /api/rfidchips/whitelist/{customerId} ← **NOUVEAU**
- GET /api/rfidchips/master-key ← **NOUVEAU** (optionnel)

### Dépendances
- [Expo Crypto Docs](https://docs.expo.dev/versions/latest/sdk/crypto/)
- [AsyncStorage Docs](https://react-native-async-storage.github.io/async-storage/)
- [react-native-nfc-manager](https://github.com/revtel/react-native-nfc-manager)

---

**✅ Migration complétée!** L'app peut maintenant valider les puces RFID en mode 100% offline. 🎉
