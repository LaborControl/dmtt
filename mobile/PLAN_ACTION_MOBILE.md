# 📱 PLAN D'ACTION - APPLICATION MOBILE LABOR CONTROL

**Date**: 21 novembre 2025
**Statut**: Audit complet effectué - Actions à prioriser
**Objectif**: Mettre l'application mobile en adéquation avec le backend API et le frontend web

---

## 🎯 RÉSUMÉ EXÉCUTIF

L'audit complet du backend API et du frontend web Blazor révèle que l'application mobile nécessite **des corrections importantes** pour être alignée avec l'architecture métier :

### ❌ Problèmes Identifiés
1. **Interface USER** contient fonctionnalités réservées admin (historique corrigé)
2. **Pas de déclaration d'anomalie** pour les utilisateurs
3. **Pas de réaffectation de tâches** pour superviseurs
4. **Formulaires statiques** au lieu de formulaires dynamiques JSON
5. **Pas de mode offline** alors que c'est critique terrain
6. **Pas de synchronisation whitelist** puces RFID

### ✅ Points Forts Actuels
- Architecture par rôles (`(tabs)`, `(supervisor)`, `(admin)`) ✅
- Authentification JWT fonctionnelle ✅
- Intégration NFC basique ✅
- Page sélection de rôle corrigée ✅

---

## 📊 ARCHITECTURE CIBLE

### Rôles et Fonctionnalités

#### 🟢 USER (Technicien)
**Interface**: `app/(tabs)/`

**Fonctionnalités OBLIGATOIRES**:
- ✅ Connexion/déconnexion
- ✅ Liste tâches assignées
- ✅ Scanner NFC pour exécuter tâche
- ❌ **MANQUANT**: Formulaires dynamiques JSON (protocoles)
- ❌ **MANQUANT**: Déclaration anomalie (scan libre)
- ✅ Prise de photos
- ❌ **MANQUANT**: Double bornage NFC (2 scans)
- ✅ Historique personnel
- ❌ **MANQUANT**: Mode offline + sync

**Fonctionnalités INTERDITES**:
- ❌ Enregistrement puces RFID (admin only) - **DÉJÀ CORRIGÉ** ✅
- ❌ Création équipements
- ❌ Gestion personnel

---

#### 🟡 SUPERVISOR
**Interface**: `app/(supervisor)/`

**Fonctionnalités USER +**:
- ❌ **MANQUANT**: Vue équipe (liste tâches toute l'équipe)
- ❌ **MANQUANT**: Réaffectation tâches entre techniciens
- ❌ **MANQUANT**: Intercepter tâches en retard
- ❌ **MANQUANT**: Validation interventions (flags anti-fraude)
- ❌ **MANQUANT**: Statistiques équipe (taux complétion, retards)

---

#### 🔵 ADMIN
**Interface**: `app/(admin)/`

**Fonctionnalités SUPERVISOR +**:
- ✅ Création équipements (écran existe)
- ✅ Gestion points de contrôle (écran existe)
- ✅ Affectation puces (écran existe)
- ✅ Enregistrement puces RFID (écran existe)
- ✅ Chronos (écran existe)

---

## 🚨 CORRECTIONS PRIORITAIRES

### PHASE 1 - CORRECTIONS CRITIQUES (1-2 jours)

#### ✅ 1.1. Interface USER - Déjà corrigé
- Bouton "Enregistrer puces" retiré de `(tabs)/_layout.tsx`
- ✅ **TERMINÉ**

#### 🔴 1.2. Déclaration Anomalie (USER) - **CRITIQUE**
**Problème**: Users ne peuvent pas signaler anomalies

**Solution**:
1. Ajouter onglet "Anomalie" dans `(tabs)/_layout.tsx`
2. Créer `app/(tabs)/anomaly.tsx`
3. Workflow:
   - Bouton "Déclarer une anomalie"
   - Scan NFC libre (pas de tâche associée)
   - Formulaire simple: Type anomalie + Description + Photos
   - POST `/api/anomalies` (endpoint à créer backend si n'existe pas)

**Code suggéré**:
```tsx
// app/(tabs)/anomaly.tsx
export default function AnomalyScreen() {
  const handleScanForAnomaly = async () => {
    const uid = await scanNFC();
    // Valider puce
    const chip = await validateChip(uid);
    // Afficher formulaire
    showAnomalyForm(chip.controlPointId);
  };

  return (
    <View>
      <Button onPress={handleScanForAnomaly}>
        Scanner pour signaler une anomalie
      </Button>
    </View>
  );
}
```

---

#### 🔴 1.3. Formulaires Dynamiques JSON - **CRITIQUE**
**Problème**: Formulaires codés en dur, pas adaptables aux protocoles client

**Solution**:
1. Endpoint backend: GET `/api/tasktemplates/{id}`
2. Parser `FormTemplate` JSON
3. Générer UI dynamiquement

**Code suggéré**:
```tsx
// components/DynamicForm.tsx
interface FormField {
  name: string;
  type: 'text' | 'number' | 'boolean' | 'select' | 'photo';
  label: string;
  required?: boolean;
  min?: number;
  max?: number;
  options?: string[];
}

export function DynamicForm({formTemplateJson}: {formTemplateJson: string}) {
  const template = JSON.parse(formTemplateJson);
  const [formData, setFormData] = useState({});

  return (
    <ScrollView>
      {template.fields.map((field: FormField) => {
        switch (field.type) {
          case 'number':
            return <NumberInput
              key={field.name}
              label={field.label}
              required={field.required}
              min={field.min}
              max={field.max}
              onChange={(v) => setFormData({...formData, [field.name]: v})}
            />;
          case 'boolean':
            return <Checkbox key={field.name} />;
          case 'select':
            return <Picker key={field.name} items={field.options} />;
          case 'photo':
            return <CameraButton key={field.name} />;
          default:
            return <TextInput key={field.name} />;
        }
      })}
    </ScrollView>
  );
}
```

**Intégration**:
```tsx
// Dans (tabs)/index.tsx
const handleScan = async (uid: string) => {
  // 1. Valider puce
  const validation = await api.post('/rfidchips/validate-scan', {uid});

  // 2. Récupérer protocole
  const task = tasks.find(t => t.controlPointId === validation.controlPointId);
  const protocol = await api.get(`/tasktemplates/${task.taskTemplateId}`);

  // 3. Afficher formulaire dynamique
  setCurrentProtocol(protocol.formTemplate);
  setShowForm(true);
};
```

---

#### 🔴 1.4. Double Bornage NFC - **IMPORTANT**
**Problème**: Sécurité insuffisante pour tâches sensibles (EHPAD, maintenance critique)

**Solution**:
1. Vérifier `task.requireDoubleScan`
2. Si true:
   - 1er scan → POST `/taskexecutions/first-scan` → ouvre tâche
   - Remplissage formulaire (min 30s, max 2h)
   - 2nd scan → POST `/taskexecutions/second-scan` → valide
3. Afficher timer visuel entre scans

**Code suggéré**:
```tsx
const handleDoubleScan = async (uid: string, isFirstScan: boolean) => {
  if (isFirstScan) {
    // Premier scan
    const {executionId, firstScanAt} = await api.post('/taskexecutions/first-scan', {
      userId, controlPointId, scheduledTaskId, firstScanAt: new Date()
    });

    // Stocker localement
    await AsyncStorage.setItem('currentExecutionId', executionId);
    await AsyncStorage.setItem('firstScanAt', firstScanAt);

    // Afficher formulaire + timer
    setShowFormWithTimer(true);
    startTimer(firstScanAt);

  } else {
    // Second scan
    const executionId = await AsyncStorage.getItem('currentExecutionId');
    const firstScanAt = await AsyncStorage.getItem('firstScanAt');

    // Vérifier intervalle temps
    const elapsed = Date.now() - new Date(firstScanAt).getTime();
    if (elapsed < 30000) {
      Alert.alert('Erreur', 'Temps minimum: 30 secondes');
      return;
    }

    // Soumettre
    await api.post('/taskexecutions/second-scan', {
      executionId,
      secondScanAt: new Date(),
      formData: collectFormData(),
      photoUrl
    });

    // Nettoyer
    await AsyncStorage.removeItem('currentExecutionId');
    await AsyncStorage.removeItem('firstScanAt');

    Alert.alert('Succès', 'Tâche validée');
  }
};
```

---

### PHASE 2 - FONCTIONNALITÉS SUPERVISOR (2-3 jours)

#### 🟡 2.1. Vue Équipe
**Écran**: `app/(supervisor)/team.tsx`

**Fonctionnalités**:
- Liste tâches de toute l'équipe (pas seulement miennes)
- Filtres: technicien, statut (PENDING/COMPLETED/OVERDUE)
- Indicateurs: taux complétion, nb retards

**Endpoint**: GET `/api/scheduledtasks?teamId={teamId}&status=PENDING`

---

#### 🟡 2.2. Réaffectation Tâches
**Interface**: Dans détail tâche, bouton "Réaffecter"

**Workflow**:
1. Superviseur ouvre tâche d'un technicien
2. Bouton "Réaffecter à..."
3. Liste techniciens qualifiés (avec même qualifications requises)
4. Confirmation
5. PUT `/api/scheduledtasks/{id}/reassign` `{newUserId}`

**Code suggéré**:
```tsx
// Dans TaskDetail.tsx (supervisor)
const handleReassign = async () => {
  // 1. Récupérer techniciens qualifiés
  const qualifiedUsers = await api.get(`/users/qualified-for-task/${taskId}`);

  // 2. Afficher modal sélection
  setShowReassignModal(true);
  setQualifiedUsers(qualifiedUsers);
};

const confirmReassign = async (newUserId: string) => {
  await api.put(`/scheduledtasks/${taskId}/reassign`, {newUserId});
  Alert.alert('Succès', 'Tâche réaffectée');
  refreshTasks();
};
```

---

#### 🟡 2.3. Interception Tâches en Retard
**Écran**: `app/(supervisor)/intercept.tsx`

**Fonctionnalités**:
- Liste tâches OVERDUE de l'équipe
- Bouton "Prendre en charge" (s'auto-affecter)
- Bouton "Réaffecter à..."

**Endpoint**: GET `/api/scheduledtasks?teamId={teamId}&status=OVERDUE`

---

### PHASE 3 - MODE OFFLINE (3-4 jours)

#### 🔵 3.1. Whitelist Puces Offline
**Problème**: Validation NFC nécessite réseau → bloquant terrain sans connexion

**Solution**:
1. Au login, télécharger whitelist:
   - GET `/api/rfidchips/whitelist/{customerId}`
   - Retourne: `[{uid, chipId, controlPointId, checksum}]`
2. Stocker dans AsyncStorage
3. Validation locale:
```tsx
const validateChipOffline = (uid: string) => {
  const chip = whitelist.find(c => c.uid === uid);
  if (!chip) return {valid: false, error: 'Puce non autorisée'};

  // Vérifier checksum (anti-clonage)
  const expectedChecksum = computeHMAC(uid, chip.salt, chip.chipId);
  if (chip.checksum !== expectedChecksum) {
    return {valid: false, error: 'Puce clonée détectée'};
  }

  return {valid: true, controlPointId: chip.controlPointId};
};
```

---

#### 🔵 3.2. Queue Synchronisation
**Problème**: Interventions terrain perdues si pas de réseau

**Solution**:
1. File d'attente locale (SQLite ou AsyncStorage JSON)
2. Quand offline: stocker exécution localement
3. Quand online revient: sync automatique

**Code suggéré**:
```tsx
// services/syncQueue.ts
export const queueExecution = async (execution: TaskExecution) => {
  const queue = await AsyncStorage.getItem('syncQueue') || '[]';
  const items = JSON.parse(queue);
  items.push({...execution, queuedAt: Date.now()});
  await AsyncStorage.setItem('syncQueue', JSON.stringify(items));
};

export const syncQueue = async () => {
  const queue = await AsyncStorage.getItem('syncQueue') || '[]';
  const items = JSON.parse(queue);

  for (const item of items) {
    try {
      await api.post('/taskexecutions', item);
      // Retirer de la queue
      items.splice(items.indexOf(item), 1);
    } catch (error) {
      console.error('Sync failed for item', item.id);
    }
  }

  await AsyncStorage.setItem('syncQueue', JSON.stringify(items));
};

// Dans App.tsx
useEffect(() => {
  const unsubscribe = NetInfo.addEventListener(state => {
    if (state.isConnected) {
      syncQueue(); // Auto-sync quand connexion revient
    }
  });
  return unsubscribe;
}, []);
```

---

#### 🔵 3.3. Indicateurs Offline
**UI**: Badge visible montrant statut sync

```tsx
// components/SyncStatus.tsx
export function SyncStatus() {
  const [pendingCount, setPendingCount] = useState(0);
  const [isOnline, setIsOnline] = useState(true);

  return (
    <View style={styles.badge}>
      {isOnline ? (
        <Text>✅ En ligne</Text>
      ) : (
        <Text>⚠️ Hors ligne ({pendingCount} en attente)</Text>
      )}
    </View>
  );
}
```

---

## 📝 FICHIERS À MODIFIER/CRÉER

### Modifications

1. **app/(tabs)/_layout.tsx**
   - ✅ Déjà corrigé (pas de "Enregistrer puces")
   - ➕ Ajouter onglet "Anomalie"

2. **app/(tabs)/index.tsx**
   - 🔄 Refactoriser: extraire formulaire statique
   - ➕ Intégrer `DynamicForm` component
   - ➕ Intégrer double bornage
   - ➕ Intégrer validation offline

3. **contexts/AuthContext.tsx**
   - ➕ Télécharger whitelist au login
   - ➕ Stocker dans state + AsyncStorage

### Créations

1. **app/(tabs)/anomaly.tsx** - Déclaration anomalie
2. **components/DynamicForm.tsx** - Formulaires JSON
3. **components/SyncStatus.tsx** - Indicateur offline
4. **services/syncQueue.ts** - Queue synchronisation
5. **services/offlineValidation.ts** - Validation NFC offline
6. **app/(supervisor)/team.tsx** - Vue équipe
7. **app/(supervisor)/intercept.tsx** - Tâches en retard
8. **app/(supervisor)/reassign.tsx** - Réaffectation

---

## 🎯 PRIORITÉS RECOMMANDÉES

### 🔥 URGENT (Cette semaine)
1. ✅ Corriger interface USER (déjà fait)
2. ❗ Formulaires dynamiques JSON
3. ❗ Double bornage NFC
4. ❗ Déclaration anomalie

### 📅 IMPORTANT (Semaine prochaine)
5. Mode offline (whitelist + queue)
6. Réaffectation tâches (supervisor)
7. Vue équipe (supervisor)

### 🔜 SOUHAITABLE (Plus tard)
8. Notifications push
9. Analytics/statistiques
10. Export données local

---

## 🧪 TESTS À EFFECTUER

### Tests Fonctionnels
- [ ] USER peut scanner et exécuter tâche avec formulaire dynamique
- [ ] USER peut déclarer anomalie par scan libre
- [ ] Double bornage fonctionne (2 scans, timer, validation temps)
- [ ] Mode offline: scan fonctionne sans réseau
- [ ] Mode offline: sync automatique au retour connexion
- [ ] SUPERVISOR peut voir tâches équipe
- [ ] SUPERVISOR peut réaffecter tâche
- [ ] ADMIN peut enregistrer puces (déjà testé)

### Tests Sécurité
- [ ] Validation checksum HMAC (anti-clonage)
- [ ] Whitelist: puce non autorisée rejetée
- [ ] Double bornage: <30s rejeté
- [ ] Double bornage: >2h alerte
- [ ] Token JWT expiré → déconnexion
- [ ] Isolation multi-tenant (CustomerId)

---

## 📞 ENDPOINTS BACKEND REQUIS

### Existants (OK)
- ✅ POST `/api/auth/login`
- ✅ GET `/api/scheduledtasks/user/{userId}`
- ✅ POST `/api/taskexecutions`
- ✅ POST `/api/taskexecutions/first-scan`
- ✅ POST `/api/taskexecutions/second-scan`
- ✅ POST `/api/rfidchips/validate-scan`
- ✅ GET `/api/rfidchips/whitelist/{customerId}`
- ✅ GET `/api/tasktemplates/{id}`
- ✅ GET `/api/controlpoints`

### À Créer Backend
- ❌ POST `/api/anomalies` - Déclaration anomalie
- ❌ PUT `/api/scheduledtasks/{id}/reassign` - Réaffectation
- ❌ GET `/api/users/qualified-for-task/{taskId}` - Techniciens qualifiés
- ❌ GET `/api/scheduledtasks?teamId={teamId}&status=OVERDUE` - Tâches retard équipe

---

## 📚 RESSOURCES

### Documentation
- Audit Backend: Voir rapport complet
- Audit Frontend: Voir rapport complet
- Décisions Continuity: 14 décisions logged

### Exemples Code
- Formulaire EHPAD: Voir audit backend section "Protocoles métier"
- Double bornage: Voir workflow détaillé
- Anti-clonage: Voir cycle de vie RFID

---

## ✅ CHECKLIST AVANT DÉPLOIEMENT

- [ ] Tests fonctionnels passés
- [ ] Tests sécurité passés
- [ ] Mode offline testé (avion mode)
- [ ] Double bornage testé
- [ ] Formulaires dynamiques testés (3+ protocoles différents)
- [ ] Build APK réussi (GitHub Actions)
- [ ] APK testé sur 2+ devices Android
- [ ] Documentation utilisateur mise à jour
- [ ] Changelog créé

---

**Auteur**: Claude Code
**Dernière mise à jour**: 21 novembre 2025
**Version**: 1.0
