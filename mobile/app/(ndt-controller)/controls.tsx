/**
 * NDT Controller - Controls List Screen
 *
 * Displays pending NDT controls for the controller
 * Filters by control type (VT, PT, MT, RT, UT)
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
import { useRouter } from 'expo-router';
import { useAuth } from '@/contexts/AuthContext';
import LoadingSpinner from '@/components/shared/LoadingSpinner';
import { apiClient } from '@/services/api';

interface NDTControl {
  id: string;
  weldReference: string;
  assetName: string;
  controlType: 'VT' | 'PT' | 'MT' | 'RT' | 'UT';
  status: string;
  priority: 'NORMAL' | 'URGENT' | 'CRITICAL';
  scheduledDate: string | null;
  weldExecutionDate: string | null;
  acceptanceCriteria: string | null;
}

type ControlTypeFilter = 'ALL' | 'VT' | 'PT' | 'MT' | 'RT' | 'UT';

const CONTROL_TYPE_CONFIG = {
  VT: { label: 'Visuel', color: '#22c55e', icon: 'eye' },
  PT: { label: 'Ressuage', color: '#f59e0b', icon: 'water' },
  MT: { label: 'Magnéto', color: '#8b5cf6', icon: 'magnet' },
  RT: { label: 'Radio', color: '#ef4444', icon: 'radio' },
  UT: { label: 'Ultrasons', color: '#3b82f6', icon: 'pulse' }
};

export default function ControlsScreen() {
  const { token } = useAuth();
  const router = useRouter();

  const [controls, setControls] = useState<NDTControl[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [filter, setFilter] = useState<ControlTypeFilter>('ALL');

  const fetchControls = useCallback(async () => {
    try {
      const params = new URLSearchParams({
        status: 'PENDING',
        ...(filter !== 'ALL' && { controlType: filter })
      });

      const response = await apiClient.get(`/ndt-controls/my-controls?${params}`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      setControls(response.data);
    } catch (error) {
      console.error('Error fetching controls:', error);
      Alert.alert('Erreur', 'Impossible de charger les contrôles.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [token, filter]);

  useEffect(() => {
    fetchControls();
  }, [fetchControls]);

  const onRefresh = () => {
    setRefreshing(true);
    fetchControls();
  };

  const getPriorityConfig = (priority: string) => {
    switch (priority) {
      case 'CRITICAL':
        return { color: '#ef4444', label: 'Critique' };
      case 'URGENT':
        return { color: '#f59e0b', label: 'Urgent' };
      default:
        return { color: '#64748b', label: 'Normal' };
    }
  };

  const renderControl = ({ item }: { item: NDTControl }) => {
    const typeConfig = CONTROL_TYPE_CONFIG[item.controlType];
    const priorityConfig = getPriorityConfig(item.priority);

    return (
      <TouchableOpacity
        style={styles.controlCard}
        onPress={() => router.push(`/(ndt-controller)/control-detail?id=${item.id}`)}
      >
        <View style={styles.cardHeader}>
          <View style={[styles.typeBadge, { backgroundColor: typeConfig.color }]}>
            <Ionicons name={typeConfig.icon as any} size={16} color="#fff" />
            <Text style={styles.typeText}>{typeConfig.label}</Text>
          </View>
          {item.priority !== 'NORMAL' && (
            <View style={[styles.priorityBadge, { backgroundColor: priorityConfig.color }]}>
              <Text style={styles.priorityText}>{priorityConfig.label}</Text>
            </View>
          )}
        </View>

        <Text style={styles.weldReference}>{item.weldReference}</Text>
        <Text style={styles.assetName}>{item.assetName}</Text>

        <View style={styles.cardFooter}>
          {item.weldExecutionDate && (
            <View style={styles.footerItem}>
              <Ionicons name="flame" size={14} color="#f97316" />
              <Text style={styles.footerText}>
                Soudé: {new Date(item.weldExecutionDate).toLocaleDateString('fr-FR')}
              </Text>
            </View>
          )}
          {item.scheduledDate && (
            <View style={styles.footerItem}>
              <Ionicons name="calendar" size={14} color="#3b82f6" />
              <Text style={styles.footerText}>
                Prévu: {new Date(item.scheduledDate).toLocaleDateString('fr-FR')}
              </Text>
            </View>
          )}
        </View>

        <View style={styles.actionRow}>
          <TouchableOpacity
            style={styles.actionButton}
            onPress={() => router.push(`/(ndt-controller)/execute-control?id=${item.id}`)}
          >
            <Ionicons name="play-circle" size={20} color="#3b82f6" />
            <Text style={styles.actionButtonText}>Exécuter</Text>
          </TouchableOpacity>
        </View>
      </TouchableOpacity>
    );
  };

  const renderFilterButtons = () => (
    <View style={styles.filterContainer}>
      <TouchableOpacity
        style={[styles.filterButton, filter === 'ALL' && styles.filterButtonActive]}
        onPress={() => setFilter('ALL')}
      >
        <Text style={[styles.filterText, filter === 'ALL' && styles.filterTextActive]}>
          Tous
        </Text>
      </TouchableOpacity>
      {Object.entries(CONTROL_TYPE_CONFIG).map(([type, config]) => (
        <TouchableOpacity
          key={type}
          style={[
            styles.filterButton,
            filter === type && styles.filterButtonActive,
            filter === type && { borderColor: config.color }
          ]}
          onPress={() => setFilter(type as ControlTypeFilter)}
        >
          <Ionicons
            name={config.icon as any}
            size={14}
            color={filter === type ? config.color : '#64748b'}
          />
          <Text style={[
            styles.filterText,
            filter === type && { color: config.color }
          ]}>
            {type}
          </Text>
        </TouchableOpacity>
      ))}
    </View>
  );

  if (loading) {
    return <LoadingSpinner message="Chargement des contrôles..." />;
  }

  return (
    <View style={styles.container}>
      {renderFilterButtons()}

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
            <Ionicons name="clipboard-outline" size={64} color="#64748b" />
            <Text style={styles.emptyTitle}>Aucun contrôle en attente</Text>
            <Text style={styles.emptyText}>
              Les contrôles CND à effectuer apparaîtront ici.
            </Text>
          </View>
        }
        ListHeaderComponent={
          <View style={styles.countHeader}>
            <Text style={styles.countText}>
              {controls.length} contrôle{controls.length !== 1 ? 's' : ''} en attente
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
    gap: 8,
    flexWrap: 'wrap'
  },
  filterButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingVertical: 8,
    paddingHorizontal: 12,
    borderRadius: 20,
    backgroundColor: '#1e293b',
    borderWidth: 1,
    borderColor: 'transparent'
  },
  filterButtonActive: {
    borderColor: '#3b82f6'
  },
  filterText: {
    fontSize: 13,
    color: '#64748b',
    fontWeight: '500'
  },
  filterTextActive: {
    color: '#3b82f6'
  },
  listContent: {
    padding: 16,
    paddingTop: 0
  },
  countHeader: {
    marginBottom: 16
  },
  countText: {
    fontSize: 14,
    color: '#64748b'
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
    gap: 6,
    paddingVertical: 4,
    paddingHorizontal: 10,
    borderRadius: 12
  },
  typeText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#fff'
  },
  priorityBadge: {
    paddingVertical: 4,
    paddingHorizontal: 8,
    borderRadius: 8
  },
  priorityText: {
    fontSize: 11,
    fontWeight: '600',
    color: '#fff',
    textTransform: 'uppercase'
  },
  weldReference: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#f1f5f9',
    marginBottom: 4
  },
  assetName: {
    fontSize: 14,
    color: '#94a3b8',
    marginBottom: 12
  },
  cardFooter: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 16,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: '#334155'
  },
  footerItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6
  },
  footerText: {
    fontSize: 13,
    color: '#64748b'
  },
  actionRow: {
    marginTop: 12
  },
  actionButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    backgroundColor: '#3b82f620',
    paddingVertical: 10,
    paddingHorizontal: 16,
    borderRadius: 8
  },
  actionButtonText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#3b82f6'
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
