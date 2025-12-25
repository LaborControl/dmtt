const { getDefaultConfig } = require('expo/metro-config');
const path = require('path');

/**
 * Metro configuration for Expo
 * https://docs.expo.dev/guides/customizing-metro/
 */
const config = getDefaultConfig(__dirname);

// Configuration des alias de chemins
config.resolver = {
  ...config.resolver,
  extraNodeModules: {
    '@': path.resolve(__dirname),
  },
};

module.exports = config;