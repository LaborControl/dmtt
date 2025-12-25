/**
 * Chip Assignment Screen (Admin)
 *
 * Features:
 * - Scan NFC chip
 * - Assign chip to control point
 * - Activate chip
 * - View assigned chips
 */

import React, { useState, useEffect } from 'react';
import {
  StyleSheet,
  Text,
  View,
  TouchableOpacity,
  FlatList,
  Alert,
  Modal,
  Picker,
  ActivityIndicator,
} from 'react-native';
import NfcManager, { NfcTech } from 'react-native-nfc-manager';
import { useAuth } from '@/contexts/AuthContext';

const API_BASE_URL = 'https://laborcontrol-api.azurewebsites.net/api';

interface ControlPoint {
  id: string;
  name: string;
  locationDescription: string;
  rfidChipId: string | null;
}

interface AssignedChip {
  id: string;
  chipId: string;
  controlPointName: string;
  status: string;
  activatedAt: string;
}

export default function ChipAssignmentScreen() {
  const { token, user } = useAuth();

  const [controlPoints, setControlPoints] = useState<ControlPoint[]>([]);
  const [assignedChips, setAssignedChips] = useState<AssignedChip[]>([]);
  const [loading, setLoading] = useState(false);
  const [showAssignModal, setShowAssignModal] = useState(false);
  const [scannedUid, setScannedUid] = useState<string | null>(null);
  const [selectedControlPointId, setSelectedControlPointId] = useState<string>('');

  useEffect(() => {
    loadControlPoints();
    loadAssignedChips();
  }, []);

  const loadControlPoints = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/controlpoints/customer/${user?.customerId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (response.ok) {
        const data = await response.json();
        setControlPoints(data);
      }
    } catch (error) {
      console.error('[CHIP ASSIGNMENT] Load control points error:', error);
    }
  };

  const loadAssignedChips = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/rfidchips/customer/${user?.customerId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (response.ok) {
        const data = await response.json();
        setAssignedChips(data);
      }
    } catch (error) {
      console.error('[CHIP ASSIGNMENT] Load chips error:', error);
    }
  };

  const handleScanChip = async () => {
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

        console.log('[CHIP ASSIGNMENT] UID scanned:', uid);
        setScannedUid(uid);
        setShowAssignModal(true);
      } else {
        Alert.alert('❌ Erreur', 'Impossible de lire la puce');
      }
    } catch (error) {
      console.error('[CHIP ASSIGNMENT] Scan error:', error);
      const errorMessage = String(error);
      if (!errorMessage.includes('cancelled') && !errorMessage.includes('Cancel')) {
        Alert.alert('❌ Erreur', 'Scan annulé ou échec de lecture');
      }
    } finally {
      NfcManager.cancelTechnologyRequest();
    }
  };

  const handleAssign = async () => {
    if (!scannedUid || !selectedControlPointId) {
      Alert.alert('Erreur', 'Veuillez sélectionner un point de contrôle');
      return;
    }

    setLoading(true);

    try {
      // First, activate the chip if not already active
      const activateResponse = await fetch(`${API_BASE_URL}/rfidchips/activate`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ uid: scannedUid }),
      });

      let chipId;
      if (activateResponse.ok) {
        const activateData = await activateResponse.json();
        chipId = activateData.chip.chipId;
      } else {
        Alert.alert('❌ Erreur', 'Impossible d\'activer la puce');
        setLoading(false);
        return;
      }

      // Then assign to control point
      const assignResponse = await fetch(
        `${API_BASE_URL}/rfidchips/${chipId}/assign-to-control-point`,
        {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({ controlPointId: selectedControlPointId }),
        }
      );

      if (assignResponse.ok) {
        Alert.alert('✅ Succès', 'Puce affectée au point de contrôle');
        setShowAssignModal(false);
        setScannedUid(null);
        setSelectedControlPointId('');
        loadControlPoints();
        loadAssignedChips();
      } else {
        Alert.alert('❌ Erreur', 'Impossible d\'affecter la puce');
      }
    } catch (error) {
      console.error('[CHIP ASSIGNMENT] Assign error:', error);
      Alert.alert('❌ Erreur', 'Problème de connexion');
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <View style={styles.scanButtonContainer}>
        <TouchableOpacity style={styles.scanButton} onPress={handleScanChip}>
          <Text style={styles.scanButtonText}>📱 SCANNER UNE PUCE</Text>
        </TouchableOpacity>
      </View>

      <FlatList
        data={assignedChips}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <View style={styles.card}>
            <View style={styles.cardHeader}>
              <Text style={styles.cardTitle}>{item.controlPointName}</Text>
              <View style={[styles.badge, { backgroundColor: '#10b981' }]}>
                <Text style={styles.badgeText}>{item.status}</Text>
              </View>
            </View>
            <Text style={styles.cardChipId}>Puce: {item.chipId}</Text>
            <Text style={styles.cardDate}>
              Activée le {new Date(item.activatedAt).toLocaleDateString('fr-FR')}
            </Text>
          </View>
        )}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Text style={styles.emptyText}>Aucune puce affectée</Text>
            <Text style={styles.emptySubtext}>Scannez une puce pour l'affecter</Text>
          </View>
        }
        contentContainerStyle={styles.listContent}
      />

      <Modal visible={showAssignModal} animationType="slide" transparent>
        <View style={styles.modalOverlay}>
          <View style={styles.modalContainer}>
            <Text style={styles.modalTitle}>Affecter la puce</Text>

            <Text style={styles.modalLabel}>UID scanné:</Text>
            <Text style={styles.modalUid}>{scannedUid}</Text>

            <Text style={styles.modalLabel}>Point de contrôle:</Text>
            <Picker
              selectedValue={selectedControlPointId}
              onValueChange={(value) => setSelectedControlPointId(value)}
              style={styles.picker}
            >
              <Picker.Item label="Sélectionnez un point" value="" />
              {controlPoints
                .filter((cp) => !cp.rfidChipId)
                .map((cp) => (
                  <Picker.Item key={cp.id} label={cp.name} value={cp.id} />
                ))}
            </Picker>

            <View style={styles.modalButtons}>
              <TouchableOpacity
                style={styles.cancelButton}
                onPress={() => {
                  setShowAssignModal(false);
                  setScannedUid(null);
                  setSelectedControlPointId('');
                }}
              >
                <Text style={styles.cancelButtonText}>Annuler</Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={[styles.submitButton, loading && styles.buttonDisabled]}
                onPress={handleAssign}
                disabled={loading}
              >
                {loading ? (
                  <ActivityIndicator color="#fff" />
                ) : (
                  <Text style={styles.submitButtonText}>✅ Affecter</Text>
                )}
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8fafc' },
  scanButtonContainer: {
    padding: 16,
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#e2e8f0',
  },
  scanButton: {
    backgroundColor: '#8b5cf6',
    paddingVertical: 16,
    borderRadius: 12,
    alignItems: 'center',
  },
  scanButtonText: {
    color: '#fff',
    fontSize: 18,
    fontWeight: 'bold',
  },
  listContent: { padding: 16 },
  card: {
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    elevation: 3,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  cardTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#1e293b',
    flex: 1,
  },
  badge: {
    paddingHorizontal: 12,
    paddingVertical: 4,
    borderRadius: 12,
  },
  badgeText: {
    color: '#fff',
    fontSize: 11,
    fontWeight: 'bold',
  },
  cardChipId: {
    fontSize: 14,
    color: '#64748b',
    marginBottom: 8,
  },
  cardDate: {
    fontSize: 12,
    color: '#94a3b8',
  },
  emptyContainer: {
    alignItems: 'center',
    paddingVertical: 60,
  },
  emptyText: {
    fontSize: 18,
    color: '#94a3b8',
    fontWeight: '600',
  },
  emptySubtext: {
    fontSize: 14,
    color: '#cbd5e1',
    marginTop: 8,
  },

  // Modal
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'center',
    padding: 20,
  },
  modalContainer: {
    backgroundColor: '#fff',
    borderRadius: 16,
    padding: 24,
  },
  modalTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#8b5cf6',
    textAlign: 'center',
    marginBottom: 24,
  },
  modalLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: '#64748b',
    marginBottom: 8,
  },
  modalUid: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#1e293b',
    marginBottom: 16,
  },
  picker: {
    backgroundColor: '#f8fafc',
    borderRadius: 12,
    marginBottom: 24,
  },
  modalButtons: {
    flexDirection: 'row',
    gap: 12,
  },
  submitButton: {
    flex: 1,
    backgroundColor: '#8b5cf6',
    paddingVertical: 14,
    borderRadius: 12,
    alignItems: 'center',
  },
  submitButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
  },
  buttonDisabled: {
    backgroundColor: '#c4b5fd',
  },
  cancelButton: {
    flex: 1,
    paddingVertical: 14,
    borderRadius: 12,
    alignItems: 'center',
    borderWidth: 2,
    borderColor: '#e2e8f0',
  },
  cancelButtonText: {
    color: '#64748b',
    fontSize: 16,
    fontWeight: '600',
  },
});
