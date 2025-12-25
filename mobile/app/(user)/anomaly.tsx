/**
 * Anomaly Declaration Screen (USER role)
 *
 * Allows users to scan any NFC chip and declare an anomaly
 * without needing a scheduled task
 */

import React, { useState, useEffect } from 'react';
import {
  StyleSheet,
  Text,
  View,
  TouchableOpacity,
  TextInput,
  ScrollView,
  Alert,
  Modal,
  Image,
  ActivityIndicator,
} from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { useAuth } from '@/contexts/AuthContext';
import { scanNfcTag } from '@/services/nfc/nfcService';
import { getControlPointByUid, createAnomaly, ControlPoint } from '@/services/api/apiService';
import { useOfflineQueue } from '@/store/offlineQueue';

// ============================================================================
// COMPONENT
// ============================================================================

export default function AnomalyScreen() {
  const { user, token } = useAuth();
  const { enqueue, isOnline } = useOfflineQueue();

  // ==========================================================================
  // STATE
  // ==========================================================================
  const [scanning, setScanning] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [controlPoint, setControlPoint] = useState<ControlPoint | null>(null);
  const [severity, setSeverity] = useState<'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL'>('MEDIUM');
  const [description, setDescription] = useState('');
  const [photo, setPhoto] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  // ==========================================================================
  // FUNCTION: Start NFC scan
  // ==========================================================================
  const handleScanStart = () => {
    setScanning(true);
    performScan();
  };

  // ==========================================================================
  // FUNCTION: Perform NFC scan
  // ==========================================================================
  const performScan = async () => {
    try {
      const uid = await scanNfcTag();

      // Fetch control point info
      if (!token || !user) {
        Alert.alert('Erreur', 'Non authentifié');
        setScanning(false);
        return;
      }

      const cp = await getControlPointByUid(uid, token);

      setControlPoint(cp);
      setScanning(false);
      setShowForm(true);
    } catch (error: any) {
      setScanning(false);

      if (error.message === 'SCAN_CANCELLED') {
        return;
      }

      if (error.status === 404) {
        Alert.alert('Puce inconnue', 'Cette puce NFC n\'est pas enregistrée dans le système');
      } else {
        Alert.alert('Erreur', 'Impossible de lire la puce NFC');
      }
    }
  };

  // ==========================================================================
  // FUNCTION: Take photo
  // ==========================================================================
  const takePhoto = async () => {
    try {
      const { status } = await ImagePicker.requestCameraPermissionsAsync();
      if (status !== 'granted') {
        Alert.alert('Permission refusée', 'Accès à la caméra nécessaire');
        return;
      }

      const result = await ImagePicker.launchCameraAsync({
        allowsEditing: true,
        quality: 0.7,
        base64: true,
      });

      if (!result.canceled && result.assets[0].base64) {
        setPhoto(result.assets[0].base64);
      }
    } catch (error) {
      console.error('[ANOMALY] Photo error:', error);
      Alert.alert('Erreur', 'Impossible de prendre la photo');
    }
  };

  // ==========================================================================
  // FUNCTION: Submit anomaly
  // ==========================================================================
  const handleSubmit = async () => {
    if (!description.trim()) {
      Alert.alert('Champ requis', 'Veuillez décrire l\'anomalie');
      return;
    }

    if (!controlPoint || !token || !user) {
      Alert.alert('Erreur', 'Informations manquantes');
      return;
    }

    setSubmitting(true);

    const payload = {
      userId: user.id,
      controlPointId: controlPoint.id,
      detectedAt: new Date().toISOString(),
      severity,
      description: description.trim(),
      photoUrl: photo,
    };

    try {
      if (!isOnline) {
        // Offline: Queue the action
        console.log('[ANOMALY] Offline, queueing anomaly...');
        enqueue('ANOMALY', payload);

        Alert.alert(
          'Anomalie mise en file d\'attente',
          'Vous êtes hors ligne. L\'anomalie sera synchronisée automatiquement au retour du réseau.'
        );
      } else {
        // Online: Submit immediately
        await createAnomaly(payload, token);
        Alert.alert('Anomalie enregistrée', 'L\'anomalie a été signalée avec succès');
      }

      // Reset form
      setShowForm(false);
      setControlPoint(null);
      setSeverity('MEDIUM');
      setDescription('');
      setPhoto(null);
    } catch (error: any) {
      console.error('[ANOMALY] Submit error:', error);

      // If online submission fails, queue it
      if (isOnline) {
        console.log('[ANOMALY] Online submission failed, queueing...');
        enqueue('ANOMALY', payload);
        Alert.alert(
          'Anomalie mise en file d\'attente',
          'L\'envoi a échoué mais l\'anomalie sera réessayée automatiquement.'
        );

        // Still reset form
        setShowForm(false);
        setControlPoint(null);
        setSeverity('MEDIUM');
        setDescription('');
        setPhoto(null);
      } else {
        Alert.alert('Erreur', error.message || 'Impossible d\'enregistrer l\'anomalie');
      }
    } finally {
      setSubmitting(false);
    }
  };

  // ==========================================================================
  // FUNCTION: Cancel
  // ==========================================================================
  const handleCancel = () => {
    setShowForm(false);
    setControlPoint(null);
    setSeverity('MEDIUM');
    setDescription('');
    setPhoto(null);
  };

  // ==========================================================================
  // RENDER: Scanning Modal
  // ==========================================================================
  const renderScanModal = () => {
    if (!scanning) return null;

    return (
      <Modal visible={scanning} transparent animationType="fade" onRequestClose={() => setScanning(false)}>
        <View style={styles.scanModalOverlay}>
          <View style={styles.scanModalContainer}>
            <Text style={styles.scanModalIcon}>📱</Text>
            <Text style={styles.scanModalTitle}>Scanner l'étiquette NFC</Text>
            <Text style={styles.scanModalMessage}>Approchez votre téléphone de la puce</Text>

            <View style={styles.scanModalAnimation}>
              <View style={styles.scanPulse} />
            </View>

            <TouchableOpacity style={styles.scanCancelButton} onPress={() => setScanning(false)}>
              <Text style={styles.scanCancelButtonText}>Annuler</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>
    );
  };

  // ==========================================================================
  // RENDER: Form Modal
  // ==========================================================================
  const renderForm = () => {
    if (!showForm || !controlPoint) return null;

    return (
      <Modal visible={showForm} animationType="slide" onRequestClose={handleCancel}>
        <View style={styles.container}>
          <ScrollView contentContainerStyle={styles.scrollContent}>
            {/* HEADER */}
            <View style={styles.header}>
              <View>
                <Text style={styles.headerTitle}>Déclaration d'anomalie</Text>
                <Text style={styles.headerSubtitle}>{controlPoint.name}</Text>
              </View>
              <TouchableOpacity style={styles.closeButton} onPress={handleCancel}>
                <Text style={styles.closeButtonText}>✕</Text>
              </TouchableOpacity>
            </View>

            <View style={styles.formContainer}>
              {/* LOCATION */}
              <View style={styles.infoCard}>
                <Text style={styles.infoLabel}>Emplacement</Text>
                <Text style={styles.infoValue}>{controlPoint.locationDescription}</Text>
              </View>

              {/* SEVERITY */}
              <Text style={styles.label}>Gravité</Text>
              <View style={styles.severityGroup}>
                <TouchableOpacity
                  style={[styles.severityButton, severity === 'LOW' && styles.severityLow]}
                  onPress={() => setSeverity('LOW')}
                >
                  <Text style={[styles.severityText, severity === 'LOW' && styles.severityTextSelected]}>
                    Faible
                  </Text>
                </TouchableOpacity>

                <TouchableOpacity
                  style={[styles.severityButton, severity === 'MEDIUM' && styles.severityMedium]}
                  onPress={() => setSeverity('MEDIUM')}
                >
                  <Text style={[styles.severityText, severity === 'MEDIUM' && styles.severityTextSelected]}>
                    Moyenne
                  </Text>
                </TouchableOpacity>

                <TouchableOpacity
                  style={[styles.severityButton, severity === 'HIGH' && styles.severityHigh]}
                  onPress={() => setSeverity('HIGH')}
                >
                  <Text style={[styles.severityText, severity === 'HIGH' && styles.severityTextSelected]}>
                    Élevée
                  </Text>
                </TouchableOpacity>

                <TouchableOpacity
                  style={[styles.severityButton, severity === 'CRITICAL' && styles.severityCritical]}
                  onPress={() => setSeverity('CRITICAL')}
                >
                  <Text style={[styles.severityText, severity === 'CRITICAL' && styles.severityTextSelected]}>
                    Critique
                  </Text>
                </TouchableOpacity>
              </View>

              {/* DESCRIPTION */}
              <Text style={styles.label}>Description *</Text>
              <TextInput
                style={styles.textArea}
                placeholder="Décrivez l'anomalie..."
                value={description}
                onChangeText={setDescription}
                multiline
                numberOfLines={6}
              />

              {/* PHOTO */}
              <Text style={styles.label}>Photo (optionnel)</Text>
              {photo ? (
                <View style={styles.photoContainer}>
                  <Image source={{ uri: `data:image/jpeg;base64,${photo}` }} style={styles.photoPreview} />
                  <TouchableOpacity style={styles.photoRemoveButton} onPress={() => setPhoto(null)}>
                    <Text style={styles.photoRemoveText}>✕ Retirer</Text>
                  </TouchableOpacity>
                </View>
              ) : (
                <TouchableOpacity style={styles.addPhotoButton} onPress={takePhoto}>
                  <Text style={styles.addPhotoIcon}>📷</Text>
                  <Text style={styles.addPhotoText}>Prendre une photo</Text>
                </TouchableOpacity>
              )}

              {/* BUTTONS */}
              <View style={styles.formButtons}>
                <TouchableOpacity style={styles.cancelButton} onPress={handleCancel}>
                  <Text style={styles.cancelButtonText}>Annuler</Text>
                </TouchableOpacity>

                <TouchableOpacity
                  style={[styles.submitButton, submitting && styles.submitButtonDisabled]}
                  onPress={handleSubmit}
                  disabled={submitting}
                >
                  {submitting ? (
                    <ActivityIndicator color="#fff" />
                  ) : (
                    <Text style={styles.submitButtonText}>Signaler</Text>
                  )}
                </TouchableOpacity>
              </View>
            </View>
          </ScrollView>
        </View>
      </Modal>
    );
  };

  // ==========================================================================
  // RENDER: Main Screen
  // ==========================================================================
  return (
    <View style={styles.container}>
      <View style={styles.content}>
        <Text style={styles.title}>Déclarer une anomalie</Text>
        <Text style={styles.subtitle}>Scannez une puce NFC pour signaler un problème</Text>

        <View style={styles.iconContainer}>
          <Text style={styles.icon}>⚠️</Text>
        </View>

        <TouchableOpacity style={styles.scanButton} onPress={handleScanStart}>
          <Text style={styles.scanButtonText}>📱 SCANNER UNE PUCE</Text>
        </TouchableOpacity>

        <View style={styles.infoBox}>
          <Text style={styles.infoBoxTitle}>Quand utiliser cette fonction ?</Text>
          <Text style={styles.infoBoxText}>
            • Équipement défectueux{'\n'}
            • Situation dangereuse{'\n'}
            • Problème d'hygiène{'\n'}
            • Maintenance nécessaire
          </Text>
        </View>
      </View>

      {renderScanModal()}
      {renderForm()}
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
  content: {
    flex: 1,
    padding: 20,
    justifyContent: 'center',
    alignItems: 'center',
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#1e293b',
    marginBottom: 8,
    textAlign: 'center',
  },
  subtitle: {
    fontSize: 16,
    color: '#64748b',
    marginBottom: 40,
    textAlign: 'center',
  },
  iconContainer: {
    width: 120,
    height: 120,
    borderRadius: 60,
    backgroundColor: '#fef3c7',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 40,
  },
  icon: {
    fontSize: 64,
  },
  scanButton: {
    width: '100%',
    backgroundColor: '#f59e0b',
    paddingVertical: 18,
    borderRadius: 12,
    alignItems: 'center',
    marginBottom: 24,
  },
  scanButtonText: {
    color: '#fff',
    fontSize: 18,
    fontWeight: 'bold',
  },
  infoBox: {
    width: '100%',
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 20,
    borderWidth: 2,
    borderColor: '#e2e8f0',
  },
  infoBoxTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#1e293b',
    marginBottom: 12,
  },
  infoBoxText: {
    fontSize: 14,
    color: '#64748b',
    lineHeight: 24,
  },
  scanModalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.85)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  scanModalContainer: {
    backgroundColor: '#fff',
    borderRadius: 24,
    padding: 40,
    width: '85%',
    alignItems: 'center',
  },
  scanModalIcon: {
    fontSize: 64,
    marginBottom: 20,
  },
  scanModalTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#2563eb',
    marginBottom: 12,
    textAlign: 'center',
  },
  scanModalMessage: {
    fontSize: 16,
    color: '#64748b',
    textAlign: 'center',
    marginBottom: 32,
  },
  scanModalAnimation: {
    width: 120,
    height: 120,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 32,
  },
  scanPulse: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: '#f59e0b',
    opacity: 0.3,
  },
  scanCancelButton: {
    paddingHorizontal: 32,
    paddingVertical: 12,
    borderRadius: 12,
    borderWidth: 2,
    borderColor: '#e2e8f0',
  },
  scanCancelButtonText: {
    fontSize: 16,
    color: '#64748b',
    fontWeight: 'bold',
  },
  header: {
    backgroundColor: '#f59e0b',
    padding: 20,
    paddingTop: 50,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  headerTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#fff',
  },
  headerSubtitle: {
    fontSize: 14,
    color: '#fef3c7',
    marginTop: 4,
  },
  closeButton: {
    backgroundColor: '#ef4444',
    width: 40,
    height: 40,
    borderRadius: 20,
    alignItems: 'center',
    justifyContent: 'center',
  },
  closeButtonText: {
    color: '#fff',
    fontWeight: 'bold',
    fontSize: 24,
  },
  scrollContent: {
    flexGrow: 1,
  },
  formContainer: {
    padding: 20,
  },
  infoCard: {
    backgroundColor: '#eff6ff',
    borderRadius: 12,
    padding: 16,
    marginBottom: 24,
    borderWidth: 2,
    borderColor: '#bfdbfe',
  },
  infoLabel: {
    fontSize: 12,
    fontWeight: 'bold',
    color: '#2563eb',
    marginBottom: 4,
    textTransform: 'uppercase',
  },
  infoValue: {
    fontSize: 16,
    color: '#1e293b',
  },
  label: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1e293b',
    marginBottom: 8,
    marginTop: 16,
  },
  severityGroup: {
    flexDirection: 'row',
    gap: 8,
    marginBottom: 16,
  },
  severityButton: {
    flex: 1,
    paddingVertical: 12,
    borderRadius: 8,
    borderWidth: 2,
    borderColor: '#e2e8f0',
    alignItems: 'center',
    backgroundColor: '#fff',
  },
  severityLow: {
    borderColor: '#10b981',
    backgroundColor: '#d1fae5',
  },
  severityMedium: {
    borderColor: '#fbbf24',
    backgroundColor: '#fef3c7',
  },
  severityHigh: {
    borderColor: '#f97316',
    backgroundColor: '#ffedd5',
  },
  severityCritical: {
    borderColor: '#ef4444',
    backgroundColor: '#fee2e2',
  },
  severityText: {
    fontSize: 14,
    color: '#64748b',
    fontWeight: '600',
  },
  severityTextSelected: {
    color: '#1e293b',
    fontWeight: 'bold',
  },
  textArea: {
    borderWidth: 2,
    borderColor: '#e2e8f0',
    borderRadius: 8,
    padding: 12,
    fontSize: 16,
    minHeight: 120,
    textAlignVertical: 'top',
    backgroundColor: '#fff',
  },
  photoContainer: {
    alignItems: 'center',
    marginBottom: 16,
  },
  photoPreview: {
    width: 200,
    height: 200,
    borderRadius: 12,
    borderWidth: 2,
    borderColor: '#e2e8f0',
    marginBottom: 12,
  },
  photoRemoveButton: {
    backgroundColor: '#ef4444',
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 8,
  },
  photoRemoveText: {
    color: '#fff',
    fontWeight: 'bold',
  },
  addPhotoButton: {
    borderWidth: 2,
    borderColor: '#2563eb',
    borderStyle: 'dashed',
    borderRadius: 12,
    padding: 24,
    alignItems: 'center',
    backgroundColor: '#eff6ff',
  },
  addPhotoIcon: {
    fontSize: 48,
    marginBottom: 8,
  },
  addPhotoText: {
    fontSize: 16,
    color: '#2563eb',
    fontWeight: 'bold',
  },
  formButtons: {
    flexDirection: 'row',
    gap: 12,
    marginTop: 24,
  },
  cancelButton: {
    flex: 1,
    paddingVertical: 14,
    borderRadius: 8,
    borderWidth: 2,
    borderColor: '#e2e8f0',
    alignItems: 'center',
    backgroundColor: '#fff',
  },
  cancelButtonText: {
    fontSize: 16,
    color: '#64748b',
    fontWeight: 'bold',
  },
  submitButton: {
    flex: 1,
    paddingVertical: 14,
    borderRadius: 8,
    backgroundColor: '#f59e0b',
    alignItems: 'center',
  },
  submitButtonDisabled: {
    backgroundColor: '#94a3b8',
  },
  submitButtonText: {
    fontSize: 16,
    color: '#fff',
    fontWeight: 'bold',
  },
});
