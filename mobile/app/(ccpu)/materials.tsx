/**
 * CCPU - Materials Validation Screen
 *
 * List of materials pending CCPU validation
 * Verify material certificates and traceability
 */

import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  RefreshControl,
  Alert,
  TextInput,
  Modal
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useAuth } from '@/contexts/AuthContext';
import LoadingSpinner from '@/components/shared/LoadingSpinner';
import { apiClient } from '@/services/api';

interface Material {
  id: string;
  reference: string;
  materialType: string;
  grade: string;
  heatNumber: string;
  certificateNumber: string | null;
  supplier: string | null;
  receivedDate: string;
  isCCPUValidated: boolean;
  ccpuValidatedAt: string | null;
  ccpuValidatorName: string | null;
}

export default function MaterialsScreen() {
  const { token } = useAuth();

  const [materials, setMaterials] = useState<Material[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [showValidated, setShowValidated] = useState(false);

  // Validation modal
  const [validationModal, setValidationModal] = useState(false);
  const [selectedMaterial, setSelectedMaterial] = useState<Material | null>(null);
  const [validationNotes, setValidationNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const fetchMaterials = useCallback(async () => {
    try {
      const params = new URLSearchParams({
        ccpuValidated: showValidated ? 'true' : 'false'
      });

      const response = await apiClient.get(`/materials?${params}`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      setMaterials(response.data);
    } catch (error) {
      console.error('Error fetching materials:', error);
      Alert.alert('Erreur', 'Impossible de charger les matériaux.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [token, showValidated]);

  useEffect(() => {
    fetchMaterials();
  }, [fetchMaterials]);

  const onRefresh = () => {
    setRefreshing(true);
    fetchMaterials();
  };

  const openValidationModal = (material: Material) => {
    setSelectedMaterial(material);
    setValidationNotes('');
    setValidationModal(true);
  };

  const submitValidation = async (approved: boolean) => {
    if (!selectedMaterial) return;

    setSubmitting(true);
    try {
      await apiClient.post(`/materials/${selectedMaterial.id}/ccpu-validation`, {
        isApproved: approved,
        validationNotes: validationNotes || null
      }, {
        headers: { Authorization: `Bearer ${token}` }
      });

      Alert.alert(
        approved ? 'Matériau validé' : 'Matériau refusé',
        approved
          ? 'Le matériau a été validé pour utilisation.'
          : 'Le matériau a été refusé. Une FNC sera créée.',
        [{ text: 'OK', onPress: () => {
          setValidationModal(false);
          setSelectedMaterial(null);
          fetchMaterials();
        }}]
      );
    } catch (error: any) {
      Alert.alert('Erreur', error.response?.data || 'Impossible de valider le matériau.');
    } finally {
      setSubmitting(false);
    }
  };

  const renderMaterial = ({ item }: { item: Material }) => (
    <TouchableOpacity
      style={styles.materialCard}
      onPress={() => !item.isCCPUValidated && openValidationModal(item)}
      disabled={item.isCCPUValidated}
    >
      <View style={styles.cardHeader}>
        <View style={styles.typeContainer}>
          <Ionicons name="cube" size={20} color="#8b5cf6" />
          <Text style={styles.materialType}>{item.materialType}</Text>
        </View>
        {item.isCCPUValidated ? (
          <View style={styles.validatedBadge}>
            <Ionicons name="checkmark-shield" size={14} color="#fff" />
            <Text style={styles.validatedText}>Validé</Text>
          </View>
        ) : (
          <View style={styles.pendingBadge}>
            <Ionicons name="time" size={14} color="#f59e0b" />
            <Text style={styles.pendingText}>En attente</Text>
          </View>
        )}
      </View>

      <Text style={styles.reference}>{item.reference}</Text>
      <Text style={styles.grade}>Nuance: {item.grade}</Text>

      <View style={styles.detailsGrid}>
        <View style={styles.detailItem}>
          <Text style={styles.detailLabel}>N° Coulée</Text>
          <Text style={styles.detailValue}>{item.heatNumber}</Text>
        </View>
        {item.certificateNumber && (
          <View style={styles.detailItem}>
            <Text style={styles.detailLabel}>N° Certificat</Text>
            <Text style={styles.detailValue}>{item.certificateNumber}</Text>
          </View>
        )}
        {item.supplier && (
          <View style={styles.detailItem}>
            <Text style={styles.detailLabel}>Fournisseur</Text>
            <Text style={styles.detailValue}>{item.supplier}</Text>
          </View>
        )}
      </View>

      <View style={styles.cardFooter}>
        <View style={styles.dateInfo}>
          <Ionicons name="calendar" size={14} color="#64748b" />
          <Text style={styles.dateText}>
            Réception: {new Date(item.receivedDate).toLocaleDateString('fr-FR')}
          </Text>
        </View>

        {item.isCCPUValidated && item.ccpuValidatorName && (
          <Text style={styles.validatorText}>
            Par: {item.ccpuValidatorName}
          </Text>
        )}
      </View>

      {!item.isCCPUValidated && (
        <View style={styles.actionHint}>
          <Ionicons name="hand-left" size={16} color="#8b5cf6" />
          <Text style={styles.actionHintText}>Appuyez pour valider</Text>
        </View>
      )}
    </TouchableOpacity>
  );

  if (loading) {
    return <LoadingSpinner message="Chargement des matériaux..." />;
  }

  return (
    <View style={styles.container}>
      {/* Filter Toggle */}
      <View style={styles.filterContainer}>
        <TouchableOpacity
          style={[styles.filterButton, !showValidated && styles.filterButtonActive]}
          onPress={() => setShowValidated(false)}
        >
          <Ionicons name="time" size={16} color={!showValidated ? '#fff' : '#64748b'} />
          <Text style={[styles.filterText, !showValidated && styles.filterTextActive]}>
            En attente
          </Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.filterButton, showValidated && styles.filterButtonActive]}
          onPress={() => setShowValidated(true)}
        >
          <Ionicons name="checkmark-circle" size={16} color={showValidated ? '#fff' : '#64748b'} />
          <Text style={[styles.filterText, showValidated && styles.filterTextActive]}>
            Validés
          </Text>
        </TouchableOpacity>
      </View>

      {/* Count */}
      <Text style={styles.countText}>
        {materials.length} matériau{materials.length !== 1 ? 'x' : ''} {showValidated ? 'validé(s)' : 'en attente'}
      </Text>

      {/* Materials List */}
      <FlatList
        data={materials}
        keyExtractor={(item) => item.id}
        renderItem={renderMaterial}
        contentContainerStyle={styles.listContent}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            tintColor="#8b5cf6"
          />
        }
        ListEmptyComponent={
          <View style={styles.emptyState}>
            <Ionicons name="cube-outline" size={64} color="#64748b" />
            <Text style={styles.emptyTitle}>
              {showValidated ? 'Aucun matériau validé' : 'Aucun matériau en attente'}
            </Text>
            <Text style={styles.emptyText}>
              {showValidated
                ? 'Les matériaux validés apparaîtront ici.'
                : 'Les matériaux à valider apparaîtront ici.'}
            </Text>
          </View>
        }
      />

      {/* Validation Modal */}
      <Modal
        visible={validationModal}
        animationType="slide"
        transparent
        onRequestClose={() => setValidationModal(false)}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Validation Matériau</Text>
              <TouchableOpacity onPress={() => setValidationModal(false)}>
                <Ionicons name="close" size={24} color="#f1f5f9" />
              </TouchableOpacity>
            </View>

            {selectedMaterial && (
              <>
                <View style={styles.modalMaterialInfo}>
                  <Text style={styles.modalReference}>{selectedMaterial.reference}</Text>
                  <Text style={styles.modalDetails}>
                    {selectedMaterial.materialType} - {selectedMaterial.grade}
                  </Text>
                  <Text style={styles.modalDetails}>
                    Coulée: {selectedMaterial.heatNumber}
                  </Text>
                </View>

                <View style={styles.checklistSection}>
                  <Text style={styles.checklistTitle}>Points de contrôle:</Text>
                  <View style={styles.checkItem}>
                    <Ionicons name="checkbox-outline" size={20} color="#8b5cf6" />
                    <Text style={styles.checkItemText}>Certificat matière 3.1</Text>
                  </View>
                  <View style={styles.checkItem}>
                    <Ionicons name="checkbox-outline" size={20} color="#8b5cf6" />
                    <Text style={styles.checkItemText}>Traçabilité coulée</Text>
                  </View>
                  <View style={styles.checkItem}>
                    <Ionicons name="checkbox-outline" size={20} color="#8b5cf6" />
                    <Text style={styles.checkItemText}>Conformité dimensionnelle</Text>
                  </View>
                  <View style={styles.checkItem}>
                    <Ionicons name="checkbox-outline" size={20} color="#8b5cf6" />
                    <Text style={styles.checkItemText}>État de surface</Text>
                  </View>
                </View>

                <View style={styles.notesSection}>
                  <Text style={styles.notesLabel}>Notes de validation:</Text>
                  <TextInput
                    style={styles.notesInput}
                    placeholder="Observations éventuelles..."
                    placeholderTextColor="#64748b"
                    value={validationNotes}
                    onChangeText={setValidationNotes}
                    multiline
                    numberOfLines={3}
                  />
                </View>

                <View style={styles.modalActions}>
                  <TouchableOpacity
                    style={styles.rejectButton}
                    onPress={() => submitValidation(false)}
                    disabled={submitting}
                  >
                    <Ionicons name="close-circle" size={20} color="#ef4444" />
                    <Text style={styles.rejectButtonText}>Refuser</Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={styles.approveButton}
                    onPress={() => submitValidation(true)}
                    disabled={submitting}
                  >
                    {submitting ? (
                      <Text style={styles.approveButtonText}>...</Text>
                    ) : (
                      <>
                        <Ionicons name="checkmark-circle" size={20} color="#fff" />
                        <Text style={styles.approveButtonText}>Valider</Text>
                      </>
                    )}
                  </TouchableOpacity>
                </View>
              </>
            )}
          </View>
        </View>
      </Modal>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#0f172a'
  },
  filterContainer: {
    flexDirection: 'row',
    padding: 16,
    gap: 12
  },
  filterButton: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    paddingVertical: 12,
    borderRadius: 10,
    backgroundColor: '#1e293b'
  },
  filterButtonActive: {
    backgroundColor: '#8b5cf6'
  },
  filterText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#64748b'
  },
  filterTextActive: {
    color: '#fff'
  },
  countText: {
    fontSize: 14,
    color: '#64748b',
    paddingHorizontal: 16,
    marginBottom: 8
  },
  listContent: {
    padding: 16,
    paddingTop: 0
  },
  materialCard: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    borderWidth: 1,
    borderColor: '#334155'
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12
  },
  typeContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8
  },
  materialType: {
    fontSize: 14,
    fontWeight: '600',
    color: '#8b5cf6'
  },
  validatedBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    backgroundColor: '#22c55e',
    paddingVertical: 4,
    paddingHorizontal: 8,
    borderRadius: 12
  },
  validatedText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#fff'
  },
  pendingBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    backgroundColor: '#f59e0b20',
    paddingVertical: 4,
    paddingHorizontal: 8,
    borderRadius: 12
  },
  pendingText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#f59e0b'
  },
  reference: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#f1f5f9'
  },
  grade: {
    fontSize: 14,
    color: '#94a3b8',
    marginTop: 4
  },
  detailsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 16,
    marginTop: 12
  },
  detailItem: {
    minWidth: '40%'
  },
  detailLabel: {
    fontSize: 11,
    color: '#64748b',
    textTransform: 'uppercase'
  },
  detailValue: {
    fontSize: 14,
    color: '#f1f5f9',
    marginTop: 2
  },
  cardFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: '#334155'
  },
  dateInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6
  },
  dateText: {
    fontSize: 13,
    color: '#64748b'
  },
  validatorText: {
    fontSize: 12,
    color: '#22c55e'
  },
  actionHint: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: '#334155'
  },
  actionHintText: {
    fontSize: 13,
    color: '#8b5cf6'
  },
  emptyState: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 60
  },
  emptyTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#f1f5f9',
    marginTop: 16
  },
  emptyText: {
    fontSize: 14,
    color: '#64748b',
    marginTop: 8,
    textAlign: 'center'
  },
  // Modal Styles
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.7)',
    justifyContent: 'flex-end'
  },
  modalContent: {
    backgroundColor: '#1e293b',
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    padding: 20,
    maxHeight: '80%'
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 20
  },
  modalTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#f1f5f9'
  },
  modalMaterialInfo: {
    backgroundColor: '#0f172a',
    borderRadius: 12,
    padding: 16,
    marginBottom: 16
  },
  modalReference: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#8b5cf6'
  },
  modalDetails: {
    fontSize: 14,
    color: '#94a3b8',
    marginTop: 4
  },
  checklistSection: {
    marginBottom: 16
  },
  checklistTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#f1f5f9',
    marginBottom: 12
  },
  checkItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    paddingVertical: 8
  },
  checkItemText: {
    fontSize: 14,
    color: '#94a3b8'
  },
  notesSection: {
    marginBottom: 20
  },
  notesLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: '#f1f5f9',
    marginBottom: 8
  },
  notesInput: {
    backgroundColor: '#0f172a',
    borderRadius: 12,
    padding: 16,
    color: '#f1f5f9',
    fontSize: 14,
    borderWidth: 1,
    borderColor: '#334155',
    textAlignVertical: 'top',
    minHeight: 80
  },
  modalActions: {
    flexDirection: 'row',
    gap: 12
  },
  rejectButton: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    backgroundColor: '#ef444420',
    borderWidth: 1,
    borderColor: '#ef4444',
    borderRadius: 12,
    padding: 16
  },
  rejectButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#ef4444'
  },
  approveButton: {
    flex: 2,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    backgroundColor: '#22c55e',
    borderRadius: 12,
    padding: 16
  },
  approveButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#fff'
  }
});
