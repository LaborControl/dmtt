/**
 * Intercept Screen (Supervisor)
 *
 * Features:
 * - Scan chip to see associated task
 * - Intercept delayed tasks
 * - Perform task on behalf of assigned user
 * - Add supervisor notes
 */

import React, { useState } from 'react';
import {
  StyleSheet,
  Text,
  View,
  TextInput,
  TouchableOpacity,
  Alert,
  Modal,
  ScrollView,
  ActivityIndicator,
} from 'react-native';
import NfcManager, { NfcTech } from 'react-native-nfc-manager';
import { useAuth } from '@/contexts/AuthContext';

const API_BASE_URL = 'https://laborcontrol-api.azurewebsites.net/api';

interface ScheduledTask {
  id: string;
  controlPointName: string;
  scheduledTimeStart: string;
  scheduledTimeEnd: string;
  status: string;
  assignedUserName: string;
  isDelayed: boolean;
}

export default function InterceptScreen() {
  const { token, user } = useAuth();

  const [loading, setLoading] = useState(false);
  const [scanning, setScanning] = useState(false);
  const [showTaskModal, setShowTaskModal] = useState(false);
  const [scannedTask, setScannedTask] = useState<ScheduledTask | null>(null);

  // Form state
  const [observations, setObservations] = useState('');
  const [supervisorNotes, setSupervisorNotes] = useState('');

  // ==========================================================================
  // FUNCTION: Scan chip to find task
  // ==========================================================================
  const handleScanChip = async () => {
    setScanning(true);

    try {
      await NfcManager.start();
      await NfcManager.requestTechnology(NfcTech.NfcA);

      const tag = await NfcManager.getTag();

      if (tag && tag.id) {
        let uid;
        if (Array.isArray(tag.id)) {
          uid = tag.id.map((byte: number) => byte.toString(16).padStart(2, '0')).join('').toUpperCase();
        } else {
          uid = String(tag.id).toUpperCase();
        }

        console.log('[SUPERVISOR] UID scanned:', uid);

        // Find control point by UID
        const cpResponse = await fetch(`${API_BASE_URL}/controlpoints/by-uid/${uid}`, {
          headers: { Authorization: `Bearer ${token}` },
        });

        if (!cpResponse.ok) {
          Alert.alert('❌ Erreur', 'Point de contrôle non trouvé');
          return;
        }

        const controlPoint = await cpResponse.json();

        // Find scheduled task for this control point
        const taskResponse = await fetch(
          `${API_BASE_URL}/scheduledtasks/control-point/${controlPoint.id}/today`,
          {
            headers: { Authorization: `Bearer ${token}` },
          }
        );

        if (taskResponse.ok) {
          const task = await taskResponse.json();

          // Check if task is delayed
          const now = new Date();
          const [hours, minutes] = task.scheduledTimeEnd.split(':');
          const endTime = new Date();
          endTime.setHours(parseInt(hours), parseInt(minutes), 0);

          task.isDelayed = now > endTime && task.status !== 'COMPLETED';
          task.controlPointName = controlPoint.name;

          setScannedTask(task);
          setShowTaskModal(true);

          if (task.isDelayed) {
            Alert.alert(
              '⚠️ Tâche en retard',
              `Cette tâche aurait dû être effectuée à ${task.scheduledTimeEnd}.\n\nVoulez-vous l'intercepter?`,
              [{ text: 'OK' }]
            );
          }
        } else {
          Alert.alert('ℹ️ Info', 'Aucune tâche planifiée pour ce point de contrôle aujourd\'hui');
        }
      }
    } catch (error) {
      console.error('[SUPERVISOR] Scan error:', error);
      const errorMessage = String(error);
      if (!errorMessage.includes('cancelled')) {
        Alert.alert('❌ Erreur', 'Scan annulé ou échec');
      }
    } finally {
      NfcManager.cancelTechnologyRequest();
      setScanning(false);
    }
  };

  // ==========================================================================
  // FUNCTION: Intercept and complete task
  // ==========================================================================
  const handleInterceptTask = async () => {
    if (!scannedTask) return;

    setLoading(true);

    try {
      const payload = {
        userId: user?.id, // Supervisor taking over
        controlPointId: scannedTask.id,
        scheduledTaskId: scannedTask.id,
        scannedAt: new Date().toISOString(),
        submittedAt: new Date().toISOString(),
        formDataJson: JSON.stringify({
          observations,
          supervisorNotes,
          interceptedBy: user?.email,
          originalAssignedUser: scannedTask.assignedUserName,
        }),
        type: 'SCHEDULED',
        status: 'COMPLETED',
      };

      const response = await fetch(`${API_BASE_URL}/taskexecutions`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(payload),
      });

      if (response.ok) {
        Alert.alert(
          '✅ Tâche interceptée',
          'La tâche a été complétée par vos soins.'
        );
        resetForm();
        setShowTaskModal(false);
      } else {
        Alert.alert('❌ Erreur', 'Impossible d\'enregistrer la tâche');
      }
    } catch (error) {
      console.error('[SUPERVISOR] Intercept error:', error);
      Alert.alert('❌ Erreur', 'Problème de connexion');
    } finally {
      setLoading(false);
    }
  };

  // ==========================================================================
  // FUNCTION: Reset form
  // ==========================================================================
  const resetForm = () => {
    setObservations('');
    setSupervisorNotes('');
    setScannedTask(null);
  };

  // ==========================================================================
  // RENDER
  // ==========================================================================
  return (
    <View style={styles.container}>
      <View style={styles.content}>
        <Text style={styles.title}>Intervention Superviseur</Text>
        <Text style={styles.subtitle}>
          Scannez une puce pour voir la tâche associée et intervenir si nécessaire
        </Text>

        <TouchableOpacity
          style={[styles.scanButton, scanning && styles.buttonDisabled]}
          onPress={handleScanChip}
          disabled={scanning}
        >
          {scanning ? (
            <ActivityIndicator color="#fff" />
          ) : (
            <Text style={styles.scanButtonText}>📱 SCANNER UNE PUCE</Text>
          )}
        </TouchableOpacity>

        <View style={styles.infoBox}>
          <Text style={styles.infoTitle}>💡 Comment ça marche ?</Text>
          <Text style={styles.infoText}>
            1. Scannez la puce NFC du point de contrôle{'\n'}
            2. Consultez la tâche planifiée{'\n'}
            3. Si la tâche est en retard, vous pouvez l'intercepter{'\n'}
            4. Remplissez le formulaire et validez
          </Text>
        </View>
      </View>

      {/* TASK MODAL */}
      <Modal
        visible={showTaskModal}
        animationType="slide"
        onRequestClose={() => setShowTaskModal(false)}
      >
        <ScrollView style={styles.modalContainer}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>Tâche Scannée</Text>
            <TouchableOpacity onPress={() => setShowTaskModal(false)}>
              <Text style={styles.closeButton}>✕</Text>
            </TouchableOpacity>
          </View>

          {scannedTask && (
            <View style={styles.form}>
              {/* TASK INFO */}
              <View
                style={[
                  styles.statusBadge,
                  { backgroundColor: scannedTask.isDelayed ? '#ef4444' : '#10b981' },
                ]}
              >
                <Text style={styles.statusBadgeText}>
                  {scannedTask.isDelayed ? '⚠️ EN RETARD' : '✅ DANS LES TEMPS'}
                </Text>
              </View>

              <Text style={styles.taskTitle}>{scannedTask.controlPointName}</Text>
              <Text style={styles.taskInfo}>
                ⏰ Horaire prévu: {scannedTask.scheduledTimeStart} -{' '}
                {scannedTask.scheduledTimeEnd}
              </Text>
              <Text style={styles.taskInfo}>
                👤 Assigné à: {scannedTask.assignedUserName}
              </Text>

              {/* OBSERVATIONS */}
              <Text style={styles.label}>Observations</Text>
              <TextInput
                style={[styles.input, styles.textArea]}
                placeholder="Vos observations sur la tâche..."
                value={observations}
                onChangeText={setObservations}
                multiline
                numberOfLines={4}
              />

              {/* SUPERVISOR NOTES */}
              <Text style={styles.label}>Notes superviseur</Text>
              <TextInput
                style={[styles.input, styles.textArea]}
                placeholder="Raison de l'intervention, contexte..."
                value={supervisorNotes}
                onChangeText={setSupervisorNotes}
                multiline
                numberOfLines={4}
              />

              {/* BUTTONS */}
              <TouchableOpacity
                style={[styles.submitButton, loading && styles.buttonDisabled]}
                onPress={handleInterceptTask}
                disabled={loading}
              >
                {loading ? (
                  <ActivityIndicator color="#fff" />
                ) : (
                  <Text style={styles.submitButtonText}>✅ Intercepter la tâche</Text>
                )}
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.cancelButton}
                onPress={() => {
                  resetForm();
                  setShowTaskModal(false);
                }}
              >
                <Text style={styles.cancelButtonText}>Annuler</Text>
              </TouchableOpacity>
            </View>
          )}
        </ScrollView>
      </Modal>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8fafc' },
  content: {
    flex: 1,
    padding: 20,
    alignItems: 'center',
    justifyContent: 'center',
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#1e293b',
    marginBottom: 12,
    textAlign: 'center',
  },
  subtitle: {
    fontSize: 16,
    color: '#64748b',
    marginBottom: 32,
    textAlign: 'center',
    lineHeight: 24,
  },
  scanButton: {
    backgroundColor: '#f59e0b',
    paddingVertical: 20,
    paddingHorizontal: 40,
    borderRadius: 16,
    elevation: 4,
    marginBottom: 32,
  },
  scanButtonText: {
    color: '#fff',
    fontSize: 20,
    fontWeight: 'bold',
  },
  buttonDisabled: {
    backgroundColor: '#fcd34d',
  },
  infoBox: {
    backgroundColor: '#fff',
    borderRadius: 16,
    padding: 20,
    borderWidth: 2,
    borderColor: '#f59e0b',
    width: '100%',
  },
  infoTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#f59e0b',
    marginBottom: 12,
  },
  infoText: {
    fontSize: 14,
    color: '#64748b',
    lineHeight: 22,
  },

  // Modal
  modalContainer: { flex: 1, backgroundColor: '#f8fafc' },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 20,
    paddingTop: 60,
    backgroundColor: '#f59e0b',
  },
  modalTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#fff',
  },
  closeButton: {
    fontSize: 32,
    color: '#fff',
    fontWeight: 'bold',
  },
  form: { padding: 20 },
  statusBadge: {
    paddingVertical: 12,
    paddingHorizontal: 20,
    borderRadius: 12,
    alignSelf: 'flex-start',
    marginBottom: 16,
  },
  statusBadgeText: {
    color: '#fff',
    fontSize: 14,
    fontWeight: 'bold',
  },
  taskTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#1e293b',
    marginBottom: 12,
  },
  taskInfo: {
    fontSize: 16,
    color: '#64748b',
    marginBottom: 8,
  },
  label: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1e293b',
    marginBottom: 8,
    marginTop: 20,
  },
  input: {
    backgroundColor: '#fff',
    borderWidth: 2,
    borderColor: '#e2e8f0',
    borderRadius: 12,
    padding: 14,
    fontSize: 16,
  },
  textArea: {
    minHeight: 100,
    textAlignVertical: 'top',
  },
  submitButton: {
    backgroundColor: '#f59e0b',
    paddingVertical: 16,
    borderRadius: 12,
    alignItems: 'center',
    marginTop: 24,
  },
  submitButtonText: {
    color: '#fff',
    fontSize: 18,
    fontWeight: 'bold',
  },
  cancelButton: {
    paddingVertical: 16,
    borderRadius: 12,
    alignItems: 'center',
    marginTop: 12,
    borderWidth: 2,
    borderColor: '#e2e8f0',
  },
  cancelButtonText: {
    color: '#64748b',
    fontSize: 16,
    fontWeight: '600',
  },
});
