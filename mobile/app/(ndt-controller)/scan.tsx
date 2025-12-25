/**
 * NDT Controller - NFC Scan Screen
 *
 * Scan weld NFC tag to execute NDT control
 * Select control type and record results
 */

import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Alert,
  ScrollView,
  TextInput
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useAuth } from '@/contexts/AuthContext';
import NfcScanButton from '@/components/shared/NfcScanButton';
import LoadingSpinner from '@/components/shared/LoadingSpinner';
import { apiClient } from '@/services/api';

interface WeldForControl {
  id: string;
  reference: string;
  assetName: string;
  executionDate: string | null;
  pendingControls: PendingControl[];
}

interface PendingControl {
  id: string;
  controlType: 'VT' | 'PT' | 'MT' | 'RT' | 'UT';
  status: string;
  acceptanceCriteria: string | null;
}

type ControlResult = 'PASSED' | 'FAILED';

const CONTROL_TYPE_CONFIG = {
  VT: { label: 'Visuel (VT)', color: '#22c55e', icon: 'eye' },
  PT: { label: 'Ressuage (PT)', color: '#f59e0b', icon: 'water' },
  MT: { label: 'Magnétoscopie (MT)', color: '#8b5cf6', icon: 'magnet' },
  RT: { label: 'Radiographie (RT)', color: '#ef4444', icon: 'radio' },
  UT: { label: 'Ultrasons (UT)', color: '#3b82f6', icon: 'pulse' }
};

export default function NDTScanScreen() {
  const { token } = useAuth();

  const [loading, setLoading] = useState(false);
  const [scannedWeld, setScannedWeld] = useState<WeldForControl | null>(null);
  const [selectedControl, setSelectedControl] = useState<PendingControl | null>(null);
  const [result, setResult] = useState<ControlResult | null>(null);
  const [observations, setObservations] = useState('');
  const [defectsDescription, setDefectsDescription] = useState('');

  const handleNfcScan = async (tagData: { weldId?: string }) => {
    if (!tagData.weldId) {
      Alert.alert('Tag invalide', 'Ce tag NFC ne contient pas de référence de soudure.');
      return;
    }

    setLoading(true);
    try {
      const response = await apiClient.get(`/welds/${tagData.weldId}/pending-controls`, {
        headers: { Authorization: `Bearer ${token}` }
      });

      const weld = response.data as WeldForControl;
      setScannedWeld(weld);

      if (weld.pendingControls.length === 0) {
        Alert.alert('Information', 'Aucun contrôle CND en attente pour cette soudure.');
      }
    } catch (error) {
      console.error('Error fetching weld controls:', error);
      Alert.alert('Erreur', 'Impossible de récupérer les contrôles de la soudure.');
    } finally {
      setLoading(false);
    }
  };

  const selectControl = (control: PendingControl) => {
    setSelectedControl(control);
    setResult(null);
    setObservations('');
    setDefectsDescription('');
  };

  const submitControlResult = async () => {
    if (!selectedControl || !result) return;

    if (result === 'FAILED' && !defectsDescription.trim()) {
      Alert.alert('Attention', 'Veuillez décrire les défauts constatés pour un contrôle non conforme.');
      return;
    }

    setLoading(true);
    try {
      await apiClient.post(`/ndt-controls/${selectedControl.id}/execute`, {
        result,
        observations: observations || null,
        defectsFound: result === 'FAILED' ? defectsDescription : null
      }, {
        headers: { Authorization: `Bearer ${token}` }
      });

      const message = result === 'PASSED'
        ? 'Le contrôle a été enregistré comme conforme.'
        : 'Une Fiche de Non-Conformité (FNC) a été créée automatiquement.';

      Alert.alert(
        result === 'PASSED' ? 'Contrôle validé' : 'Non-conformité enregistrée',
        message,
        [{
          text: 'OK',
          onPress: () => {
            setScannedWeld(null);
            setSelectedControl(null);
            setResult(null);
            setObservations('');
            setDefectsDescription('');
          }
        }]
      );
    } catch (error: any) {
      Alert.alert('Erreur', error.response?.data || 'Impossible d\'enregistrer le contrôle.');
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    setScannedWeld(null);
    setSelectedControl(null);
    setResult(null);
    setObservations('');
    setDefectsDescription('');
  };

  if (loading) {
    return <LoadingSpinner message="Traitement en cours..." />;
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      {/* Header */}
      <View style={styles.header}>
        <Ionicons name="scan-circle" size={48} color="#3b82f6" />
        <Text style={styles.title}>Contrôle CND</Text>
        <Text style={styles.subtitle}>
          {!scannedWeld && 'Scannez le tag NFC de la soudure'}
          {scannedWeld && !selectedControl && 'Sélectionnez le contrôle à effectuer'}
          {selectedControl && 'Enregistrez le résultat du contrôle'}
        </Text>
      </View>

      {/* Initial scan state */}
      {!scannedWeld && (
        <View style={styles.scanSection}>
          <NfcScanButton
            onScan={handleNfcScan}
            buttonText="Scanner une soudure"
            buttonColor="#3b82f6"
          />

          <View style={styles.instructions}>
            <Text style={styles.instructionTitle}>Types de contrôles:</Text>
            {Object.entries(CONTROL_TYPE_CONFIG).map(([type, config]) => (
              <View key={type} style={styles.instructionItem}>
                <Ionicons name={config.icon as any} size={16} color={config.color} />
                <Text style={styles.instructionText}>{config.label}</Text>
              </View>
            ))}
          </View>
        </View>
      )}

      {/* Weld scanned - select control */}
      {scannedWeld && !selectedControl && (
        <View style={styles.selectSection}>
          <View style={styles.weldCard}>
            <Text style={styles.weldReference}>{scannedWeld.reference}</Text>
            <Text style={styles.weldAsset}>{scannedWeld.assetName}</Text>
            {scannedWeld.executionDate && (
              <View style={styles.executionInfo}>
                <Ionicons name="flame" size={14} color="#f97316" />
                <Text style={styles.executionText}>
                  Soudé le {new Date(scannedWeld.executionDate).toLocaleDateString('fr-FR')}
                </Text>
              </View>
            )}
          </View>

          <Text style={styles.sectionTitle}>
            Contrôles en attente ({scannedWeld.pendingControls.length})
          </Text>

          {scannedWeld.pendingControls.map((control) => {
            const config = CONTROL_TYPE_CONFIG[control.controlType];
            return (
              <TouchableOpacity
                key={control.id}
                style={styles.controlOption}
                onPress={() => selectControl(control)}
              >
                <View style={[styles.controlIcon, { backgroundColor: config.color }]}>
                  <Ionicons name={config.icon as any} size={24} color="#fff" />
                </View>
                <View style={styles.controlInfo}>
                  <Text style={styles.controlType}>{config.label}</Text>
                  {control.acceptanceCriteria && (
                    <Text style={styles.controlCriteria}>
                      Critères: {control.acceptanceCriteria}
                    </Text>
                  )}
                </View>
                <Ionicons name="chevron-forward" size={24} color="#64748b" />
              </TouchableOpacity>
            );
          })}

          <TouchableOpacity style={styles.cancelButton} onPress={handleCancel}>
            <Text style={styles.cancelButtonText}>Annuler</Text>
          </TouchableOpacity>
        </View>
      )}

      {/* Control selected - record result */}
      {selectedControl && (
        <View style={styles.resultSection}>
          <View style={styles.selectedControlCard}>
            <View style={[
              styles.controlIcon,
              { backgroundColor: CONTROL_TYPE_CONFIG[selectedControl.controlType].color }
            ]}>
              <Ionicons
                name={CONTROL_TYPE_CONFIG[selectedControl.controlType].icon as any}
                size={24}
                color="#fff"
              />
            </View>
            <View style={styles.controlInfo}>
              <Text style={styles.controlType}>
                {CONTROL_TYPE_CONFIG[selectedControl.controlType].label}
              </Text>
              <Text style={styles.weldRefSmall}>{scannedWeld?.reference}</Text>
            </View>
          </View>

          {selectedControl.acceptanceCriteria && (
            <View style={styles.criteriaCard}>
              <Ionicons name="document-text" size={16} color="#3b82f6" />
              <Text style={styles.criteriaTitle}>Critères d'acceptation:</Text>
              <Text style={styles.criteriaText}>{selectedControl.acceptanceCriteria}</Text>
            </View>
          )}

          <Text style={styles.sectionTitle}>Résultat du contrôle</Text>

          <View style={styles.resultButtons}>
            <TouchableOpacity
              style={[
                styles.resultButton,
                styles.passedButton,
                result === 'PASSED' && styles.resultButtonSelected
              ]}
              onPress={() => setResult('PASSED')}
            >
              <Ionicons
                name="checkmark-circle"
                size={32}
                color={result === 'PASSED' ? '#fff' : '#22c55e'}
              />
              <Text style={[
                styles.resultButtonText,
                result === 'PASSED' && styles.resultButtonTextSelected
              ]}>
                Conforme
              </Text>
            </TouchableOpacity>

            <TouchableOpacity
              style={[
                styles.resultButton,
                styles.failedButton,
                result === 'FAILED' && styles.resultButtonSelectedFailed
              ]}
              onPress={() => setResult('FAILED')}
            >
              <Ionicons
                name="close-circle"
                size={32}
                color={result === 'FAILED' ? '#fff' : '#ef4444'}
              />
              <Text style={[
                styles.resultButtonText,
                styles.failedButtonText,
                result === 'FAILED' && styles.resultButtonTextSelected
              ]}>
                Non Conforme
              </Text>
            </TouchableOpacity>
          </View>

          {/* Defects description for failed controls */}
          {result === 'FAILED' && (
            <View style={styles.inputSection}>
              <Text style={styles.inputLabel}>Description des défauts *</Text>
              <TextInput
                style={styles.textInput}
                placeholder="Décrivez les défauts constatés..."
                placeholderTextColor="#64748b"
                value={defectsDescription}
                onChangeText={setDefectsDescription}
                multiline
                numberOfLines={4}
              />
            </View>
          )}

          {/* Observations */}
          <View style={styles.inputSection}>
            <Text style={styles.inputLabel}>Observations (optionnel)</Text>
            <TextInput
              style={styles.textInput}
              placeholder="Remarques supplémentaires..."
              placeholderTextColor="#64748b"
              value={observations}
              onChangeText={setObservations}
              multiline
              numberOfLines={3}
            />
          </View>

          {/* Action buttons */}
          <View style={styles.actionButtons}>
            <TouchableOpacity
              style={styles.backButton}
              onPress={() => setSelectedControl(null)}
            >
              <Text style={styles.backButtonText}>Retour</Text>
            </TouchableOpacity>

            <TouchableOpacity
              style={[
                styles.submitButton,
                !result && styles.submitButtonDisabled
              ]}
              onPress={submitControlResult}
              disabled={!result}
            >
              <Ionicons name="checkmark" size={20} color="#fff" />
              <Text style={styles.submitButtonText}>Valider</Text>
            </TouchableOpacity>
          </View>
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
    paddingTop: 40
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
  selectSection: {
    gap: 16
  },
  weldCard: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    borderWidth: 1,
    borderColor: '#334155'
  },
  weldReference: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#3b82f6'
  },
  weldAsset: {
    fontSize: 14,
    color: '#94a3b8',
    marginTop: 4
  },
  executionInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    marginTop: 12
  },
  executionText: {
    fontSize: 13,
    color: '#f97316'
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#f1f5f9',
    marginTop: 8
  },
  controlOption: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    gap: 12
  },
  controlIcon: {
    width: 48,
    height: 48,
    borderRadius: 24,
    alignItems: 'center',
    justifyContent: 'center'
  },
  controlInfo: {
    flex: 1
  },
  controlType: {
    fontSize: 16,
    fontWeight: '600',
    color: '#f1f5f9'
  },
  controlCriteria: {
    fontSize: 12,
    color: '#64748b',
    marginTop: 4
  },
  cancelButton: {
    backgroundColor: '#334155',
    borderRadius: 12,
    padding: 16,
    alignItems: 'center',
    marginTop: 8
  },
  cancelButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#f1f5f9'
  },
  resultSection: {
    gap: 16
  },
  selectedControlCard: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    gap: 12
  },
  weldRefSmall: {
    fontSize: 13,
    color: '#64748b',
    marginTop: 2
  },
  criteriaCard: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    borderLeftWidth: 3,
    borderLeftColor: '#3b82f6'
  },
  criteriaTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#f1f5f9',
    marginTop: 8
  },
  criteriaText: {
    fontSize: 14,
    color: '#94a3b8',
    marginTop: 4
  },
  resultButtons: {
    flexDirection: 'row',
    gap: 12
  },
  resultButton: {
    flex: 1,
    alignItems: 'center',
    padding: 20,
    borderRadius: 12,
    borderWidth: 2
  },
  passedButton: {
    backgroundColor: '#22c55e15',
    borderColor: '#22c55e'
  },
  failedButton: {
    backgroundColor: '#ef444415',
    borderColor: '#ef4444'
  },
  resultButtonSelected: {
    backgroundColor: '#22c55e'
  },
  resultButtonSelectedFailed: {
    backgroundColor: '#ef4444'
  },
  resultButtonText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#22c55e',
    marginTop: 8
  },
  failedButtonText: {
    color: '#ef4444'
  },
  resultButtonTextSelected: {
    color: '#fff'
  },
  inputSection: {
    gap: 8
  },
  inputLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: '#f1f5f9'
  },
  textInput: {
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
  actionButtons: {
    flexDirection: 'row',
    gap: 12,
    marginTop: 8
  },
  backButton: {
    flex: 1,
    backgroundColor: '#334155',
    borderRadius: 12,
    padding: 16,
    alignItems: 'center'
  },
  backButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#f1f5f9'
  },
  submitButton: {
    flex: 2,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    backgroundColor: '#3b82f6',
    borderRadius: 12,
    padding: 16
  },
  submitButtonDisabled: {
    backgroundColor: '#475569'
  },
  submitButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#fff'
  }
});
