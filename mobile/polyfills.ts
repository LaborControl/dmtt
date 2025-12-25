/**
 * Polyfills pour Expo Go
 * À utiliser UNIQUEMENT pour les tests rapides
 * En production, utiliser expo-dev-client
 */

// Mock en mémoire AVANT l'import
const memoryStorage = new Map<string, string>();

// Mock complet d'AsyncStorage
const AsyncStorageMock = {
  getItem: async (key: string): Promise<string | null> => {
    const value = memoryStorage.get(key) || null;
    console.log(`[AsyncStorage Mock] getItem("${key}"):`, value ? 'found' : 'null');
    return value;
  },
  setItem: async (key: string, value: string): Promise<void> => {
    console.log(`[AsyncStorage Mock] setItem("${key}")`);
    memoryStorage.set(key, value);
  },
  removeItem: async (key: string): Promise<void> => {
    console.log(`[AsyncStorage Mock] removeItem("${key}")`);
    memoryStorage.delete(key);
  },
  clear: async (): Promise<void> => {
    console.log('[AsyncStorage Mock] clear()');
    memoryStorage.clear();
  },
  getAllKeys: async (): Promise<readonly string[]> => {
    return Array.from(memoryStorage.keys());
  },
  multiGet: async (keys: readonly string[]): Promise<readonly [string, string | null][]> => {
    return keys.map(key => [key, memoryStorage.get(key) || null]);
  },
  multiSet: async (keyValuePairs: readonly [string, string][]): Promise<void> => {
    keyValuePairs.forEach(([key, value]) => memoryStorage.set(key, value));
  },
  multiRemove: async (keys: readonly string[]): Promise<void> => {
    keys.forEach(key => memoryStorage.delete(key));
  },
};

// Patch le module avant qu'il soit importé
const Module = require('module');
const originalRequire = Module.prototype.require;

Module.prototype.require = function (id: string) {
  if (id === '@react-native-async-storage/async-storage') {
    console.log('⚠️ AsyncStorage intercepté - utilisation du mock mémoire');
    return AsyncStorageMock;
  }
  return originalRequire.apply(this, arguments);
};

console.log('✅ Polyfill AsyncStorage activé (stockage en mémoire)');

export {};
