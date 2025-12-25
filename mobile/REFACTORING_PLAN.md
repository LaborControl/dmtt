# PLAN DE REFACTORING - Mobile App 🚀

**Date**: 2025-01-22
**Objectif**: Repartir sur des bases solides avant Phase 2 (SUPERVISOR)
**Durée estimée**: 2-3 semaines (3 sprints)

---

## 📋 Vue d'ensemble

### Problème actuel
L'application mobile a accumulé une dette technique importante :
- Fichier monstre de 1821 lignes (index.tsx)
- Pas de state management
- apiService.ts créé mais non utilisé
- DynamicForm créé mais non intégré
- JWT refresh non implémenté
- ~2000 lignes de code mort

### Objectif
- Base propre et maintenable
- Architecture modulaire et testable
- Prêt pour évolutions futures (Phase 2, 3, 4...)
- **Synchronisation systématique avec backend**

---

## 🎯 SPRINT 1 - Fondations (1 semaine)

### Objectif
Nettoyer la dette technique critique et poser des bases saines

---

### 1.1 - Refactoriser index.tsx (1821 lignes → architecture modulaire)

**Durée**: 3-4 jours
**Priorité**: P0 - CRITIQUE

#### Analyse du monstre actuel
[app/(tabs)/index.tsx](app/(tabs)/index.tsx) contient TOUT :
- 27+ useState hooks
- Logique d'authentification
- Gestion NFC
- Formulaires
- Liste des tâches
- Historique
- API calls
- UI rendering

#### Architecture cible

```
app/
├── (user)/
│   ├── _layout.tsx                   (Navigation USER)
│   ├── tasks/
│   │   ├── index.tsx                 (Liste des tâches - 200 lignes)
│   │   ├── [id].tsx                  (Détail tâche - 150 lignes)
│   │   └── execute.tsx               (Exécution avec NFC - 250 lignes)
│   ├── anomaly.tsx                   (Déclaration anomalie - EXISTANT)
│   ├── history.tsx                   (Historique - 200 lignes)
│   └── profile.tsx                   (Profil utilisateur - 150 lignes)
│
├── (supervisor)/                      (PHASE 2)
│   └── _layout.tsx
│
├── (admin)/                           (PHASE 3)
│   └── _layout.tsx
│
└── (auth)/
    ├── login.tsx                      (EXISTANT)
    └── role-selection.tsx             (EXISTANT)

hooks/
├── tasks/
│   ├── useTaskList.ts                 (Logique liste tâches)
│   ├── useTaskExecution.ts            (Logique exécution)
│   └── useTaskHistory.ts              (Logique historique)
├── nfc/
│   ├── useNfcScan.ts                  (Scan NFC réutilisable)
│   └── useDoubleBornage.ts            (EXISTANT)
└── auth/
    └── useBiometrics.ts               (Logique biométrie)

components/
├── tasks/
│   ├── TaskCard.tsx                   (Carte tâche)
│   ├── TaskStatusBadge.tsx            (Badge statut)
│   └── TaskFilters.tsx                (Filtres)
├── forms/
│   ├── DynamicForm.tsx                (EXISTANT - à intégrer)
│   └── PhotoPicker.tsx                (Sélection photos)
└── shared/
    ├── NfcScanButton.tsx              (Bouton scan réutilisable)
    └── LoadingSpinner.tsx             (Spinner)
```

#### Étapes de migration

**Jour 1-2 : Extraction des hooks**

1. **useTaskList.ts** (extraire de index.tsx lignes 300-450)
```typescript
// hooks/tasks/useTaskList.ts
import { useState, useEffect } from 'react';
import { getScheduledTasks } from '@/services/api/apiService';
import { useAuth } from '@/contexts/auth-context';

export function useTaskList() {
  const { user, token } = useAuth();
  const [tasks, setTasks] = useState<ScheduledTask[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchTasks = async () => {
    if (!user || !token) return;

    setLoading(true);
    setError(null);

    try {
      const data = await getScheduledTasks(user.id, token);
      setTasks(data);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTasks();
  }, [user, token]);

  return {
    tasks,
    loading,
    error,
    refetch: fetchTasks
  };
}
```

2. **useTaskExecution.ts** (extraire logique d'exécution)
```typescript
// hooks/tasks/useTaskExecution.ts
import { useState } from 'react';
import { createTaskExecution } from '@/services/api/apiService';
import { useAuth } from '@/contexts/auth-context';
import { useDoubleBornage } from '@/hooks/useDoubleBornage';

export function useTaskExecution() {
  const { user, token } = useAuth();
  const [formData, setFormData] = useState({
    temperature: '',
    pressure: '',
    notes: '',
    photos: []
  });
  const [submitting, setSubmitting] = useState(false);

  const doubleBornage = useDoubleBornage(token);

  const executeTask = async (scheduledTaskId: string, controlPointId: string) => {
    if (!token || !user) return { success: false };

    setSubmitting(true);

    try {
      // Premier scan (double bornage)
      const firstScanResult = await doubleBornage.startDoubleBornage(
        scheduledTaskId,
        controlPointId,
        user.id
      );

      if (!firstScanResult.success) {
        return { success: false };
      }

      return { success: true, executionId: firstScanResult.executionId };
    } catch (error: any) {
      console.error('Task execution failed:', error);
      return { success: false, error: error.message };
    } finally {
      setSubmitting(false);
    }
  };

  const submitForm = async () => {
    if (!doubleBornage.executionId) return { success: false };

    setSubmitting(true);

    try {
      // Second scan avec formulaire
      const result = await doubleBornage.completeDoubleBornage(
        JSON.stringify(formData),
        formData.photos[0] || null
      );

      if (result.success) {
        // Reset form
        setFormData({
          temperature: '',
          pressure: '',
          notes: '',
          photos: []
        });
      }

      return result;
    } catch (error: any) {
      return { success: false, error: error.message };
    } finally {
      setSubmitting(false);
    }
  };

  return {
    formData,
    setFormData,
    submitting,
    executeTask,
    submitForm,
    isInProgress: doubleBornage.isInProgress
  };
}
```

3. **useNfcScan.ts** (extraire logique NFC générique)
```typescript
// hooks/nfc/useNfcScan.ts
import { useState } from 'react';
import { scanNfcTag, initNfc, cancelNfcScan } from '@/services/nfc/nfcService';

export function useNfcScan() {
  const [scanning, setScanning] = useState(false);
  const [lastScannedUid, setLastScannedUid] = useState<string | null>(null);

  const scan = async (): Promise<{ success: boolean; uid?: string }> => {
    setScanning(true);

    try {
      await initNfc();
      const uid = await scanNfcTag();
      setLastScannedUid(uid);
      return { success: true, uid };
    } catch (error: any) {
      console.error('NFC scan failed:', error);
      return { success: false };
    } finally {
      setScanning(false);
    }
  };

  const cancel = async () => {
    await cancelNfcScan();
    setScanning(false);
  };

  return {
    scanning,
    lastScannedUid,
    scan,
    cancel
  };
}
```

**Jour 3 : Création des composants**

1. **TaskCard.tsx**
```typescript
// components/tasks/TaskCard.tsx
import React from 'react';
import { View, Text, TouchableOpacity, StyleSheet } from 'react-native';
import { ScheduledTask } from '@/services/api/apiService';
import TaskStatusBadge from './TaskStatusBadge';

interface TaskCardProps {
  task: ScheduledTask;
  onPress: () => void;
}

export default function TaskCard({ task, onPress }: TaskCardProps) {
  return (
    <TouchableOpacity style={styles.container} onPress={onPress}>
      <View style={styles.header}>
        <Text style={styles.title}>{task.taskTemplate.name}</Text>
        <TaskStatusBadge status={task.status} />
      </View>

      <Text style={styles.location}>
        📍 {task.controlPoint.location}
      </Text>

      <Text style={styles.time}>
        🕐 {new Date(task.scheduledFor).toLocaleTimeString('fr-FR')}
      </Text>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: '#fff',
    padding: 16,
    borderRadius: 12,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8
  },
  title: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#1e293b',
    flex: 1
  },
  location: {
    fontSize: 14,
    color: '#64748b',
    marginBottom: 4
  },
  time: {
    fontSize: 14,
    color: '#64748b'
  }
});
```

2. **NfcScanButton.tsx**
```typescript
// components/shared/NfcScanButton.tsx
import React from 'react';
import { TouchableOpacity, Text, StyleSheet, ActivityIndicator } from 'react';

interface NfcScanButtonProps {
  onPress: () => void;
  scanning?: boolean;
  disabled?: boolean;
  label?: string;
}

export default function NfcScanButton({
  onPress,
  scanning = false,
  disabled = false,
  label = 'Scanner NFC'
}: NfcScanButtonProps) {
  return (
    <TouchableOpacity
      style={[styles.button, disabled && styles.disabled]}
      onPress={onPress}
      disabled={disabled || scanning}
    >
      {scanning ? (
        <ActivityIndicator color="#fff" />
      ) : (
        <Text style={styles.text}>📱 {label}</Text>
      )}
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  button: {
    backgroundColor: '#2563eb',
    paddingVertical: 14,
    paddingHorizontal: 24,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: 48
  },
  disabled: {
    backgroundColor: '#94a3b8',
    opacity: 0.6
  },
  text: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold'
  }
});
```

**Jour 4 : Création des nouveaux écrans**

1. **app/(user)/tasks/index.tsx** (Liste des tâches)
```typescript
// app/(user)/tasks/index.tsx
import React from 'react';
import { View, FlatList, Text, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';
import { useTaskList } from '@/hooks/tasks/useTaskList';
import TaskCard from '@/components/tasks/TaskCard';

export default function TaskListScreen() {
  const router = useRouter();
  const { tasks, loading, error, refetch } = useTaskList();

  if (loading) {
    return (
      <View style={styles.container}>
        <Text>Chargement...</Text>
      </View>
    );
  }

  if (error) {
    return (
      <View style={styles.container}>
        <Text style={styles.error}>❌ {error}</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <FlatList
        data={tasks}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <TaskCard
            task={item}
            onPress={() => router.push(`/(user)/tasks/${item.id}`)}
          />
        )}
        onRefresh={refetch}
        refreshing={loading}
        contentContainerStyle={styles.list}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8fafc'
  },
  list: {
    padding: 16
  },
  error: {
    color: '#dc2626',
    fontSize: 16,
    textAlign: 'center',
    marginTop: 20
  }
});
```

2. **app/(user)/tasks/execute.tsx** (Exécution avec formulaire)
```typescript
// app/(user)/tasks/execute.tsx
import React, { useState } from 'react';
import { View, ScrollView, Alert, StyleSheet } from 'react-native';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useTaskExecution } from '@/hooks/tasks/useTaskExecution';
import DynamicForm from '@/components/DynamicForm';
import NfcScanButton from '@/components/shared/NfcScanButton';

export default function TaskExecuteScreen() {
  const { taskId, controlPointId } = useLocalSearchParams();
  const router = useRouter();
  const [step, setStep] = useState<'scan' | 'form'>('scan');

  const {
    formData,
    setFormData,
    submitting,
    executeTask,
    submitForm,
    isInProgress
  } = useTaskExecution();

  const handleFirstScan = async () => {
    const result = await executeTask(
      taskId as string,
      controlPointId as string
    );

    if (result.success) {
      setStep('form');
    } else {
      Alert.alert('Erreur', 'Impossible de démarrer la tâche');
    }
  };

  const handleSubmit = async (values: any) => {
    setFormData(values);

    const result = await submitForm();

    if (result.success) {
      Alert.alert('✅ Succès', 'Tâche enregistrée avec succès');
      router.back();
    } else {
      Alert.alert('Erreur', result.error || 'Erreur lors de la soumission');
    }
  };

  return (
    <ScrollView style={styles.container}>
      {step === 'scan' ? (
        <View style={styles.scanStep}>
          <NfcScanButton
            onPress={handleFirstScan}
            label="Scanner pour commencer"
          />
        </View>
      ) : (
        <DynamicForm
          template={/* TODO: Get from task */}
          onSubmit={handleSubmit}
          submitting={submitting}
        />
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff'
  },
  scanStep: {
    padding: 24,
    alignItems: 'center'
  }
});
```

#### Plan de migration progressive

1. ✅ Créer nouveaux fichiers (hooks, components, screens)
2. ✅ Tester chaque nouveau module isolément
3. ✅ Migrer route par route (tasks → anomaly → history)
4. ✅ Garder index.tsx comme fallback pendant migration
5. ✅ Une fois migration terminée, supprimer index.tsx
6. ✅ Mettre à jour navigation principale

---

### 1.2 - Implémenter JWT Refresh Token

**Durée**: 1 jour
**Priorité**: P0 - CRITIQUE SÉCURITÉ

#### Problème actuel
[contexts/auth-context.tsx:52](contexts/auth-context.tsx:52)
```typescript
const refreshToken = async () => {
  console.log('Token refresh not yet implemented');
};
```

#### Solution complète

```typescript
// contexts/auth-context.tsx

const refreshToken = async () => {
  try {
    const currentRefreshToken = await SecureStore.getItemAsync('refreshToken');

    if (!currentRefreshToken) {
      throw new Error('No refresh token available');
    }

    console.log('[AUTH] Refreshing token...');

    const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: currentRefreshToken })
    });

    if (!response.ok) {
      throw new Error('Token refresh failed');
    }

    const { token, refreshToken: newRefreshToken } = await response.json();

    // Sauvegarder nouveaux tokens
    await SecureStore.setItemAsync('token', token);
    await SecureStore.setItemAsync('refreshToken', newRefreshToken);

    setState(prev => ({
      ...prev,
      token,
      refreshToken: newRefreshToken
    }));

    console.log('[AUTH] ✅ Token refreshed successfully');
    return token;
  } catch (error: any) {
    console.error('[AUTH] ❌ Token refresh failed:', error);

    // Déconnecter l'utilisateur si refresh échoue
    await logout();
    throw error;
  }
};

// Intercepteur automatique pour apiService.ts
export const apiCallWithRefresh = async <T>(
  apiCall: () => Promise<T>
): Promise<T> => {
  try {
    return await apiCall();
  } catch (error: any) {
    // Si token expiré (401), essayer de rafraîchir
    if (error.status === 401 || error.message?.includes('Unauthorized')) {
      console.log('[API] Token expired, refreshing...');

      await refreshToken();

      // Réessayer l'appel avec nouveau token
      return await apiCall();
    }

    throw error;
  }
};
```

#### Intégration dans apiService.ts

```typescript
// services/api/apiService.ts

import { apiCallWithRefresh } from '@/contexts/auth-context';

export async function getScheduledTasks(
  userId: string,
  token: string
): Promise<ScheduledTask[]> {
  return apiCallWithRefresh(async () => {
    const response = await fetch(
      `${API_BASE_URL}/api/scheduledtasks/user/${userId}`,
      {
        headers: { 'Authorization': `Bearer ${token}` }
      }
    );

    if (!response.ok) {
      throw new Error('Failed to fetch tasks');
    }

    return response.json();
  });
}
```

---

### 1.3 - Migrer tous les fetch() vers apiService.ts

**Durée**: 2 jours
**Priorité**: P0 - CRITIQUE

#### Fichiers à migrer (13 fichiers)

**Analyse complète**:
```bash
# Rechercher tous les fetch() dans le projet
grep -r "fetch(" --include="*.tsx" --include="*.ts" app/
```

**Fichiers identifiés**:
1. app/(tabs)/index.tsx (ligne ~850)
2. app/(tabs)/explore.tsx
3. app/(tabs)/anomaly.tsx (déjà utilise apiService ✅)
4. app/login.tsx
5. app/role-selection.tsx
6. + 8 autres fichiers

#### Migration systématique

**Exemple 1: app/login.tsx**

AVANT:
```typescript
// ❌ MAUVAIS
const handleLogin = async () => {
  const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  });

  const data = await response.json();
  // ...
};
```

APRÈS:
```typescript
// ✅ BON
import { loginUser } from '@/services/api/apiService';

const handleLogin = async () => {
  try {
    const data = await loginUser(email, password);
    // ...
  } catch (error: any) {
    Alert.alert('Erreur', error.message);
  }
};
```

**Exemple 2: app/(tabs)/explore.tsx**

AVANT:
```typescript
// ❌ MAUVAIS
const fetchControlPoint = async (uid: string) => {
  const response = await fetch(
    `${API_BASE_URL}/api/controlpoints/uid/${uid}`,
    { headers: { 'Authorization': `Bearer ${token}` } }
  );

  const data = await response.json();
  // ...
};
```

APRÈS:
```typescript
// ✅ BON
import { getControlPointByUid } from '@/services/api/apiService';

const fetchControlPoint = async (uid: string) => {
  try {
    const data = await getControlPointByUid(uid, token!);
    // ...
  } catch (error: any) {
    Alert.alert('Erreur', error.message);
  }
};
```

#### Checklist migration

- [ ] login.tsx
- [ ] role-selection.tsx
- [ ] explore.tsx
- [ ] index.tsx (sera refactorisé de toute façon)
- [ ] Tous les autres fichiers avec fetch()

---

### 1.4 - Nettoyer code mort (~2000 lignes)

**Durée**: 1 jour
**Priorité**: P1

#### Fichiers à supprimer

```bash
# Composants jamais utilisés
components/hello-wave.tsx               # 50 lignes
components/parallax-scroll-view.tsx     # 120 lignes
components/external-link.tsx            # 40 lignes
components/collapsible.tsx              # 80 lignes

# Partiellement utilisés (à évaluer)
components/themed-text.tsx              # Garder si utilisé
components/themed-view.tsx              # Garder si utilisé
```

#### Vérification avant suppression

```bash
# Rechercher les imports de chaque composant
grep -r "hello-wave" --include="*.tsx" --include="*.ts" app/
grep -r "parallax-scroll-view" --include="*.tsx" --include="*.ts" app/
grep -r "external-link" --include="*.tsx" --include="*.ts" app/
grep -r "collapsible" --include="*.tsx" --include="*.ts" app/
```

#### Nettoyage package.json

```bash
# Supprimer dépendances inutilisées
npm prune
npx depcheck  # Identifier packages non utilisés
```

---

## 🎯 SPRINT 2 - Architecture (1 semaine)

### Objectif
Implémenter architecture moderne et scalable

---

### 2.1 - Implémenter Zustand (State Management)

**Durée**: 2-3 jours
**Priorité**: P0 - CRITIQUE pour SUPERVISOR

#### Installation

```bash
npm install zustand
npm install @react-native-async-storage/async-storage
```

#### Architecture des stores

```
store/
├── taskStore.ts           (Tâches)
├── anomalyStore.ts        (Anomalies)
├── authStore.ts           (Alternative à AuthContext - optionnel)
└── index.ts               (Export centralisé)
```

#### 1. Task Store

```typescript
// store/taskStore.ts
import create from 'zustand';
import { persist } from 'zustand/middleware';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { getScheduledTasks, ScheduledTask } from '@/services/api/apiService';

interface TaskState {
  // État
  tasks: ScheduledTask[];
  selectedTask: ScheduledTask | null;
  loading: boolean;
  error: string | null;
  lastFetch: Date | null;

  // Actions
  fetchTasks: (userId: string, token: string) => Promise<void>;
  selectTask: (taskId: string) => void;
  updateTaskStatus: (taskId: string, status: string) => void;
  clearTasks: () => void;
}

export const useTaskStore = create<TaskState>()(
  persist(
    (set, get) => ({
      // État initial
      tasks: [],
      selectedTask: null,
      loading: false,
      error: null,
      lastFetch: null,

      // Fetch tasks avec cache
      fetchTasks: async (userId, token) => {
        const { lastFetch } = get();

        // Cache de 30 secondes
        if (lastFetch && Date.now() - lastFetch.getTime() < 30000) {
          console.log('[STORE] Using cached tasks');
          return;
        }

        set({ loading: true, error: null });

        try {
          const tasks = await getScheduledTasks(userId, token);
          set({
            tasks,
            loading: false,
            lastFetch: new Date()
          });
        } catch (error: any) {
          set({
            error: error.message,
            loading: false
          });
        }
      },

      // Sélectionner une tâche
      selectTask: (taskId) => {
        const task = get().tasks.find(t => t.id === taskId);
        set({ selectedTask: task || null });
      },

      // Mettre à jour statut
      updateTaskStatus: (taskId, status) => {
        set(state => ({
          tasks: state.tasks.map(task =>
            task.id === taskId ? { ...task, status } : task
          )
        }));
      },

      // Clear
      clearTasks: () => {
        set({
          tasks: [],
          selectedTask: null,
          error: null,
          lastFetch: null
        });
      }
    }),
    {
      name: 'task-storage',
      storage: AsyncStorage
    }
  )
);
```

#### 2. Anomaly Store

```typescript
// store/anomalyStore.ts
import create from 'zustand';
import { createAnomaly, CreateAnomalyPayload } from '@/services/api/apiService';

interface AnomalyState {
  submitting: boolean;
  lastSubmitted: Date | null;

  submit: (payload: CreateAnomalyPayload, token: string) => Promise<boolean>;
}

export const useAnomalyStore = create<AnomalyState>((set) => ({
  submitting: false,
  lastSubmitted: null,

  submit: async (payload, token) => {
    set({ submitting: true });

    try {
      await createAnomaly(payload, token);
      set({
        submitting: false,
        lastSubmitted: new Date()
      });
      return true;
    } catch (error) {
      set({ submitting: false });
      return false;
    }
  }
}));
```

#### 3. Utilisation dans les composants

```typescript
// app/(user)/tasks/index.tsx
import { useTaskStore } from '@/store/taskStore';
import { useAuth } from '@/contexts/auth-context';

export default function TaskListScreen() {
  const { user, token } = useAuth();
  const { tasks, loading, error, fetchTasks } = useTaskStore();

  useEffect(() => {
    if (user && token) {
      fetchTasks(user.id, token);
    }
  }, [user, token]);

  return (
    <FlatList
      data={tasks}
      renderItem={({ item }) => <TaskCard task={item} />}
      refreshing={loading}
      onRefresh={() => fetchTasks(user!.id, token!)}
    />
  );
}
```

**Avantages immédiats**:
- ✅ Cache automatique (30 secondes)
- ✅ Pas de refetch inutiles
- ✅ État partagé entre écrans
- ✅ Persistance avec AsyncStorage
- ✅ Performance améliorée

---

### 2.2 - Intégrer DynamicForm dans les écrans

**Durée**: 1-2 jours
**Priorité**: P1

#### Problème actuel

[components/DynamicForm.tsx](components/DynamicForm.tsx) créé (472 lignes) mais JAMAIS utilisé.

Formulaires statiques en dur dans index.tsx.

#### Solution

**1. Ajouter formTemplate dans apiService.ts**

```typescript
// services/api/apiService.ts

export interface TaskTemplate {
  id: string;
  name: string;
  description: string;
  formTemplate: FormTemplate | null;  // ← AJOUTER
}

export interface FormTemplate {
  fields: FormField[];
}

export interface FormField {
  name: string;
  label: string;
  type: 'text' | 'number' | 'boolean' | 'select' | 'multiselect' | 'photo';
  required?: boolean;
  min?: number;
  max?: number;
  options?: string[];
  maxPhotos?: number;
}
```

**2. Utiliser DynamicForm dans execute.tsx**

```typescript
// app/(user)/tasks/execute.tsx
import DynamicForm from '@/components/DynamicForm';
import { useTaskStore } from '@/store/taskStore';

export default function TaskExecuteScreen() {
  const { selectedTask } = useTaskStore();
  const { submitForm } = useTaskExecution();

  if (!selectedTask?.taskTemplate.formTemplate) {
    return <Text>Aucun formulaire défini</Text>;
  }

  return (
    <DynamicForm
      template={selectedTask.taskTemplate.formTemplate}
      onSubmit={(values) => submitForm(values)}
    />
  );
}
```

**3. Vérifier backend**

S'assurer que le backend renvoie bien `formTemplate` dans les réponses API.

---

### 2.3 - Restructurer navigation (USER/SUPERVISOR/ADMIN)

**Durée**: 1-2 jours
**Priorité**: P0 - REQUIS pour Phase 2

#### Structure actuelle (problématique)

```
app/
├── (tabs)/          ← Mélange USER uniquement
│   ├── index.tsx
│   ├── anomaly.tsx
│   └── explore.tsx
├── login.tsx
└── role-selection.tsx
```

#### Structure cible (claire)

```
app/
├── _layout.tsx                        (Root)
│
├── (auth)/
│   ├── _layout.tsx
│   ├── login.tsx
│   └── role-selection.tsx
│
├── (user)/
│   ├── _layout.tsx                    (Tabs USER)
│   ├── tasks/
│   │   ├── index.tsx                  (Liste)
│   │   ├── [id].tsx                   (Détail)
│   │   └── execute.tsx                (Exécution)
│   ├── anomaly.tsx
│   ├── history.tsx
│   └── profile.tsx
│
├── (supervisor)/                       ← PHASE 2
│   ├── _layout.tsx                    (Tabs SUPERVISOR)
│   ├── team.tsx                       (Vue équipe)
│   ├── reassign.tsx                   (Réaffectation)
│   └── overdue.tsx                    (Tâches en retard)
│
├── (admin)/                            ← PHASE 3
│   ├── _layout.tsx                    (Tabs ADMIN)
│   ├── dashboard.tsx
│   ├── chips.tsx                      (Enregistrement puces)
│   └── users.tsx
│
└── +not-found.tsx
```

#### Implémentation

**1. Root Layout**

```typescript
// app/_layout.tsx
import { Stack } from 'expo-router';
import { AuthProvider } from '@/contexts/auth-context';

export default function RootLayout() {
  return (
    <AuthProvider>
      <Stack screenOptions={{ headerShown: false }}>
        <Stack.Screen name="(auth)" />
        <Stack.Screen name="(user)" />
        <Stack.Screen name="(supervisor)" />
        <Stack.Screen name="(admin)" />
      </Stack>
    </AuthProvider>
  );
}
```

**2. User Layout (Tabs)**

```typescript
// app/(user)/_layout.tsx
import { Tabs } from 'expo-router';
import { IconSymbol } from '@/components/ui/icon-symbol';

export default function UserLayout() {
  return (
    <Tabs screenOptions={{ headerShown: false }}>
      <Tabs.Screen
        name="tasks"
        options={{
          title: 'Tâches',
          tabBarIcon: ({ color }) => (
            <IconSymbol size={28} name="house.fill" color={color} />
          )
        }}
      />
      <Tabs.Screen
        name="anomaly"
        options={{
          title: 'Anomalie',
          tabBarIcon: ({ color }) => (
            <IconSymbol size={28} name="exclamationmark.triangle.fill" color={color} />
          )
        }}
      />
      <Tabs.Screen
        name="history"
        options={{
          title: 'Historique',
          tabBarIcon: ({ color }) => (
            <IconSymbol size={28} name="clock.fill" color={color} />
          )
        }}
      />
      <Tabs.Screen
        name="profile"
        options={{
          title: 'Profil',
          tabBarIcon: ({ color }) => (
            <IconSymbol size={28} name="person.fill" color={color} />
          )
        }}
      />
    </Tabs>
  );
}
```

**3. Supervisor Layout (Tabs)** - PHASE 2

```typescript
// app/(supervisor)/_layout.tsx
import { Tabs } from 'expo-router';
import { IconSymbol } from '@/components/ui/icon-symbol';

export default function SupervisorLayout() {
  return (
    <Tabs screenOptions={{ headerShown: false }}>
      <Tabs.Screen
        name="team"
        options={{
          title: 'Équipe',
          tabBarIcon: ({ color }) => (
            <IconSymbol size={28} name="person.3.fill" color={color} />
          )
        }}
      />
      <Tabs.Screen
        name="reassign"
        options={{
          title: 'Réaffecter',
          tabBarIcon: ({ color }) => (
            <IconSymbol size={28} name="arrow.triangle.2.circlepath" color={color} />
          )
        }}
      />
      <Tabs.Screen
        name="overdue"
        options={{
          title: 'En retard',
          tabBarIcon: ({ color }) => (
            <IconSymbol size={28} name="exclamationmark.circle.fill" color={color} />
          )
        }}
      />
    </Tabs>
  );
}
```

**4. Redirection basée sur le rôle**

```typescript
// app/(auth)/role-selection.tsx
import { useRouter } from 'expo-router';

const handleRoleSelection = (role: 'USER' | 'SUPERVISOR' | 'ADMIN') => {
  await saveRole(role);

  // Rediriger selon le rôle
  switch (role) {
    case 'USER':
      router.replace('/(user)/tasks');
      break;
    case 'SUPERVISOR':
      router.replace('/(supervisor)/team');
      break;
    case 'ADMIN':
      router.replace('/(admin)/dashboard');
      break;
  }
};
```

---

## 🎯 SPRINT 3 - Phase 2 SUPERVISOR (1 semaine)

### Objectif
Implémenter features SUPERVISOR sur base saine

---

### 3.1 - Vue équipe avec filtres

**Fichier**: app/(supervisor)/team.tsx

**Features**:
- Liste tous les techniciens de l'équipe
- Voir leurs tâches (PENDING, IN_PROGRESS, COMPLETED, OVERDUE)
- Filtres : Par statut, par technicien, par date
- Vue temps réel (refresh auto)

---

### 3.2 - Réaffectation de tâches

**Fichier**: app/(supervisor)/reassign.tsx

**Features**:
- Sélectionner tâche à réaffecter
- Voir techniciens qualifiés (backend endpoint)
- Réaffecter avec confirmation

**Endpoint backend requis**:
```
GET /api/users/qualified-for-task/{taskId}
PUT /api/scheduledtasks/{id}/reassign
```

---

### 3.3 - Interception OVERDUE

**Fichier**: app/(supervisor)/overdue.tsx

**Features**:
- Liste tâches en retard (OVERDUE)
- Prendre en charge (claim)
- Réaffecter à un technicien qualifié

**Endpoint backend requis**:
```
GET /api/scheduledtasks?teamId={teamId}&status=OVERDUE
PUT /api/scheduledtasks/{id}/claim
```

---

## 📊 Suivi de Progrès

### Sprint 1 (Semaine 1)
- [ ] Refactoriser index.tsx
- [ ] Implémenter JWT refresh
- [ ] Migrer fetch() → apiService
- [ ] Nettoyer code mort

### Sprint 2 (Semaine 2)
- [ ] Implémenter Zustand
- [ ] Intégrer DynamicForm
- [ ] Restructurer navigation

### Sprint 3 (Semaine 3)
- [ ] Vue équipe
- [ ] Réaffectation
- [ ] Interception OVERDUE

---

## 🔄 Synchronisation avec Backend

### Processus continu

À chaque modification backend qui impacte le mobile :

1. **Backend fait une modification** (nouveau endpoint, champ ajouté, etc.)
2. **Backend notifie** (issue GitHub, Slack, email)
3. **Mobile update immédiat**:
   - Mettre à jour types TypeScript (apiService.ts)
   - Mettre à jour stores si nécessaire
   - Tester l'intégration
   - Push + PR

### Exemple concret

**Backend ajoute champ `priority` aux tâches**

1. Backend :
```csharp
public class ScheduledTask {
  public string Priority { get; set; } // NOUVEAU
}
```

2. Mobile (même jour) :
```typescript
// services/api/apiService.ts
export interface ScheduledTask {
  priority: 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL'; // NOUVEAU
}

// components/tasks/TaskCard.tsx
<Text>🔥 Priorité: {task.priority}</Text>
```

3. Commit + Push :
```bash
git add .
git commit -m "feat: Add priority field to tasks (sync with backend)"
git push
```

---

## ✅ Checklist Complète

### Sprint 1
- [ ] Créer hooks (useTaskList, useTaskExecution, useNfcScan)
- [ ] Créer composants (TaskCard, NfcScanButton, etc.)
- [ ] Créer écrans modulaires (tasks/index, tasks/execute)
- [ ] Migrer progressivement depuis index.tsx
- [ ] Supprimer index.tsx monstre
- [ ] Implémenter JWT refresh
- [ ] Migrer 13 fichiers vers apiService
- [ ] Supprimer composants morts

### Sprint 2
- [ ] Installer Zustand + AsyncStorage
- [ ] Créer taskStore.ts
- [ ] Créer anomalyStore.ts
- [ ] Intégrer dans écrans
- [ ] Tester cache et persistance
- [ ] Intégrer DynamicForm
- [ ] Créer structure (auth)/(user)/(supervisor)/(admin)
- [ ] Migrer écrans existants
- [ ] Implémenter redirection par rôle

### Sprint 3
- [ ] Créer app/(supervisor)/team.tsx
- [ ] Créer app/(supervisor)/reassign.tsx
- [ ] Créer app/(supervisor)/overdue.tsx
- [ ] Tester avec backend
- [ ] Documentation Phase 2

---

**Prêt à démarrer le refactoring ! 🚀**
