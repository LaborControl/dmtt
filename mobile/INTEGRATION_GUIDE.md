# 📱 Guide d'intégration - Validation NFC RFID

## 🎯 Objectif

Intégrer la validation de puces RFID NTAG 213 dans l'application mobile React Native avec sécurité anti-clonage.

---

## 📦 Installation des dépendances

```bash
cd Mobile/LaborControlApp

# Installer react-native-nfc-manager
npm install react-native-nfc-manager

# Installer axios (si pas déjà installé)
npm install axios
```

### Configuration Android

Ajouter les permissions dans `android/app/src/main/AndroidManifest.xml` :

```xml
<uses-permission android:name="android.permission.NFC" />
<uses-feature android:name="android.hardware.nfc" android:required="false" />
```

### Configuration iOS

Ajouter dans `ios/Podfile` :

```ruby
pod 'react-native-nfc-manager', :path => '../node_modules/react-native-nfc-manager'
```

Puis :
```bash
cd ios && pod install && cd ..
```

Ajouter les clés dans `ios/LaborControlApp/Info.plist` :

```xml
<key>NFCReaderUsageDescription</key>
<string>Nous avons besoin d'accéder au NFC pour valider les puces RFID</string>
<key>com.apple.developer.nfc.readersession.formats</key>
<array>
  <string>NDEF</string>
  <string>TAG</string>
</array>
```

---

## 🔧 Utilisation dans les composants

### Option 1 : Hook personnalisé

```typescript
// Dans un composant
import { useNfcScan } from '../hooks/useNfcValidation';

export const MyComponent = () => {
  const { isScanning, error, scanAndValidate, clearError } = useNfcScan(
    'http://localhost:5278',
    'YOUR_JWT_TOKEN'
  );

  const handleScan = async () => {
    const result = await scanAndValidate();

    if (result?.isValid) {
      console.log('✅ Puce valide:', result.chipId);
      // Enregistrer la tâche, etc.
    } else {
      console.error('❌ Puce invalide:', result?.message);
    }
  };

  return (
    <TouchableOpacity onPress={handleScan} disabled={isScanning}>
      <Text>{isScanning ? 'Lecture...' : 'Scaner'}</Text>
    </TouchableOpacity>
  );
};
```

### Option 2 : Composant complet

```typescript
// Dans une page
import { NfcValidationComponent } from '../components/NfcValidationComponent';

export const TaskPage = () => {
  const handleValidationSuccess = (chipId: string) => {
    console.log('Puce validée:', chipId);
    // Enregistrer la tâche avec cette puce
  };

  const handleValidationError = (message: string) => {
    console.error('Erreur validation:', message);
  };

  return (
    <NfcValidationComponent
      apiUrl="http://localhost:5278"
      token="YOUR_JWT_TOKEN"
      onValidationSuccess={handleValidationSuccess}
      onValidationError={handleValidationError}
    />
  );
};
```

---

## 🔐 Flux de validation complet

```
1. Utilisateur appuie sur "Scaner"
   ↓
2. Hook useNfcScan lance scanAndValidate()
   ↓
3. readChip() lit la puce NFC
   - Récupère l'UID (pages 0-2)
   - Récupère le checksum (pages 6-7)
   ↓
4. validateChip() appelle l'API Backend
   - POST /api/rfidchips/validate-scan
   - Envoie l'UID
   ↓
5. Backend valide
   - Cherche l'UID en BD
   - Récupère le Salt
   - Recalcule HMAC-SHA256
   - Compare avec le checksum
   ↓
6. Résultat retourné
   - isValid: true/false
   - chipId: "LC-2025-10-00042"
   - message: "Puce authentique" ou "Puce non autorisée"
   ↓
7. Callback onValidationSuccess/Error
```

---

## 📊 Structure des données

### Données lues de la puce

```typescript
interface NfcChipData {
  uid: string;           // Ex: "04A1B2C3D4E5F6"
  checksum: string;      // Ex: "XyZ9k4P2mN7qW1rT"
  systemId?: string;     // Ex: "LC:2025-10-23"
}
```

### Résultat de validation

```typescript
interface ValidationResult {
  isValid: boolean;           // true si puce authentique
  chipId?: string;            // Ex: "LC-2025-10-00042"
  message: string;            // Message de statut
  controlPointId?: string;    // ID du point de contrôle
}
```

---

## 🐛 Dépannage

### Erreur : "NFC not available"

```typescript
// Vérifier la disponibilité NFC
import NfcManager from 'react-native-nfc-manager';

const checkNfc = async () => {
  const isSupported = await NfcManager.isSupported();
  if (!isSupported) {
    console.error('NFC non supporté sur cet appareil');
  }
};
```

### Erreur : "Permission denied"

```typescript
// Demander les permissions
import { PermissionsAndroid } from 'react-native';

const requestNfcPermission = async () => {
  try {
    const granted = await PermissionsAndroid.request(
      PermissionsAndroid.PERMISSIONS.NFC,
      {
        title: 'Permission NFC',
        message: 'Nous avons besoin d\'accéder au NFC',
        buttonNeutral: 'Plus tard',
        buttonNegative: 'Refuser',
        buttonPositive: 'Accepter',
      }
    );
    return granted === PermissionsAndroid.RESULTS.GRANTED;
  } catch (err) {
    console.error('Erreur permission:', err);
    return false;
  }
};
```

### Erreur : "UID non trouvé"

```typescript
// Vérifier que la puce est bien NTAG 213
// et qu'elle est correctement encodée

// Essayer de lire manuellement
const tag = await NfcManager.getTag();
console.log('Tag complet:', JSON.stringify(tag, null, 2));
```

### Erreur : "Puce non autorisée"

```typescript
// Vérifier que :
// 1. La puce a été encodée avec register-chip.ps1
// 2. L'UID est correct
// 3. Le statut en BD est "ACTIVE"
// 4. Le CustomerId correspond
```

---

## 🔄 Intégration avec le flux existant

### Exemple : Enregistrement de tâche

```typescript
import { NfcValidationComponent } from '../components/NfcValidationComponent';
import { useTaskContext } from '../context/TaskContext';

export const TaskRegistrationPage = () => {
  const { createTaskExecution } = useTaskContext();

  const handleValidationSuccess = async (chipId: string) => {
    // Créer l'exécution de tâche
    await createTaskExecution({
      chipId,
      timestamp: new Date(),
      status: 'COMPLETED',
    });

    // Afficher un message de succès
    Alert.alert('✅ Succès', 'Tâche enregistrée');
  };

  return (
    <NfcValidationComponent
      apiUrl={API_URL}
      token={authToken}
      onValidationSuccess={handleValidationSuccess}
      onValidationError={(msg) => Alert.alert('❌ Erreur', msg)}
    />
  );
};
```

---

## 📝 Checklist d'intégration

- [ ] Dépendances installées (react-native-nfc-manager, axios)
- [ ] Permissions Android configurées
- [ ] Permissions iOS configurées
- [ ] Hook useNfcValidation importé
- [ ] Composant NfcValidationComponent importé
- [ ] API URL configurée
- [ ] JWT Token disponible
- [ ] Callbacks onValidationSuccess/Error implémentés
- [ ] Tests manuels réussis
- [ ] Gestion des erreurs implémentée
- [ ] Historique des scans affiché
- [ ] Intégration avec le flux existant complétée

---

## 🚀 Prochaines étapes

1. ✅ Intégrer le hook dans vos pages
2. ✅ Tester avec une vraie puce encodée
3. ✅ Gérer les cas d'erreur
4. ✅ Afficher les résultats à l'utilisateur
5. ✅ Enregistrer les données en BD
6. ✅ Déployer en production

---

## 📞 Support

Pour toute question :
1. Vérifier les logs console
2. Consulter la section [Dépannage](#dépannage)
3. Vérifier que la puce est bien encodée
4. Vérifier la connexion API

---

**Version** : 1.0.0
**Date** : 2025-10-23
**Auteur** : Labor Control Team
