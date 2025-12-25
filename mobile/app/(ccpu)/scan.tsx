/**
 * CCPU - NFC Scan Screen
 *
 * Scan material or weld NFC tag for quick validation
 * Dual-mode: Material validation or Weld validation
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

type ScanMode = 'idle' | 'material' | 'weld';
type ValidationResult = 'approve' | 'reject' | null;

interface ScannedItem {
  id: string;
  reference: string;
  type: 'material' | 'weld';
  details: string;
  subDetails?: string;
  isCCPUValidated: boolean;
  prerequisitesOk: boolean;
  prerequisiteIssues?: string[];
}

export default function CCPUScanScreen() {
  const { token } = useAuth();

  const [loading, setLoading] = useState(false);
  const [scanMode, setScanMode] = useState<ScanMode>('idle');
  const [scannedItem, setScannedItem] = useState<ScannedItem | null>(null);
  const [validationResult, setValidationResult] = useState<ValidationResult>(null);
  const [notes, setNotes] = useState('');

  const handleNfcScan = async (tagData: { weldId?: string; materialId?: string; assetId?: string }) => {
    setLoading(true);
    try {
      if (tagData.materialId) {
        // Material scan
        const response = await apiClient.get(`/materials/${tagData.materialId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        const material = response.data;

        setScannedItem({
          id: material.id,
          reference: material.reference,
          type: 'material',
          details: `${material.materialType} - ${material.grade}`,
          subDetails: `Coulée: ${material.heatNumber}`,
          isCCPUValidated: material.isCCPUValidated,
          prerequisitesOk: !!material.certificateNumber,
          prerequisiteIssues: !material.certificateNumber ? ['Certificat matière manquant'] : []
        });
        setScanMode('material');

      } else if (tagData.weldId) {
        // Weld scan
        const response = await apiClient.get(`/welds/${tagData.weldId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        const weld = response.data;

        const issues = [];
        if (!weld.welderQualificationValid) issues.push('Qualification soudeur invalide');
        if (!weld.materialsValidated) issues.push('Matériaux non validés');
        if (!weld.dmosReference) issues.push('DMOS non défini');

        setScannedItem({
          id: weld.id,
          reference: weld.reference,
          type: 'weld',
          details: `${weld.weldingProcess} - ${weld.assetName}`,
          subDetails: weld.weldClass ? `Classe ${weld.weldClass}` : undefined,
          isCCPUValidated: weld.isCCPUValidated,
          prerequisitesOk: issues.length === 0,
          prerequisiteIssues: issues
        });
        setScanMode('weld');

      } else {
        Alert.alert('Tag non reconnu', 'Ce tag NFC ne contient pas d\'information de matériau ou soudure.');
      }
    } catch (error) {
      console.error('Error scanning:', error);
      Alert.alert('Erreur', 'Impossible de lire les informations du tag.');
    } finally {
      setLoading(false);
    }
  };

  const submitValidation = async () => {
    if (!scannedItem || !validationResult) return;

    const isApproved = validationResult === 'approve';

    setLoading(true);
    try {
      const endpoint = scannedItem.type === 'material'
        ? `/materials/${scannedItem.id}/ccpu-validation`
        : `/welds/${scannedItem.id}/ccpu-validation`;

      await apiClient.post(endpoint, {
        isApproved,
        validationNotes: notes || null
      }, {
        headers: { Authorization: `Bearer ${token}` }
      });

      const itemType = scannedItem.type === 'material' ? 'matériau' : 'soudure';

      Alert.alert(
        isApproved ? 'Validation réussie' : 'Refus enregistré',
        isApproved
          ? `Le ${itemType} a été validé avec succès.`
          : `Le ${itemType} a été refusé. ${scannedItem.type === 'weld' ? 'La soudure est bloquée.' : 'Une FNC sera créée.'}`,
        [{ text: 'OK', onPress: resetState }]
      );
    } catch (error: any) {
      Alert.alert('Erreur', error.response?.data || 'Impossible de valider.');
    } finally {
      setLoading(false);
    }
  };

  const resetState = () => {
    setScanMode('idle');
    setScannedItem(null);
    setValidationResult(null);
    setNotes('');
  };

  if (loading) {
    return <LoadingSpinner message="Traitement en cours..." />;
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      {/* Header */}
      <View style={styles.header}>
        <Ionicons name="scan-circle" size={48} color="#8b5cf6" />
        <Text style={styles.title}>Scanner CCPU</Text>
        <Text style={styles.subtitle}>
          {scanMode === 'idle' && 'Scannez un tag NFC pour valider'}
          {scanMode === 'material' && 'Validation du matériau'}
          {scanMode === 'weld' && 'Validation de la soudure'}
        </Text>
      </View>

      {/* Idle State - Scan Button */}
      {scanMode === 'idle' && (
        <View style={styles.scanSection}>
          <NfcScanButton
            onScan={handleNfcScan}
            buttonText="Scanner"
            buttonColor="#8b5cf6"
          />

          <View style={styles.infoCards}>
            <View style={styles.infoCard}>
              <Ionicons name="cube" size={32} color="#8b5cf6" />
              <Text style={styles.infoCardTitle}>Matériaux</Text>
              <Text style={styles.infoCardText}>
                Vérifiez les certificats et la traçabilité
              </Text>
            </View>
            <View style={styles.infoCard}>
              <Ionicons name="flame" size={32} color="#f97316" />
              <Text style={styles.infoCardTitle}>Soudures</Text>
              <Text style={styles.infoCardText}>
                Validez les prérequis avant exécution
              </Text>
            </View>
          </View>
        </View>
      )}

      {/* Scanned Item View */}
      {scannedItem && (
        <View style={styles.validationSection}>
          {/* Scanned Item Card */}
          <View style={[
            styles.itemCard,
            scannedItem.type === 'material' ? styles.materialCard : styles.weldCard
          ]}>
            <View style={styles.itemTypeHeader}>
              <Ionicons
                name={scannedItem.type === 'material' ? 'cube' : 'flame'}
                size={24}
                color={scannedItem.type === 'material' ? '#8b5cf6' : '#f97316'}
              />
              <Text style={styles.itemTypeLabel}>
                {scannedItem.type === 'material' ? 'MATÉRIAU' : 'SOUDURE'}
              </Text>
            </View>

            <Text style={styles.itemReference}>{scannedItem.reference}</Text>
            <Text style={styles.itemDetails}>{scannedItem.details}</Text>
            {scannedItem.subDetails && (
              <Text style={styles.itemSubDetails}>{scannedItem.subDetails}</Text>
            )}

            {scannedItem.isCCPUValidated && (
              <View style={styles.alreadyValidated}>
                <Ionicons name="checkmark-shield" size={20} color="#22c55e" />
                <Text style={styles.alreadyValidatedText}>Déjà validé CCPU</Text>
              </View>
            )}
          </View>

          {/* Prerequisites Check */}
          {!scannedItem.isCCPUValidated && (
            <View style={styles.prerequisitesCard}>
              <Text style={styles.prerequisitesTitle}>Vérification prérequis</Text>

              {scannedItem.prerequisitesOk ? (
                <View style={styles.prerequisiteOk}>
                  <Ionicons name="checkmark-circle" size={24} color="#22c55e" />
                  <Text style={styles.prerequisiteOkText}>
                    Tous les prérequis sont conformes
                  </Text>
                </View>
              ) : (
                <View style={styles.prerequisiteIssues}>
                  <Ionicons name="alert-circle" size={24} color="#f59e0b" />
                  <View style={styles.issuesList}>
                    {scannedItem.prerequisiteIssues?.map((issue, index) => (
                      <Text key={index} style={styles.issueText}>• {issue}</Text>
                    ))}
                  </View>
                </View>
              )}
            </View>
          )}

          {/* Validation Buttons */}
          {!scannedItem.isCCPUValidated && (
            <>
              <Text style={styles.sectionTitle}>Décision CCPU</Text>

              <View style={styles.decisionButtons}>
                <TouchableOpacity
                  style={[
                    styles.decisionButton,
                    styles.rejectButton,
                    validationResult === 'reject' && styles.rejectButtonSelected
                  ]}
                  onPress={() => setValidationResult('reject')}
                >
                  <Ionicons
                    name="close-circle"
                    size={32}
                    color={validationResult === 'reject' ? '#fff' : '#ef4444'}
                  />
                  <Text style={[
                    styles.decisionButtonText,
                    styles.rejectButtonText,
                    validationResult === 'reject' && styles.decisionButtonTextSelected
                  ]}>
                    Refuser
                  </Text>
                </TouchableOpacity>

                <TouchableOpacity
                  style={[
                    styles.decisionButton,
                    styles.approveButton,
                    validationResult === 'approve' && styles.approveButtonSelected
                  ]}
                  onPress={() => setValidationResult('approve')}
                >
                  <Ionicons
                    name="checkmark-shield"
                    size={32}
                    color={validationResult === 'approve' ? '#fff' : '#22c55e'}
                  />
                  <Text style={[
                    styles.decisionButtonText,
                    styles.approveButtonText,
                    validationResult === 'approve' && styles.decisionButtonTextSelected
                  ]}>
                    Valider
                  </Text>
                </TouchableOpacity>
              </View>

              {/* Notes */}
              <View style={styles.notesSection}>
                <Text style={styles.notesLabel}>Notes (optionnel)</Text>
                <TextInput
                  style={styles.notesInput}
                  placeholder="Observations..."
                  placeholderTextColor="#64748b"
                  value={notes}
                  onChangeText={setNotes}
                  multiline
                  numberOfLines={2}
                />
              </View>

              {/* Action Buttons */}
              <View style={styles.actionButtons}>
                <TouchableOpacity style={styles.cancelButton} onPress={resetState}>
                  <Text style={styles.cancelButtonText}>Annuler</Text>
                </TouchableOpacity>

                <TouchableOpacity
                  style={[
                    styles.submitButton,
                    !validationResult && styles.submitButtonDisabled
                  ]}
                  onPress={submitValidation}
                  disabled={!validationResult}
                >
                  <Ionicons name="checkmark" size={20} color="#fff" />
                  <Text style={styles.submitButtonText}>Confirmer</Text>
                </TouchableOpacity>
              </View>
            </>
          )}

          {/* Already Validated - New Scan Button */}
          {scannedItem.isCCPUValidated && (
            <TouchableOpacity style={styles.newScanButton} onPress={resetState}>
              <Ionicons name="scan" size={20} color="#8b5cf6" />
              <Text style={styles.newScanButtonText}>Nouveau scan</Text>
            </TouchableOpacity>
          )}
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
  infoCards: {
    flexDirection: 'row',
    gap: 12,
    width: '100%'
  },
  infoCard: {
    flex: 1,
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    alignItems: 'center'
  },
  infoCardTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#f1f5f9',
    marginTop: 8
  },
  infoCardText: {
    fontSize: 12,
    color: '#64748b',
    textAlign: 'center',
    marginTop: 4
  },
  validationSection: {
    gap: 16
  },
  itemCard: {
    borderRadius: 12,
    padding: 16,
    borderWidth: 2
  },
  materialCard: {
    backgroundColor: '#8b5cf615',
    borderColor: '#8b5cf6'
  },
  weldCard: {
    backgroundColor: '#f9731615',
    borderColor: '#f97316'
  },
  itemTypeHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginBottom: 12
  },
  itemTypeLabel: {
    fontSize: 12,
    fontWeight: '700',
    color: '#94a3b8',
    letterSpacing: 1
  },
  itemReference: {
    fontSize: 22,
    fontWeight: 'bold',
    color: '#f1f5f9'
  },
  itemDetails: {
    fontSize: 14,
    color: '#94a3b8',
    marginTop: 4
  },
  itemSubDetails: {
    fontSize: 13,
    color: '#64748b',
    marginTop: 2
  },
  alreadyValidated: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginTop: 16,
    padding: 12,
    backgroundColor: '#22c55e20',
    borderRadius: 8
  },
  alreadyValidatedText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#22c55e'
  },
  prerequisitesCard: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16
  },
  prerequisitesTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#f1f5f9',
    marginBottom: 12
  },
  prerequisiteOk: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10
  },
  prerequisiteOkText: {
    fontSize: 14,
    color: '#22c55e'
  },
  prerequisiteIssues: {
    flexDirection: 'row',
    gap: 10
  },
  issuesList: {
    flex: 1
  },
  issueText: {
    fontSize: 14,
    color: '#f59e0b',
    marginBottom: 4
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#f1f5f9'
  },
  decisionButtons: {
    flexDirection: 'row',
    gap: 12
  },
  decisionButton: {
    flex: 1,
    alignItems: 'center',
    padding: 20,
    borderRadius: 12,
    borderWidth: 2
  },
  rejectButton: {
    backgroundColor: '#ef444415',
    borderColor: '#ef4444'
  },
  approveButton: {
    backgroundColor: '#22c55e15',
    borderColor: '#22c55e'
  },
  rejectButtonSelected: {
    backgroundColor: '#ef4444'
  },
  approveButtonSelected: {
    backgroundColor: '#22c55e'
  },
  decisionButtonText: {
    fontSize: 14,
    fontWeight: '600',
    marginTop: 8
  },
  rejectButtonText: {
    color: '#ef4444'
  },
  approveButtonText: {
    color: '#22c55e'
  },
  decisionButtonTextSelected: {
    color: '#fff'
  },
  notesSection: {
    gap: 8
  },
  notesLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: '#f1f5f9'
  },
  notesInput: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    color: '#f1f5f9',
    fontSize: 14,
    borderWidth: 1,
    borderColor: '#334155',
    textAlignVertical: 'top',
    minHeight: 60
  },
  actionButtons: {
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
  submitButton: {
    flex: 2,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    backgroundColor: '#8b5cf6',
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
  },
  newScanButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    borderWidth: 1,
    borderColor: '#8b5cf6'
  },
  newScanButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#8b5cf6'
  }
});
