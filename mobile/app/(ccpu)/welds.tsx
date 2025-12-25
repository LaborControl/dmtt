/**
 * CCPU - Welds Validation Screen
 *
 * List of welds pending CCPU validation before execution
 * Verify preparation, materials, and welder qualifications
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

interface WeldForValidation {
  id: string;
  reference: string;
  assetName: string;
  weldingProcess: string;
  weldClass: string | null;
  assignedWelderName: string | null;
  welderQualificationValid: boolean;
  materialsValidated: boolean;
  dmosReference: string | null;
  isCCPUValidated: boolean;
  ccpuValidatedAt: string | null;
  ccpuValidatorName: string | null;
  isBlocked: boolean;
}

export default function WeldsValidationScreen() {
  const { token } = useAuth();

  const [welds, setWelds] = useState<WeldForValidation[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [showValidated, setShowValidated] = useState(false);

  // Validation modal
  const [validationModal, setValidationModal] = useState(false);
  const [selectedWeld, setSelectedWeld] = useState<WeldForValidation | null>(null);
  const [validationNotes, setValidationNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const fetchWelds = useCallback(async () => {
    try {
      const params = new URLSearchParams({
        ccpuValidated: showValidated ? 'true' : 'false'
      });

      const response = await apiClient.get(`/welds?${params}`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      setWelds(response.data);
    } catch (error) {
      console.error('Error fetching welds:', error);
      Alert.alert('Erreur', 'Impossible de charger les soudures.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [token, showValidated]);

  useEffect(() => {
    fetchWelds();
  }, [fetchWelds]);

  const onRefresh = () => {
    setRefreshing(true);
    fetchWelds();
  };

  const openValidationModal = (weld: WeldForValidation) => {
    setSelectedWeld(weld);
    setValidationNotes('');
    setValidationModal(true);
  };

  const submitValidation = async (approved: boolean) => {
    if (!selectedWeld) return;

    setSubmitting(true);
    try {
      await apiClient.post(`/welds/${selectedWeld.id}/ccpu-validation`, {
        isApproved: approved,
        validationNotes: validationNotes || null
      }, {
        headers: { Authorization: `Bearer ${token}` }
      });

      Alert.alert(
        approved ? 'Soudure validée' : 'Soudure refusée',
        approved
          ? 'La soudure est maintenant autorisée pour exécution.'
          : 'La soudure a été bloquée. Le soudeur ne pourra pas l\'exécuter.',
        [{ text: 'OK', onPress: () => {
          setValidationModal(false);
          setSelectedWeld(null);
          fetchWelds();
        }}]
      );
    } catch (error: any) {
      Alert.alert('Erreur', error.response?.data || 'Impossible de valider la soudure.');
    } finally {
      setSubmitting(false);
    }
  };

  const getPrerequisiteStatus = (weld: WeldForValidation) => {
    const issues = [];
    if (!weld.welderQualificationValid) issues.push('Qualification soudeur');
    if (!weld.materialsValidated) issues.push('Matériaux');
    if (!weld.dmosReference) issues.push('DMOS');
    return issues;
  };

  const renderWeld = ({ item }: { item: WeldForValidation }) => {
    const issues = getPrerequisiteStatus(item);
    const hasIssues = issues.length > 0;

    return (
      <TouchableOpacity
        style={[styles.weldCard, item.isBlocked && styles.blockedCard]}
        onPress={() => !item.isCCPUValidated && !item.isBlocked && openValidationModal(item)}
        disabled={item.isCCPUValidated || item.isBlocked}
      >
        <View style={styles.cardHeader}>
          <View style={styles.processContainer}>
            <Ionicons name="flame" size={20} color="#8b5cf6" />
            <Text style={styles.processText}>{item.weldingProcess}</Text>
          </View>
          {item.isBlocked ? (
            <View style={styles.blockedBadge}>
              <Ionicons name="lock-closed" size={14} color="#fff" />
              <Text style={styles.blockedText}>Bloquée</Text>
            </View>
          ) : item.isCCPUValidated ? (
            <View style={styles.validatedBadge}>
              <Ionicons name="checkmark-shield" size={14} color="#fff" />
              <Text style={styles.validatedText}>Validée</Text>
            </View>
          ) : (
            <View style={styles.pendingBadge}>
              <Ionicons name="time" size={14} color="#f59e0b" />
              <Text style={styles.pendingText}>En attente</Text>
            </View>
          )}
        </View>

        <Text style={styles.reference}>{item.reference}</Text>
        <Text style={styles.assetName}>{item.assetName}</Text>
        {item.weldClass && (
          <Text style={styles.weldClass}>Classe: {item.weldClass}</Text>
        )}

        {/* Prerequisites Check */}
        <View style={styles.prerequisitesSection}>
          <View style={styles.prerequisiteItem}>
            <Ionicons
              name={item.welderQualificationValid ? 'checkmark-circle' : 'alert-circle'}
              size={16}
              color={item.welderQualificationValid ? '#22c55e' : '#ef4444'}
            />
            <Text style={styles.prerequisiteText}>
              Soudeur: {item.assignedWelderName || 'Non assigné'}
            </Text>
          </View>
          <View style={styles.prerequisiteItem}>
            <Ionicons
              name={item.materialsValidated ? 'checkmark-circle' : 'alert-circle'}
              size={16}
              color={item.materialsValidated ? '#22c55e' : '#ef4444'}
            />
            <Text style={styles.prerequisiteText}>Matériaux validés</Text>
          </View>
          <View style={styles.prerequisiteItem}>
            <Ionicons
              name={item.dmosReference ? 'checkmark-circle' : 'alert-circle'}
              size={16}
              color={item.dmosReference ? '#22c55e' : '#ef4444'}
            />
            <Text style={styles.prerequisiteText}>
              DMOS: {item.dmosReference || 'Non défini'}
            </Text>
          </View>
        </View>

        {hasIssues && !item.isCCPUValidated && !item.isBlocked && (
          <View style={styles.warningBanner}>
            <Ionicons name="warning" size={16} color="#f59e0b" />
            <Text style={styles.warningText}>
              Attention: Prérequis manquants
            </Text>
          </View>
        )}

        {item.isCCPUValidated && item.ccpuValidatorName && (
          <View style={styles.cardFooter}>
            <Ionicons name="person" size={14} color="#22c55e" />
            <Text style={styles.validatorText}>
              Validé par {item.ccpuValidatorName}
            </Text>
          </View>
        )}

        {!item.isCCPUValidated && !item.isBlocked && (
          <View style={styles.actionHint}>
            <Ionicons name="hand-left" size={16} color="#8b5cf6" />
            <Text style={styles.actionHintText}>Appuyez pour valider</Text>
          </View>
        )}
      </TouchableOpacity>
    );
  };

  if (loading) {
    return <LoadingSpinner message="Chargement des soudures..." />;
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
            Validées
          </Text>
        </TouchableOpacity>
      </View>

      {/* Count */}
      <Text style={styles.countText}>
        {welds.length} soudure{welds.length !== 1 ? 's' : ''} {showValidated ? 'validée(s)' : 'en attente'}
      </Text>

      {/* Welds List */}
      <FlatList
        data={welds}
        keyExtractor={(item) => item.id}
        renderItem={renderWeld}
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
            <Ionicons name="flame-outline" size={64} color="#64748b" />
            <Text style={styles.emptyTitle}>
              {showValidated ? 'Aucune soudure validée' : 'Aucune soudure en attente'}
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
              <Text style={styles.modalTitle}>Validation CCPU</Text>
              <TouchableOpacity onPress={() => setValidationModal(false)}>
                <Ionicons name="close" size={24} color="#f1f5f9" />
              </TouchableOpacity>
            </View>

            {selectedWeld && (
              <>
                <View style={styles.modalWeldInfo}>
                  <Text style={styles.modalReference}>{selectedWeld.reference}</Text>
                  <Text style={styles.modalDetails}>{selectedWeld.assetName}</Text>
                  <Text style={styles.modalDetails}>
                    {selectedWeld.weldingProcess} - Classe {selectedWeld.weldClass || 'N/A'}
                  </Text>
                </View>

                <View style={styles.checklistSection}>
                  <Text style={styles.checklistTitle}>Vérifications CCPU:</Text>

                  <View style={styles.checkItem}>
                    <Ionicons
                      name={selectedWeld.welderQualificationValid ? 'checkbox' : 'close-circle'}
                      size={20}
                      color={selectedWeld.welderQualificationValid ? '#22c55e' : '#ef4444'}
                    />
                    <Text style={styles.checkItemText}>
                      Qualification soudeur valide
                    </Text>
                  </View>

                  <View style={styles.checkItem}>
                    <Ionicons
                      name={selectedWeld.materialsValidated ? 'checkbox' : 'close-circle'}
                      size={20}
                      color={selectedWeld.materialsValidated ? '#22c55e' : '#ef4444'}
                    />
                    <Text style={styles.checkItemText}>
                      Matériaux validés CCPU
                    </Text>
                  </View>

                  <View style={styles.checkItem}>
                    <Ionicons
                      name={selectedWeld.dmosReference ? 'checkbox' : 'close-circle'}
                      size={20}
                      color={selectedWeld.dmosReference ? '#22c55e' : '#ef4444'}
                    />
                    <Text style={styles.checkItemText}>
                      DMOS applicable
                    </Text>
                  </View>

                  <View style={styles.checkItem}>
                    <Ionicons name="checkbox-outline" size={20} color="#8b5cf6" />
                    <Text style={styles.checkItemText}>Préparation conforme</Text>
                  </View>

                  <View style={styles.checkItem}>
                    <Ionicons name="checkbox-outline" size={20} color="#8b5cf6" />
                    <Text style={styles.checkItemText}>Conditions environnementales OK</Text>
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
                    style={styles.blockButton}
                    onPress={() => submitValidation(false)}
                    disabled={submitting}
                  >
                    <Ionicons name="lock-closed" size={20} color="#ef4444" />
                    <Text style={styles.blockButtonText}>Bloquer</Text>
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
                        <Ionicons name="checkmark-shield" size={20} color="#fff" />
                        <Text style={styles.approveButtonText}>Valider CCPU</Text>
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
  weldCard: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    borderWidth: 1,
    borderColor: '#334155'
  },
  blockedCard: {
    borderColor: '#ef444450',
    opacity: 0.7
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12
  },
  processContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8
  },
  processText: {
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
  blockedBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    backgroundColor: '#ef4444',
    paddingVertical: 4,
    paddingHorizontal: 8,
    borderRadius: 12
  },
  blockedText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#fff'
  },
  reference: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#f1f5f9'
  },
  assetName: {
    fontSize: 14,
    color: '#94a3b8',
    marginTop: 4
  },
  weldClass: {
    fontSize: 13,
    color: '#64748b',
    marginTop: 2
  },
  prerequisitesSection: {
    marginTop: 12,
    gap: 6
  },
  prerequisiteItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8
  },
  prerequisiteText: {
    fontSize: 13,
    color: '#94a3b8'
  },
  warningBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    backgroundColor: '#f59e0b20',
    padding: 10,
    borderRadius: 8,
    marginTop: 12
  },
  warningText: {
    fontSize: 13,
    color: '#f59e0b',
    fontWeight: '500'
  },
  cardFooter: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: '#334155'
  },
  validatorText: {
    fontSize: 13,
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
    maxHeight: '85%'
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
  modalWeldInfo: {
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
  blockButton: {
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
  blockButtonText: {
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
    backgroundColor: '#8b5cf6',
    borderRadius: 12,
    padding: 16
  },
  approveButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#fff'
  }
});
