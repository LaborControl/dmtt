/**
 * Welder - Qualifications Screen
 *
 * Displays welder's qualifications with validity status
 * Shows AI pre-validation results if available
 */

import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  RefreshControl,
  Alert
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useAuth } from '@/contexts/AuthContext';
import LoadingSpinner from '@/components/shared/LoadingSpinner';
import { apiClient } from '@/services/api';

interface Qualification {
  id: string;
  qualificationNumber: string;
  weldingProcess: string;
  qualificationStandard: string;
  qualifiedPositions: string;
  qualifiedMaterials: string | null;
  thicknessRange: string | null;
  diameterRange: string | null;
  issueDate: string;
  expirationDate: string;
  status: 'VALID' | 'EXPIRING_SOON' | 'EXPIRED' | 'PENDING_VALIDATION';
  aiPreValidated: boolean;
  aiConfidenceScore: number | null;
  certifyingBody: string | null;
}

export default function QualificationsScreen() {
  const { token } = useAuth();
  const [qualifications, setQualifications] = useState<Qualification[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const fetchQualifications = useCallback(async () => {
    try {
      const response = await apiClient.get('/welder-qualifications/my-qualifications', {
        headers: { Authorization: `Bearer ${token}` }
      });
      setQualifications(response.data);
    } catch (error) {
      console.error('Error fetching qualifications:', error);
      Alert.alert('Erreur', 'Impossible de charger vos qualifications.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [token]);

  useEffect(() => {
    fetchQualifications();
  }, [fetchQualifications]);

  const onRefresh = () => {
    setRefreshing(true);
    fetchQualifications();
  };

  const getStatusConfig = (status: string) => {
    switch (status) {
      case 'VALID':
        return { color: '#22c55e', icon: 'checkmark-circle', label: 'Valide' };
      case 'EXPIRING_SOON':
        return { color: '#f59e0b', icon: 'warning', label: 'Expire bientôt' };
      case 'EXPIRED':
        return { color: '#ef4444', icon: 'close-circle', label: 'Expirée' };
      case 'PENDING_VALIDATION':
        return { color: '#3b82f6', icon: 'time', label: 'En attente' };
      default:
        return { color: '#64748b', icon: 'help-circle', label: status };
    }
  };

  const getDaysUntilExpiry = (expirationDate: string) => {
    const expiry = new Date(expirationDate);
    const today = new Date();
    const diffTime = expiry.getTime() - today.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays;
  };

  const renderQualification = ({ item }: { item: Qualification }) => {
    const statusConfig = getStatusConfig(item.status);
    const daysUntilExpiry = getDaysUntilExpiry(item.expirationDate);

    return (
      <TouchableOpacity style={styles.qualificationCard}>
        <View style={styles.cardHeader}>
          <View style={styles.processContainer}>
            <Ionicons name="flame" size={20} color="#f97316" />
            <Text style={styles.processText}>{item.weldingProcess}</Text>
          </View>
          <View style={[styles.statusBadge, { backgroundColor: statusConfig.color }]}>
            <Ionicons name={statusConfig.icon as any} size={14} color="#fff" />
            <Text style={styles.statusText}>{statusConfig.label}</Text>
          </View>
        </View>

        <Text style={styles.qualificationNumber}>{item.qualificationNumber}</Text>
        <Text style={styles.standard}>{item.qualificationStandard}</Text>

        <View style={styles.detailsGrid}>
          <View style={styles.detailItem}>
            <Text style={styles.detailLabel}>Positions</Text>
            <Text style={styles.detailValue}>{item.qualifiedPositions}</Text>
          </View>

          {item.thicknessRange && (
            <View style={styles.detailItem}>
              <Text style={styles.detailLabel}>Épaisseur</Text>
              <Text style={styles.detailValue}>{item.thicknessRange}</Text>
            </View>
          )}

          {item.diameterRange && (
            <View style={styles.detailItem}>
              <Text style={styles.detailLabel}>Diamètre</Text>
              <Text style={styles.detailValue}>{item.diameterRange}</Text>
            </View>
          )}

          {item.qualifiedMaterials && (
            <View style={styles.detailItem}>
              <Text style={styles.detailLabel}>Matériaux</Text>
              <Text style={styles.detailValue}>{item.qualifiedMaterials}</Text>
            </View>
          )}
        </View>

        <View style={styles.cardFooter}>
          <View style={styles.expiryInfo}>
            <Ionicons
              name="calendar"
              size={14}
              color={daysUntilExpiry <= 30 ? '#f59e0b' : '#64748b'}
            />
            <Text style={[
              styles.expiryText,
              daysUntilExpiry <= 30 && styles.expiryWarning
            ]}>
              {daysUntilExpiry > 0
                ? `Expire dans ${daysUntilExpiry} jours`
                : 'Expirée'}
            </Text>
          </View>

          {item.aiPreValidated && (
            <View style={styles.aiValidation}>
              <Ionicons name="sparkles" size={14} color="#8b5cf6" />
              <Text style={styles.aiText}>
                IA: {Math.round((item.aiConfidenceScore || 0) * 100)}%
              </Text>
            </View>
          )}
        </View>

        {item.certifyingBody && (
          <Text style={styles.certifyingBody}>
            Certifié par: {item.certifyingBody}
          </Text>
        )}
      </TouchableOpacity>
    );
  };

  const getStatistics = () => {
    const valid = qualifications.filter(q => q.status === 'VALID').length;
    const expiringSoon = qualifications.filter(q => q.status === 'EXPIRING_SOON').length;
    const expired = qualifications.filter(q => q.status === 'EXPIRED').length;
    return { valid, expiringSoon, expired };
  };

  if (loading) {
    return <LoadingSpinner message="Chargement des qualifications..." />;
  }

  const stats = getStatistics();

  return (
    <View style={styles.container}>
      {/* Statistics Header */}
      <View style={styles.statsContainer}>
        <View style={[styles.statCard, styles.statValid]}>
          <Ionicons name="checkmark-circle" size={24} color="#22c55e" />
          <Text style={styles.statNumber}>{stats.valid}</Text>
          <Text style={styles.statLabel}>Valides</Text>
        </View>
        <View style={[styles.statCard, styles.statWarning]}>
          <Ionicons name="warning" size={24} color="#f59e0b" />
          <Text style={styles.statNumber}>{stats.expiringSoon}</Text>
          <Text style={styles.statLabel}>Expirent</Text>
        </View>
        <View style={[styles.statCard, styles.statExpired]}>
          <Ionicons name="close-circle" size={24} color="#ef4444" />
          <Text style={styles.statNumber}>{stats.expired}</Text>
          <Text style={styles.statLabel}>Expirées</Text>
        </View>
      </View>

      {/* Qualifications List */}
      <FlatList
        data={qualifications}
        keyExtractor={(item) => item.id}
        renderItem={renderQualification}
        contentContainerStyle={styles.listContent}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            tintColor="#f97316"
          />
        }
        ListEmptyComponent={
          <View style={styles.emptyState}>
            <Ionicons name="document-text-outline" size={64} color="#64748b" />
            <Text style={styles.emptyTitle}>Aucune qualification</Text>
            <Text style={styles.emptyText}>
              Vos qualifications de soudeur apparaîtront ici.
            </Text>
          </View>
        }
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#0f172a'
  },
  statsContainer: {
    flexDirection: 'row',
    padding: 16,
    gap: 12
  },
  statCard: {
    flex: 1,
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    alignItems: 'center',
    borderWidth: 1
  },
  statValid: {
    borderColor: '#22c55e30'
  },
  statWarning: {
    borderColor: '#f59e0b30'
  },
  statExpired: {
    borderColor: '#ef444430'
  },
  statNumber: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#f1f5f9',
    marginTop: 8
  },
  statLabel: {
    fontSize: 12,
    color: '#94a3b8',
    marginTop: 4
  },
  listContent: {
    padding: 16,
    paddingTop: 0
  },
  qualificationCard: {
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
  processContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8
  },
  processText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#f97316'
  },
  statusBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingVertical: 4,
    paddingHorizontal: 8,
    borderRadius: 12
  },
  statusText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#fff'
  },
  qualificationNumber: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#f1f5f9',
    marginBottom: 4
  },
  standard: {
    fontSize: 14,
    color: '#94a3b8',
    marginBottom: 12
  },
  detailsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 12,
    marginBottom: 12
  },
  detailItem: {
    minWidth: '45%'
  },
  detailLabel: {
    fontSize: 12,
    color: '#64748b',
    marginBottom: 2
  },
  detailValue: {
    fontSize: 14,
    color: '#f1f5f9'
  },
  cardFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: '#334155'
  },
  expiryInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6
  },
  expiryText: {
    fontSize: 13,
    color: '#64748b'
  },
  expiryWarning: {
    color: '#f59e0b'
  },
  aiValidation: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    backgroundColor: '#8b5cf620',
    paddingVertical: 4,
    paddingHorizontal: 8,
    borderRadius: 8
  },
  aiText: {
    fontSize: 12,
    color: '#8b5cf6',
    fontWeight: '600'
  },
  certifyingBody: {
    fontSize: 12,
    color: '#64748b',
    marginTop: 8,
    fontStyle: 'italic'
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
  }
});
