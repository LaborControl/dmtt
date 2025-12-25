/**
 * Welder - NFC Scan Screen
 *
 * Double-scan workflow for weld execution:
 * 1. First scan = Start welding (records FirstScanAt)
 * 2. Second scan = End welding (records SecondScanAt, ExecutionDate)
 */

import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Alert,
  TextInput,
  ScrollView
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useAuth } from '@/contexts/AuthContext';
import NfcScanButton from '@/components/shared/NfcScanButton';
import LoadingSpinner from '@/components/shared/LoadingSpinner';
import { apiClient } from '@/services/api';

interface ScannedWeld {
  id: string;
  reference: string;
  assetName: string;
  weldingProcess: string;
  status: string;
  isCCPUValidated: boolean;
  isBlocked: boolean;
  firstScanAt: string | null;
  secondScanAt: string | null;
  ccpuValidatorName: string | null;
}

type ScanMode = 'idle' | 'scanning' | 'first_scan' | 'second_scan' | 'completed';

export default function ScanScreen() {
  const { token } = useAuth();

  const [mode, setMode] = useState<ScanMode>('idle');
  const [scannedWeld, setScannedWeld] = useState<ScannedWeld | null>(null);
  const [loading, setLoading] = useState(false);
  const [observations, setObservations] = useState('');

  // Handle NFC scan result
  const handleNfcScan = async (tagData: { weldId?: string; assetId?: string }) => {
    if (!tagData.weldId) {
      Alert.alert('Tag invalide', 'Ce tag NFC ne contient pas de référence de soudure.');
      return;
    }

    setLoading(true);
    try {
      // Fetch weld details
      const response = await apiClient.get(`/welds/${tagData.weldId}`, {
        headers: { Authorization: `Bearer ${token}` }
      });

      const weld = response.data as ScannedWeld;
      setScannedWeld(weld);

      // Determine scan mode
      if (weld.isBlocked) {
        Alert.alert('Soudure bloquée', 'Cette soudure est bloquée et ne peut pas être exécutée.');
        setMode('idle');
      } else if (!weld.isCCPUValidated) {
        Alert.alert('Non validée', 'Cette soudure n\'a pas été validée par le CCPU.');
        setMode('idle');
      } else if (weld.secondScanAt) {
        Alert.alert('Déjà terminée', 'Cette soudure a déjà été exécutée.');
        setMode('completed');
      } else if (weld.firstScanAt) {
        setMode('second_scan');
      } else {
        setMode('first_scan');
      }
    } catch (error) {
      console.error('Error fetching weld:', error);
      Alert.alert('Erreur', 'Impossible de récupérer les informations de la soudure.');
      setMode('idle');
    } finally {
      setLoading(false);
    }
  };

  // Confirm first scan (start welding)
  const confirmFirstScan = async () => {
    if (!scannedWeld) return;

    setLoading(true);
    try {
      await apiClient.post(`/welds/${scannedWeld.id}/execution-scan`, {
        isFirstScan: true
      }, {
        headers: { Authorization: `Bearer ${token}` }
      });

      Alert.alert(
        'Début enregistré',
        'Le début de soudage a été enregistré. Scannez à nouveau une fois terminé.',
        [{ text: 'OK', onPress: () => setMode('idle') }]
      );
      setScannedWeld(null);
    } catch (error: any) {
      Alert.alert('Erreur', error.response?.data || 'Impossible d\'enregistrer le scan');
    } finally {
      setLoading(false);
    }
  };

  // Confirm second scan (end welding)
  const confirmSecondScan = async () => {
    if (!scannedWeld) return;

    setLoading(true);
    try {
      await apiClient.post(`/welds/${scannedWeld.id}/execution-scan`, {
        isFirstScan: false,
        welderObservations: observations || null
      }, {
        headers: { Authorization: `Bearer ${token}` }
      });

      Alert.alert(
        'Soudure terminée',
        'L\'exécution de la soudure a été enregistrée. Elle est maintenant en attente de contrôle CND.',
        [{ text: 'OK', onPress: () => {
          setMode('idle');
          setScannedWeld(null);
          setObservations('');
        }}]
      );
    } catch (error: any) {
      Alert.alert('Erreur', error.response?.data || 'Impossible d\'enregistrer le scan');
    } finally {
      setLoading(false);
    }
  };

  // Cancel current operation
  const handleCancel = () => {
    setMode('idle');
    setScannedWeld(null);
    setObservations('');
  };

  // Loading state
  if (loading) {
    return <LoadingSpinner message="Traitement en cours..." />;
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      {/* Header */}
      <View style={styles.header}>
        <Ionicons name="scan-circle" size={48} color="#f97316" />
        <Text style={styles.title}>Scanner NFC</Text>
        <Text style={styles.subtitle}>
          {mode === 'idle' && 'Scannez le tag NFC de la soudure'}
          {mode === 'first_scan' && 'Confirmez le début du soudage'}
          {mode === 'second_scan' && 'Confirmez la fin du soudage'}
          {mode === 'completed' && 'Soudure déjà terminée'}
        </Text>
      </View>

      {/* Idle mode - Show scan button */}
      {mode === 'idle' && (
        <View style={styles.scanSection}>
          <NfcScanButton
            onScan={handleNfcScan}
            buttonText="Scanner une soudure"
            buttonColor="#f97316"
          />

          <View style={styles.instructions}>
            <Text style={styles.instructionTitle}>Instructions:</Text>
            <View style={styles.instructionItem}>
              <Ionicons name="radio-button-on" size={16} color="#f97316" />
              <Text style={styles.instructionText}>
                1er scan = Début de soudage
              </Text>
            </View>
            <View style={styles.instructionItem}>
              <Ionicons name="checkmark-circle" size={16} color="#22c55e" />
              <Text style={styles.instructionText}>
                2ème scan = Fin de soudage
              </Text>
            </View>
          </View>
        </View>
      )}

      {/* First scan confirmation */}
      {mode === 'first_scan' && scannedWeld && (
        <View style={styles.confirmSection}>
          <View style={styles.weldCard}>
            <Text style={styles.weldReference}>{scannedWeld.reference}</Text>
            <Text style={styles.weldInfo}>{scannedWeld.assetName}</Text>
            <Text style={styles.weldInfo}>{scannedWeld.weldingProcess}</Text>

            {scannedWeld.ccpuValidatorName && (
              <View style={styles.validationInfo}>
                <Ionicons name="checkmark-shield" size={16} color="#22c55e" />
                <Text style={styles.validatedText}>
                  Validé CCPU: {scannedWeld.ccpuValidatorName}
                </Text>
              </View>
            )}
          </View>

          <View style={styles.scanTypeIndicator}>
            <Ionicons name="play-circle" size={64} color="#f97316" />
            <Text style={styles.scanTypeText}>DÉBUT DE SOUDAGE</Text>
          </View>

          <View style={styles.buttonRow}>
            <TouchableOpacity style={styles.cancelButton} onPress={handleCancel}>
              <Text style={styles.cancelButtonText}>Annuler</Text>
            </TouchableOpacity>
            <TouchableOpacity style={styles.confirmButton} onPress={confirmFirstScan}>
              <Ionicons name="flame" size={20} color="#fff" />
              <Text style={styles.confirmButtonText}>Commencer</Text>
            </TouchableOpacity>
          </View>
        </View>
      )}

      {/* Second scan confirmation */}
      {mode === 'second_scan' && scannedWeld && (
        <View style={styles.confirmSection}>
          <View style={styles.weldCard}>
            <Text style={styles.weldReference}>{scannedWeld.reference}</Text>
            <Text style={styles.weldInfo}>{scannedWeld.assetName}</Text>
            <Text style={styles.weldInfo}>{scannedWeld.weldingProcess}</Text>

            <View style={styles.progressInfo}>
              <Ionicons name="time" size={16} color="#f97316" />
              <Text style={styles.progressText}>
                Démarré: {new Date(scannedWeld.firstScanAt!).toLocaleTimeString('fr-FR')}
              </Text>
            </View>
          </View>

          <View style={styles.scanTypeIndicator}>
            <Ionicons name="stop-circle" size={64} color="#22c55e" />
            <Text style={styles.scanTypeTextGreen}>FIN DE SOUDAGE</Text>
          </View>

          {/* Observations input */}
          <View style={styles.observationsSection}>
            <Text style={styles.observationsLabel}>Observations (optionnel):</Text>
            <TextInput
              style={styles.observationsInput}
              placeholder="Remarques sur l'exécution..."
              placeholderTextColor="#64748b"
              value={observations}
              onChangeText={setObservations}
              multiline
              numberOfLines={3}
            />
          </View>

          <View style={styles.buttonRow}>
            <TouchableOpacity style={styles.cancelButton} onPress={handleCancel}>
              <Text style={styles.cancelButtonText}>Annuler</Text>
            </TouchableOpacity>
            <TouchableOpacity style={styles.confirmButtonGreen} onPress={confirmSecondScan}>
              <Ionicons name="checkmark" size={20} color="#fff" />
              <Text style={styles.confirmButtonText}>Terminer</Text>
            </TouchableOpacity>
          </View>
        </View>
      )}

      {/* Completed state */}
      {mode === 'completed' && scannedWeld && (
        <View style={styles.completedSection}>
          <Ionicons name="checkmark-circle" size={80} color="#22c55e" />
          <Text style={styles.completedTitle}>Soudure Terminée</Text>
          <Text style={styles.completedText}>
            {scannedWeld.reference} a déjà été exécutée.
          </Text>
          <TouchableOpacity style={styles.newScanButton} onPress={handleCancel}>
            <Text style={styles.newScanButtonText}>Scanner une autre soudure</Text>
          </TouchableOpacity>
        </View>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#0f172a'
  },
  content: {
    padding: 20,
    paddingTop: 60
  },
  header: {
    alignItems: 'center',
    marginBottom: 32
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#f1f5f9',
    marginTop: 12
  },
  subtitle: {
    fontSize: 16,
    color: '#94a3b8',
    marginTop: 8,
    textAlign: 'center'
  },
  scanSection: {
    alignItems: 'center',
    gap: 32
  },
  instructions: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    width: '100%',
    gap: 12
  },
  instructionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#f1f5f9',
    marginBottom: 8
  },
  instructionItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12
  },
  instructionText: {
    fontSize: 14,
    color: '#94a3b8'
  },
  confirmSection: {
    gap: 24
  },
  weldCard: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 20,
    borderWidth: 1,
    borderColor: '#334155'
  },
  weldReference: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#f97316',
    marginBottom: 8
  },
  weldInfo: {
    fontSize: 16,
    color: '#94a3b8',
    marginBottom: 4
  },
  validationInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: '#334155'
  },
  validatedText: {
    fontSize: 14,
    color: '#22c55e'
  },
  progressInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: '#334155'
  },
  progressText: {
    fontSize: 14,
    color: '#f97316'
  },
  scanTypeIndicator: {
    alignItems: 'center',
    paddingVertical: 24
  },
  scanTypeText: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#f97316',
    marginTop: 12
  },
  scanTypeTextGreen: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#22c55e',
    marginTop: 12
  },
  observationsSection: {
    gap: 8
  },
  observationsLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: '#f1f5f9'
  },
  observationsInput: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    color: '#f1f5f9',
    fontSize: 14,
    borderWidth: 1,
    borderColor: '#334155',
    textAlignVertical: 'top',
    minHeight: 80
  },
  buttonRow: {
    flexDirection: 'row',
    gap: 12
  },
  cancelButton: {
    flex: 1,
    backgroundColor: '#334155',
    borderRadius: 12,
    padding: 16,
    alignItems: 'center'
  },
  cancelButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#f1f5f9'
  },
  confirmButton: {
    flex: 2,
    backgroundColor: '#f97316',
    borderRadius: 12,
    padding: 16,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8
  },
  confirmButtonGreen: {
    flex: 2,
    backgroundColor: '#22c55e',
    borderRadius: 12,
    padding: 16,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8
  },
  confirmButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#fff'
  },
  completedSection: {
    alignItems: 'center',
    paddingVertical: 40
  },
  completedTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#22c55e',
    marginTop: 16
  },
  completedText: {
    fontSize: 16,
    color: '#94a3b8',
    marginTop: 8
  },
  newScanButton: {
    backgroundColor: '#334155',
    borderRadius: 12,
    paddingVertical: 12,
    paddingHorizontal: 24,
    marginTop: 24
  },
  newScanButtonText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#f1f5f9'
  }
});
