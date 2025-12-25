/**
 * Storage Wrapper - AsyncStorage (Fallback)
 *
 * Using AsyncStorage for development
 * TODO: Switch back to MMKV for production builds
 */

import AsyncStorage from '@react-native-async-storage/async-storage';

// ============================================================================
// Storage Instance (AsyncStorage Wrapper)
// ============================================================================

// Create MMKV-like API using AsyncStorage
export const storage = {
  set: async (key: string, value: string | number | boolean) => {
    try {
      await AsyncStorage.setItem(key, String(value));
    } catch (error) {
      console.error(`[Storage] Failed to set ${key}:`, error);
    }
  },

  getString: async (key: string): Promise<string | undefined> => {
    try {
      const value = await AsyncStorage.getItem(key);
      return value ?? undefined;
    } catch (error) {
      console.error(`[Storage] Failed to get string ${key}:`, error);
      return undefined;
    }
  },

  getNumber: async (key: string): Promise<number | undefined> => {
    try {
      const value = await AsyncStorage.getItem(key);
      return value ? Number(value) : undefined;
    } catch (error) {
      console.error(`[Storage] Failed to get number ${key}:`, error);
      return undefined;
    }
  },

  getBoolean: async (key: string): Promise<boolean | undefined> => {
    try {
      const value = await AsyncStorage.getItem(key);
      return value === 'true' ? true : value === 'false' ? false : undefined;
    } catch (error) {
      console.error(`[Storage] Failed to get boolean ${key}:`, error);
      return undefined;
    }
  },

  delete: async (key: string) => {
    try {
      await AsyncStorage.removeItem(key);
    } catch (error) {
      console.error(`[Storage] Failed to delete ${key}:`, error);
    }
  },

  clearAll: async () => {
    try {
      await AsyncStorage.clear();
    } catch (error) {
      console.error('[Storage] Failed to clear all:', error);
    }
  },

  getAllKeys: async (): Promise<string[]> => {
    try {
      return await AsyncStorage.getAllKeys();
    } catch (error) {
      console.error('[Storage] Failed to get all keys:', error);
      return [];
    }
  },

  contains: async (key: string): Promise<boolean> => {
    try {
      const value = await AsyncStorage.getItem(key);
      return value !== null;
    } catch (error) {
      console.error(`[Storage] Failed to check contains ${key}:`, error);
      return false;
    }
  },
};

// ============================================================================
// Storage Adapter for Zustand Persist
// ============================================================================

export const mmkvStorage = {
  getItem: async (name: string): Promise<string | null> => {
    const value = await storage.getString(name);
    return value ?? null;
  },

  setItem: async (name: string, value: string): Promise<void> => {
    await storage.set(name, value);
  },

  removeItem: async (name: string): Promise<void> => {
    await storage.delete(name);
  },
};

// ============================================================================
// Typed Storage Helpers
// ============================================================================

/**
 * Save JSON object to storage
 */
export const saveObject = async <T>(key: string, value: T): Promise<void> => {
  try {
    const jsonValue = JSON.stringify(value);
    await storage.set(key, jsonValue);
  } catch (error) {
    console.error(`[Storage] Failed to save object for key "${key}":`, error);
  }
};

/**
 * Get JSON object from storage
 */
export const getObject = async <T>(key: string): Promise<T | null> => {
  try {
    const jsonValue = await storage.getString(key);
    return jsonValue ? JSON.parse(jsonValue) : null;
  } catch (error) {
    console.error(`[Storage] Failed to get object for key "${key}":`, error);
    return null;
  }
};

/**
 * Save string to storage
 */
export const saveString = async (key: string, value: string): Promise<void> => {
  await storage.set(key, value);
};

/**
 * Get string from storage
 */
export const getString = async (key: string): Promise<string | undefined> => {
  return await storage.getString(key);
};

/**
 * Save number to storage
 */
export const saveNumber = async (key: string, value: number): Promise<void> => {
  await storage.set(key, value);
};

/**
 * Get number from storage
 */
export const getNumber = async (key: string): Promise<number | undefined> => {
  return await storage.getNumber(key);
};

/**
 * Save boolean to storage
 */
export const saveBoolean = async (key: string, value: boolean): Promise<void> => {
  await storage.set(key, value);
};

/**
 * Get boolean from storage
 */
export const getBoolean = async (key: string): Promise<boolean | undefined> => {
  return await storage.getBoolean(key);
};

/**
 * Remove item from storage
 */
export const removeItem = async (key: string): Promise<void> => {
  await storage.delete(key);
};

/**
 * Clear all storage
 */
export const clearAll = async (): Promise<void> => {
  await storage.clearAll();
};

/**
 * Get all keys
 */
export const getAllKeys = async (): Promise<string[]> => {
  return await storage.getAllKeys();
};

/**
 * Check if key exists
 */
export const hasKey = async (key: string): Promise<boolean> => {
  return await storage.contains(key);
};

// ============================================================================
// Storage Keys (Constants)
// ============================================================================

export const StorageKeys = {
  // Auth
  AUTH_TOKEN: 'auth.token',
  REFRESH_TOKEN: 'auth.refreshToken',
  USER: 'auth.user',

  // Tasks
  TASKS_CACHE: 'tasks.cache',
  TASKS_LAST_FETCH: 'tasks.lastFetch',

  // Anomalies
  ANOMALY_HISTORY: 'anomaly.history',
  ANOMALY_LAST_SUBMITTED: 'anomaly.lastSubmitted',

  // Offline Queue
  OFFLINE_QUEUE: 'offline.queue',

  // Settings
  BIOMETRIC_ENABLED: 'settings.biometricEnabled',
  SELECTED_ROLE: 'settings.selectedRole',
} as const;

// ============================================================================
// Migration from AsyncStorage (Optional Helper)
// ============================================================================

/**
 * Migrate data from AsyncStorage to MMKV
 * Call this once during app startup if migrating from AsyncStorage
 */
export const migrateFromAsyncStorage = async (
  AsyncStorage: any
): Promise<void> => {
  try {
    console.log('[Storage] Starting migration from AsyncStorage to MMKV...');

    const keys = await AsyncStorage.getAllKeys();
    console.log(`[Storage] Found ${keys.length} keys to migrate`);

    for (const key of keys) {
      const value = await AsyncStorage.getItem(key);
      if (value !== null) {
        storage.set(key, value);
      }
    }

    console.log('[Storage] Migration complete, clearing AsyncStorage...');
    await AsyncStorage.clear();
    console.log('[Storage] ✅ Migration successful');
  } catch (error) {
    console.error('[Storage] Migration failed:', error);
  }
};
