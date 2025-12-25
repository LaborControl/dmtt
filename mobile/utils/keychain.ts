/**
 * Wrapper Keychain avec fallback en mémoire pour Expo Go
 */

import { storage as AsyncStorage } from './storage';

let Keychain: any;
let isMemoryKeychain = false;

try {
  Keychain = require('react-native-keychain');

  // Tester si Keychain fonctionne
  if (!Keychain || typeof Keychain.setInternetCredentials !== 'function') {
    throw new Error('Keychain non disponible');
  }

  console.log('✅ Keychain natif détecté');
} catch (error) {
  console.warn('⚠️ Keychain non disponible - utilisation du stockage en mémoire');
  isMemoryKeychain = true;

  // Fallback vers AsyncStorage
  Keychain = {
    setInternetCredentials: async (server: string, username: string, password: string) => {
      console.log(`[Memory Keychain] setInternetCredentials("${server}", "${username}")`);
      await AsyncStorage.setItem(`keychain:${server}`, JSON.stringify({ username, password }));
      return true;
    },

    getInternetCredentials: async (server: string) => {
      console.log(`[Memory Keychain] getInternetCredentials("${server}")`);
      const data = await AsyncStorage.getItem(`keychain:${server}`);

      if (!data) {
        return false;
      }

      const { username, password } = JSON.parse(data);
      return { username, password };
    },

    resetInternetCredentials: async (server: string) => {
      console.log(`[Memory Keychain] resetInternetCredentials("${server}")`);
      await AsyncStorage.removeItem(`keychain:${server}`);
      return true;
    },
  };
}

export const keychain = Keychain;
export const isUsingMemoryKeychain = isMemoryKeychain;

if (isMemoryKeychain) {
  console.warn(`
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⚠️  KEYCHAIN EN MÉMOIRE ACTIVÉ
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Les credentials seront stockés de manière non sécurisée.

Pour Keychain natif, utilise expo-dev-client :
  npx expo prebuild
  npx expo run:android (ou run:ios)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  `);
}
