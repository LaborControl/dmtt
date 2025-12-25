/**
 * NDT Controller - History Screen
 *
 * Shows completed NDT controls with results
 * Filterable by date range and control type
 */

import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  RefreshControl
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useAuth } from '@/contexts/AuthContext';
import LoadingSpinner from '@/components/shared/LoadingSpinner';
import { apiClient } from '@/services/api';

interface ControlHistory {
  id: string;
  weldReference: string;
  assetName: string;
  controlType: 'VT' | 'PT' | 'MT' | 'RT' | 'UT';
  result: 'PASSED' | 'FAILED';
  executionDate: string;
  observations: string | null;
  nonConformityCreated: boolean;
  nonConformityReference: string | null;
}

type DateFilter = 'TODAY' | 'WEEK' | 'MONTH' | 'ALL';

const CONTROL_TYPE_CONFIG = {
  VT: { label: 'VT', color: '#22c55e', icon: 'eye' },
  PT: { label: 'PT', color: '#f59e0b', icon: 'water' },
  MT: { label: 'MT', color: '#8b5cf6', icon: 'magnet' },
  RT: { label: 'RT', color: '#ef4444', icon: 'radio' },
  UT: { label: 'UT', color: '#3b82f6', icon: 'pulse' }
};

export default function HistoryScreen() {
  const { token } = useAuth();

  const [controls, setControls] = useState<ControlHistory[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [dateFilter, setDateFilter] = useState<DateFilter>('WEEK');

  const getDateRange = (filter: DateFilter) => {
    const now = new Date();
    let startDate: Date;

    switch (filter) {
      case 'TODAY':
        startDate = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        break;
      case 'WEEK':
        startDate = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
        break;
      case 'MONTH':
        startDate = new Date(now.getFullYear(), now.getMonth(), 1);
        break;
      default:
        return null;
    }

    return startDate.toISOString();
  };

  const fetchHistory = useCallback(async () => {
    try {
      const params = new URLSearchParams({ status: 'COMPLETED' });
      const startDate = getDateRange(dateFilter);
      if (startDate) {
        params.append('fromDate', startDate);
      }

      const response = await apiClient.get(`/ndt-controls/my-controls?${params}`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      setControls(response.data);
    } catch (error) {
      console.error('Error fetching history:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [token, dateFilter]);

  useEffect(() => {
    fetchHistory();
  }, [fetchHistory]);

  const onRefresh = () => {
    setRefreshing(true);
    fetchHistory();
  };

  const getStatistics = () => {
    const total = controls.length;
    const passed = controls.filter(c => c.result === 'PASSED').length;
    const failed = controls.filter(c => c.result === 'FAILED').length;
    const successRate = total > 0 ? Math.round((passed / total) * 100) : 0;
    return { total, passed, failed, successRate };
  };

  const renderControl = ({ item }: { item: ControlHistory }) => {
    const typeConfig = CONTROL_TYPE_CONFIG[item.controlType];

    return (
      <View style={styles.controlCard}>
        <View style={styles.cardHeader}>
          <View style={[styles.typeBadge, { backgroundColor: typeConfig.color }]}>
            <Ionicons name={typeConfig.icon as any} size={14} color="#fff" />
            <Text style={styles.typeText}>{typeConfig.label}</Text>
          </View>
          <View style={[
            styles.resultBadge,
            item.result === 'PASSED' ? styles.passedBadge : styles.failedBadge
          ]}>
            <Ionicons
              name={item.result === 'PASSED' ? 'checkmark-circle' : 'close-circle'}
              size={14}
              color="#fff"
            />
            <Text style={styles.resultText}>
              {item.result === 'PASSED' ? 'Conforme' : 'Non conforme'}
            </Text>
          </View>
        </View>

        <Text style={styles.weldReference}>{item.weldReference}</Text>
        <Text style={styles.assetName}>{item.assetName}</Text>

        <View style={styles.cardFooter}>
          <View style={styles.dateInfo}>
            <Ionicons name="calendar" size={14} color="#64748b" />
            <Text style={styles.dateText}>
              {new Date(item.executionDate).toLocaleDateString('fr-FR', {
                day: '2-digit',
                month: 'short',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
              })}
            </Text>
          </View>
        </View>

        {item.observations && (
          <View style={styles.observationsSection}>
            <Text style={styles.observationsLabel}>Observations:</Text>
            <Text style={styles.observationsText}>{item.observations}</Text>
          </View>
        )}

        {item.nonConformityCreated && (
          <View style={styles.ncBadge}>
            <Ionicons name="warning" size={14} color="#ef4444" />
            <Text style={styles.ncText}>
              FNC: {item.nonConformityReference || 'En cours'}
            </Text>
          </View>
        )}
      </View>
    );
  };

  if (loading) {
    return <LoadingSpinner message="Chargement de l'historique..." />;
  }

  const stats = getStatistics();

  return (
    <View style={styles.container}>
      {/* Date Filter */}
      <View style={styles.filterContainer}>
        {(['TODAY', 'WEEK', 'MONTH', 'ALL'] as DateFilter[]).map((filter) => (
          <TouchableOpacity
            key={filter}
            style={[styles.filterButton, dateFilter === filter && styles.filterButtonActive]}
            onPress={() => setDateFilter(filter)}
          >
            <Text style={[styles.filterText, dateFilter === filter && styles.filterTextActive]}>
              {filter === 'TODAY' ? "Aujourd'hui" :
               filter === 'WEEK' ? '7 jours' :
               filter === 'MONTH' ? 'Ce mois' : 'Tout'}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      {/* Statistics */}
      <View style={styles.statsContainer}>
        <View style={styles.statItem}>
          <Text style={styles.statValue}>{stats.total}</Text>
          <Text style={styles.statLabel}>Total</Text>
        </View>
        <View style={styles.statDivider} />
        <View style={styles.statItem}>
          <Text style={[styles.statValue, { color: '#22c55e' }]}>{stats.passed}</Text>
          <Text style={styles.statLabel}>Conformes</Text>
        </View>
        <View style={styles.statDivider} />
        <View style={styles.statItem}>
          <Text style={[styles.statValue, { color: '#ef4444' }]}>{stats.failed}</Text>
          <Text style={styles.statLabel}>Non conf.</Text>
        </View>
        <View style={styles.statDivider} />
        <View style={styles.statItem}>
          <Text style={[styles.statValue, { color: '#3b82f6' }]}>{stats.successRate}%</Text>
          <Text style={styles.statLabel}>Taux</Text>
        </View>
      </View>

      {/* History List */}
      <FlatList
        data={controls}
        keyExtractor={(item) => item.id}
        renderItem={renderControl}
        contentContainerStyle={styles.listContent}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            tintColor="#3b82f6"
          />
        }
        ListEmptyComponent={
          <View style={styles.emptyState}>
            <Ionicons name="time-outline" size={64} color="#64748b" />
            <Text style={styles.emptyTitle}>Aucun contrôle</Text>
            <Text style={styles.emptyText}>
              Vos contrôles effectués apparaîtront ici.
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
  filterContainer: {
    flexDirection: 'row',
    padding: 16,
    gap: 8
  },
  filterButton: {
    flex: 1,
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderRadius: 8,
    backgroundColor: '#1e293b',
    alignItems: 'center'
  },
  filterButtonActive: {
    backgroundColor: '#3b82f6'
  },
  filterText: {
    fontSize: 13,
    fontWeight: '500',
    color: '#64748b'
  },
  filterTextActive: {
    color: '#fff'
  },
  statsContainer: {
    flexDirection: 'row',
    backgroundColor: '#1e293b',
    marginHorizontal: 16,
    borderRadius: 12,
    padding: 16,
    marginBottom: 16
  },
  statItem: {
    flex: 1,
    alignItems: 'center'
  },
  statDivider: {
    width: 1,
    backgroundColor: '#334155'
  },
  statValue: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#f1f5f9'
  },
  statLabel: {
    fontSize: 11,
    color: '#64748b',
    marginTop: 4
  },
  listContent: {
    padding: 16,
    paddingTop: 0
  },
  controlCard: {
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
  typeBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingVertical: 4,
    paddingHorizontal: 8,
    borderRadius: 8
  },
  typeText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#fff'
  },
  resultBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingVertical: 4,
    paddingHorizontal: 8,
    borderRadius: 8
  },
  passedBadge: {
    backgroundColor: '#22c55e'
  },
  failedBadge: {
    backgroundColor: '#ef4444'
  },
  resultText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#fff'
  },
  weldReference: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#f1f5f9'
  },
  assetName: {
    fontSize: 14,
    color: '#94a3b8',
    marginTop: 4
  },
  cardFooter: {
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
  observationsSection: {
    marginTop: 12,
    padding: 12,
    backgroundColor: '#0f172a',
    borderRadius: 8
  },
  observationsLabel: {
    fontSize: 12,
    fontWeight: '600',
    color: '#64748b',
    marginBottom: 4
  },
  observationsText: {
    fontSize: 13,
    color: '#94a3b8'
  },
  ncBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    marginTop: 12,
    padding: 10,
    backgroundColor: '#ef444420',
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#ef444440'
  },
  ncText: {
    fontSize: 13,
    color: '#ef4444',
    fontWeight: '600'
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
