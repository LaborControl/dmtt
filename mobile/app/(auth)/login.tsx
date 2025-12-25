/**
 * Login Screen
 *
 * Authenticates users and navigates to role selection screen
 */

import React, { useState, useEffect } from 'react';
import { StyleSheet, Text, View, TextInput, TouchableOpacity, Image, Alert, ActivityIndicator } from 'react-native';
import { router } from 'expo-router';
import ReactNativeBiometrics from 'react-native-biometrics';
import { useAuth } from '@/contexts/AuthContext';

export default function LoginScreen() {
  const { login, isAuthenticated, isLoading: authLoading, user } = useAuth();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [biometricsAvailable, setBiometricsAvailable] = useState(false);
  const [hasSavedCredentials, setHasSavedCredentials] = useState(false);

  // ==========================================================================
  // EFFECT: Check biometrics availability and saved credentials
  // ==========================================================================
  useEffect(() => {
    checkBiometrics();
    checkSavedCredentials();
  }, []);

  // ==========================================================================
  // EFFECT: Navigate to DMTT role selection if authenticated
  // ==========================================================================
  useEffect(() => {
    if (isAuthenticated && user) {
      router.replace('/dmtt-role-selection');
    }
  }, [isAuthenticated, user]);

  // ==========================================================================
  // FUNCTION: Check if biometrics is available
  // ==========================================================================
  const checkBiometrics = async () => {
    try {
      const rnBiometrics = new ReactNativeBiometrics({ allowDeviceCredentials: true });
      const { available } = await rnBiometrics.isSensorAvailable();
      setBiometricsAvailable(available);
    } catch (error) {
      console.error('[LOGIN] Biometrics check error:', error);
      setBiometricsAvailable(false);
    }
  };

  // ==========================================================================
  // FUNCTION: Check if there are saved credentials
  // ==========================================================================
  const checkSavedCredentials = async () => {
    try {
      const Keychain = require('@/utils/keychain').keychain;
      const credentials = await Keychain.getInternetCredentials('laborcontrol');
      setHasSavedCredentials(!!credentials);
    } catch (error) {
      console.error('[LOGIN] Error checking saved credentials:', error);
      setHasSavedCredentials(false);
    }
  };

  // ==========================================================================
  // FUNCTION: Handle login
  // ==========================================================================
  const handleLogin = async () => {
    if (!email || !password) {
      Alert.alert('Erreur', 'Veuillez remplir tous les champs');
      return;
    }

    setLoading(true);

    try {
      await login(email, password);
      // Navigation handled by useEffect
    } catch (error) {
      console.error('[LOGIN] Login error:', error);
      Alert.alert('Erreur', 'Email ou mot de passe incorrect');
    } finally {
      setLoading(false);
    }
  };

  // ==========================================================================
  // FUNCTION: Handle biometric login
  // ==========================================================================
  const handleBiometricLogin = async () => {
    try {
      const rnBiometrics = new ReactNativeBiometrics({ allowDeviceCredentials: true });

      const { success } = await rnBiometrics.simplePrompt({
        promptMessage: 'Authentifiez-vous pour accéder à LABOR CONTROL',
      });

      if (success) {
        setLoading(true);
        // Retrieve saved credentials from keychain
        const Keychain = require('@/utils/keychain').keychain;
        const credentials = await Keychain.getInternetCredentials('laborcontrol');

        if (credentials) {
          // Auto-login with saved credentials
          await login(credentials.username, credentials.password);
          // Navigation handled by useEffect
        } else {
          Alert.alert('Info', 'Aucune information de connexion enregistrée. Veuillez vous connecter manuellement.');
        }
      }
    } catch (error) {
      console.error('[LOGIN] Biometric error:', error);
      Alert.alert('Erreur', 'Authentification biométrique échouée');
    } finally {
      setLoading(false);
    }
  };

  // ==========================================================================
  // RENDER: Loading state
  // ==========================================================================
  if (authLoading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#2563eb" />
        <Text style={styles.loadingText}>Chargement...</Text>
      </View>
    );
  }

  // ==========================================================================
  // RENDER
  // ==========================================================================
  return (
    <View style={styles.container}>
      <View style={styles.content}>
        {/* LOGO */}
        <Image
          source={require('@/assets/logo.png')}
          style={styles.logo}
          resizeMode="contain"
        />

        {/* TITLE */}
        <Text style={styles.title}>LABOR CONTROL</Text>
        <Text style={styles.subtitle}>Fait, scanné, prouvé.</Text>

        {/* BIOMETRIC LOGIN BUTTON */}
        {biometricsAvailable && hasSavedCredentials && (
          <TouchableOpacity
            style={[styles.button, styles.biometricButton]}
            onPress={handleBiometricLogin}
            disabled={loading}
          >
            <Text style={styles.buttonText}>Connexion rapide</Text>
          </TouchableOpacity>
        )}

        {/* EMAIL INPUT */}
        <TextInput
          style={styles.input}
          placeholder="Email"
          value={email}
          onChangeText={setEmail}
          autoCapitalize="none"
          keyboardType="email-address"
          editable={!loading}
        />

        {/* PASSWORD INPUT */}
        <TextInput
          style={styles.input}
          placeholder="Mot de passe"
          value={password}
          onChangeText={setPassword}
          secureTextEntry
          editable={!loading}
        />

        {/* LOGIN BUTTON */}
        <TouchableOpacity
          style={[styles.button, loading && styles.buttonDisabled]}
          onPress={handleLogin}
          disabled={loading}
        >
          {loading ? (
            <ActivityIndicator color="#fff" />
          ) : (
            <Text style={styles.buttonText}>SE CONNECTER</Text>
          )}
        </TouchableOpacity>

        {/* FORGOT PASSWORD */}
        <TouchableOpacity style={styles.forgotPasswordButton}>
          <Text style={styles.forgotPasswordText}>Mot de passe oublié ?</Text>
        </TouchableOpacity>
      </View>
    </View>
  );
}

// ============================================================================
// STYLES
// ============================================================================

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8fafc',
  },
  loadingContainer: {
    flex: 1,
    backgroundColor: '#f8fafc',
    alignItems: 'center',
    justifyContent: 'center',
  },
  loadingText: {
    marginTop: 16,
    fontSize: 16,
    color: '#64748b',
  },
  content: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 20,
  },
  logo: {
    width: 120,
    height: 120,
    marginBottom: 24,
  },
  title: {
    fontSize: 36,
    fontWeight: 'bold',
    color: '#2563eb',
    marginBottom: 8,
  },
  subtitle: {
    fontSize: 18,
    color: '#64748b',
    marginBottom: 48,
    fontStyle: 'italic',
  },
  input: {
    width: '100%',
    height: 56,
    borderWidth: 2,
    borderColor: '#e2e8f0',
    borderRadius: 12,
    paddingHorizontal: 16,
    marginBottom: 16,
    fontSize: 16,
    backgroundColor: '#fff',
    color: '#1e293b',
  },
  button: {
    width: '100%',
    height: 56,
    backgroundColor: '#2563eb',
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 8,
  },
  buttonDisabled: {
    backgroundColor: '#94a3b8',
  },
  biometricButton: {
    backgroundColor: '#10b981',
    marginBottom: 20,
  },
  buttonText: {
    color: '#fff',
    fontSize: 18,
    fontWeight: 'bold',
  },
  forgotPasswordButton: {
    marginTop: 16,
  },
  forgotPasswordText: {
    color: '#2563eb',
    fontSize: 14,
    fontWeight: '600',
  },
  hint: {
    marginTop: 24,
    fontSize: 12,
    color: '#94a3b8',
  },
});
