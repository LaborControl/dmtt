// ============================================================================
// LABOR CONTROL - Écran d'enregistrement rapide de puces NFC
// Page temporaire pour scanner et enregistrer toutes les puces
// ============================================================================

import React, { useState, useEffect } from 'react';
import {
  StyleSheet,
  Text,
  View,
  TouchableOpacity,
  FlatList,
  Alert,
} from 'react-native';
import NfcManager, { NfcTech } from 'react-native-nfc-manager';
import { keychain as Keychain } from '@/utils/keychain';

const API_BASE_URL = 'https://laborcontrol-api.azurewebsites.net/api';

interface ScannedChip {
  uid: string;
  chipId?: string;
  status: 'scanning' | 'success' | 'duplicate' | 'error';
  message?: string;
  timestamp: string;
}

export default function RegisterChipsScreen() {
  const [isScanning, setIsScanning] = useState(false);
  const [scannedChips, setScannedChips] = useState<ScannedChip[]>([]);
  const [token, setToken] = useState('');

  // ==========================================================================
  // EFFET : Charger le token au démarrage
  // ==========================================================================
  useEffect(() => {
    loadToken();
  }, []);

  const loadToken = async () => {
    try {
      const credentials = await Keychain.getInternetCredentials('laborcontrol');
      if (credentials) {
        // Re-login pour avoir un token frais
        const response = await fetch(`${API_BASE_URL}/auth/login`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            email: credentials.username,
            password: credentials.password,
          }),
        });

        if (response.ok) {
          const data = await response.json();
          setToken(data.token);
        }
      }
    } catch (error) {
      console.error('Erreur chargement token:', error);
    }
  };

  // ==========================================================================
  // FONCTION : Scanner une puce
  // ==========================================================================
  const scanChip = async () => {
    if (!token) {
      Alert.alert('Erreur', 'Vous devez être connecté pour enregistrer des puces');
      return;
    }

    setIsScanning(true);

    try {
      await NfcManager.start();
      await NfcManager.requestTechnology(NfcTech.Ndef);

      const tag = await NfcManager.getTag();

      if (tag && tag.id) {
        let uid;
        if (Array.isArray(tag.id)) {
          uid = tag.id
            .map((byte: number) => byte.toString(16).padStart(2, '0'))
            .join('')
            .toUpperCase();
        } else if (typeof tag.id === 'string') {
          uid = tag.id.toUpperCase();
        } else {
          uid = String(tag.id).toUpperCase();
        }

        console.log('[REGISTER] UID scanné:', uid);

        // Ajouter à la liste en "scanning"
        const newChip: ScannedChip = {
          uid,
          status: 'scanning',
          timestamp: new Date().toLocaleTimeString('fr-FR'),
        };
        setScannedChips((prev) => [newChip, ...prev]);

        // Enregistrer dans la base de données
        await registerChipInDatabase(uid);
      }
    } catch (error) {
      console.error('[REGISTER] Erreur scan:', error);
      const errorMessage = String(error);
      if (
        !errorMessage.includes('cancelled') &&
        !errorMessage.includes('Cancel')
      ) {
        Alert.alert('❌ Erreur', 'Scan annulé ou échec de lecture');
      }
    } finally {
      setIsScanning(false);
      NfcManager.cancelTechnologyRequest();
    }
  };

  // ==========================================================================
  // FONCTION : Enregistrer la puce dans la base de données
  // ==========================================================================
  const registerChipInDatabase = async (uid: string) => {
    try {
      const response = await fetch(
        `${API_BASE_URL}/rfidchips/quick-register`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({ uid }),
        }
      );

      if (response.ok) {
        const data = await response.json();
        // Mettre à jour le statut en "success"
        setScannedChips((prev) =>
          prev.map((chip) =>
            chip.uid === uid
              ? {
                  ...chip,
                  status: 'success',
                  chipId: data.chip.chipId,
                  message: '✅ Enregistrée',
                }
              : chip
          )
        );
      } else if (response.status === 409) {
        // Conflit = déjà enregistrée
        const data = await response.json();
        setScannedChips((prev) =>
          prev.map((chip) =>
            chip.uid === uid
              ? {
                  ...chip,
                  status: 'duplicate',
                  chipId: data.chip.chipId,
                  message: '⚠️ Déjà enregistrée',
                }
              : chip
          )
        );
      } else {
        // Erreur
        setScannedChips((prev) =>
          prev.map((chip) =>
            chip.uid === uid
              ? {
                  ...chip,
                  status: 'error',
                  message: '❌ Erreur serveur',
                }
              : chip
          )
        );
      }
    } catch (error) {
      console.error('[REGISTER] Erreur enregistrement:', error);
      setScannedChips((prev) =>
        prev.map((chip) =>
          chip.uid === uid
            ? {
                ...chip,
                status: 'error',
                message: '❌ Erreur connexion',
              }
            : chip
        )
      );
    }
  };

  // ==========================================================================
  // RENDU
  // ==========================================================================

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'scanning':
        return '#3b82f6';
      case 'success':
        return '#10b981';
      case 'duplicate':
        return '#fbbf24';
      case 'error':
        return '#ef4444';
      default:
        return '#64748b';
    }
  };

  return (
    <View style={styles.container}>
      {/* HEADER */}
      <View style={styles.header}>
        <Text style={styles.headerTitle}>Enregistrement Puces</Text>
        <Text style={styles.headerSubtitle}>
          {scannedChips.filter((c) => c.status === 'success').length} puces
          enregistrées
        </Text>
      </View>

      {/* BOUTON SCAN */}
      <View style={styles.scanContainer}>
        <TouchableOpacity
          style={[styles.scanButton, isScanning && styles.scanButtonActive]}
          onPress={scanChip}
          disabled={isScanning}
        >
          <Text style={styles.scanButtonIcon}>
            {isScanning ? '📡' : '📱'}
          </Text>
          <Text style={styles.scanButtonText}>
            {isScanning ? 'Scan en cours...' : 'SCANNER UNE PUCE'}
          </Text>
        </TouchableOpacity>

        <Text style={styles.instructions}>
          Approchez une puce NFC de votre téléphone
        </Text>
      </View>

      {/* LISTE DES PUCES SCANNÉES */}
      <View style={styles.listContainer}>
        <Text style={styles.listTitle}>
          Puces scannées ({scannedChips.length})
        </Text>

        <FlatList
          data={scannedChips}
          keyExtractor={(item, index) => `${item.uid}-${index}`}
          renderItem={({ item }) => (
            <View
              style={[
                styles.chipCard,
                { borderLeftColor: getStatusColor(item.status), borderLeftWidth: 4 },
              ]}
            >
              <View style={styles.chipHeader}>
                <Text style={styles.chipUid}>{item.uid}</Text>
                <Text style={styles.chipTime}>{item.timestamp}</Text>
              </View>

              {item.chipId && (
                <Text style={styles.chipId}>ID: {item.chipId}</Text>
              )}

              {item.message && (
                <Text
                  style={[
                    styles.chipMessage,
                    { color: getStatusColor(item.status) },
                  ]}
                >
                  {item.message}
                </Text>
              )}
            </View>
          )}
          ListEmptyComponent={
            <View style={styles.emptyContainer}>
              <Text style={styles.emptyText}>Aucune puce scannée</Text>
              <Text style={styles.emptyHint}>
                Utilisez le bouton ci-dessus pour commencer
              </Text>
            </View>
          }
        />
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
  header: {
    backgroundColor: '#2563eb',
    padding: 20,
    paddingTop: 60,
  },
  headerTitle: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#fff',
  },
  headerSubtitle: {
    fontSize: 16,
    color: '#93c5fd',
    marginTop: 4,
  },
  scanContainer: {
    padding: 24,
    alignItems: 'center',
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#e2e8f0',
  },
  scanButton: {
    backgroundColor: '#2563eb',
    paddingVertical: 24,
    paddingHorizontal: 48,
    borderRadius: 16,
    alignItems: 'center',
    justifyContent: 'center',
    elevation: 4,
    shadowColor: '#2563eb',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
  },
  scanButtonActive: {
    backgroundColor: '#1e40af',
  },
  scanButtonIcon: {
    fontSize: 48,
    marginBottom: 12,
  },
  scanButtonText: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#fff',
  },
  instructions: {
    fontSize: 14,
    color: '#64748b',
    marginTop: 16,
    textAlign: 'center',
  },
  listContainer: {
    flex: 1,
    padding: 16,
  },
  listTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#1e293b',
    marginBottom: 12,
  },
  chipCard: {
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    elevation: 2,
  },
  chipHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  chipUid: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#1e293b',
    fontFamily: 'monospace',
  },
  chipTime: {
    fontSize: 12,
    color: '#94a3b8',
  },
  chipId: {
    fontSize: 14,
    color: '#64748b',
    marginBottom: 4,
    fontFamily: 'monospace',
  },
  chipMessage: {
    fontSize: 14,
    fontWeight: '600',
    marginTop: 4,
  },
  emptyContainer: {
    alignItems: 'center',
    paddingVertical: 48,
  },
  emptyText: {
    fontSize: 18,
    color: '#64748b',
    fontWeight: '600',
  },
  emptyHint: {
    fontSize: 14,
    color: '#94a3b8',
    marginTop: 8,
  },
});
