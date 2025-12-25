/**
 * Welder - My Welds Screen
 *
 * Main screen for welder to see assigned welds
 * Features:
 * - List of assigned welds with status
 * - Filter by status (planned, in progress, pending NDT)
 * - Quick access to NFC scan for weld execution
 */

import React, { useEffect, useState, useCallback } from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  TouchableOpacity,
  RefreshControl,
  Alert
} from 'react-native';
import { useRouter } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';
import { useAuth } from '@/contexts/AuthContext';
import LoadingSpinner from '@/components/shared/LoadingSpinner';
import { apiClient } from '@/services/api';

interface Weld {
  id: string;
  reference: string;
  assetName: string;
  weldingProcess: string;
  jointType: string;
  status: string;
  plannedDate: string | null;
  executionDate: string | null;
  isCCPUValidated: boolean;
  isBlocked: boolean;
  firstScanAt: string | null;
  secondScanAt: string | null;
}

type FilterType = 'all' | 'planned' | 'in_progress' | 'pending_ndt';

const STATUS_COLORS: Record<string, string> = {
  'PLANNED': '#3b82f6',
  'CCPU_VALIDATED': '#10b981',
  'IN_PROGRESS': '#f97316',
  'PENDING_NDT': '#8b5cf6',
  'IN_CONTROL': '#6366f1',
  'COMPLETED': '#22c55e',
  'NON_CONFORM': '#ef4444',
  'BLOCKED': '#dc2626'
};

const STATUS_LABELS: Record<string, string> = {
  'PLANNED': 'Planifiée',
  'CCPU_VALIDATED': 'Validée CCPU',
  'IN_PROGRESS': 'En cours',
  'PENDING_NDT': 'Attente CND',
  'IN_CONTROL': 'En contrôle',
  'COMPLETED': 'Terminée',
  'NON_CONFORM': 'Non conforme',
  'BLOCKED': 'Bloquée'
};

export default function WeldsScreen() {
  const router = useRouter();
  const { user, token } = useAuth();

  const [welds, setWelds] = useState<Weld[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [filter, setFilter] = useState<FilterType>('all');

  // Fetch welds
  const fetchWelds = useCallback(async (showLoading = true) => {
    if (!token) return;

    if (showLoading) setLoading(true);
    try {
      const response = await apiClient.get('/welds', {
        headers: { Authorization: `Bearer ${token}` },
        params: { welderId: user?.id }
      });
      setWelds(response.data);
    } catch (error) {
      console.error('Error fetching welds:', error);
      Alert.alert('Erreur', 'Impossible de charger les soudures');
    } finally {
      setLoading(false);
    }
  }, [token, user?.id]);

  useEffect(() => {
    fetchWelds();
  }, [fetchWelds]);

  const handleRefresh = async () => {
    setRefreshing(true);
    await fetchWelds(false);
    setRefreshing(false);
  };

  // Filter welds
  const filteredWelds = React.useMemo(() => {
    switch (filter) {
      case 'planned':
        return welds.filter(w => w.status === 'PLANNED' || w.status === 'CCPU_VALIDATED');
      case 'in_progress':
        return welds.filter(w => w.status === 'IN_PROGRESS');
      case 'pending_ndt':
        return welds.filter(w => w.status === 'PENDING_NDT' || w.status === 'IN_CONTROL');
      default:
        return welds;
    }
  }, [welds, filter]);

  // Handle weld press - navigate to detail/execution
  const handleWeldPress = (weld: Weld) => {
    if (weld.isBlocked) {
      Alert.alert('Soudure bloquée', 'Cette soudure est bloquée et ne peut pas être exécutée.');
      return;
    }

    if (!weld.isCCPUValidated && weld.status === 'PLANNED') {
      Alert.alert('Attente validation', 'Cette soudure attend la validation du CCPU.');
      return;
    }

    router.push(`/(welder)/weld-detail?id=${weld.id}`);
  };

  // Render weld card
  const renderWeldCard = ({ item }: { item: Weld }) => (
    <TouchableOpacity
      style={[styles.card, item.isBlocked && styles.cardBlocked]}
      onPress={() => handleWeldPress(item)}
      disabled={item.isBlocked}
    >
      <View style={styles.cardHeader}>
        <View style={styles.referenceContainer}>
          <Ionicons name="flame" size={20} color="#f97316" />
          <Text style={styles.reference}>{item.reference}</Text>
        </View>
        <View style={[styles.statusBadge, { backgroundColor: STATUS_COLORS[item.status] || '#64748b' }]}>
          <Text style={styles.statusText}>{STATUS_LABELS[item.status] || item.status}</Text>
        </View>
      </View>

      <View style={styles.cardBody}>
        <View style={styles.infoRow}>
          <Ionicons name="cube-outline" size={16} color="#64748b" />
          <Text style={styles.infoText}>{item.assetName}</Text>
        </View>

        <View style={styles.infoRow}>
          <Ionicons name="settings-outline" size={16} color="#64748b" />
          <Text style={styles.infoText}>{item.weldingProcess} - {item.jointType}</Text>
        </View>

        {item.plannedDate && (
          <View style={styles.infoRow}>
            <Ionicons name="calendar-outline" size={16} color="#64748b" />
            <Text style={styles.infoText}>
              Planifiée: {new Date(item.plannedDate).toLocaleDateString('fr-FR')}
            </Text>
          </View>
        )}
      </View>

      {/* Progress indicator for in-progress welds */}
      {item.status === 'IN_PROGRESS' && (
        <View style={styles.progressContainer}>
          <View style={styles.scanIndicator}>
            <Ionicons
              name={item.firstScanAt ? 'checkmark-circle' : 'ellipse-outline'}
              size={20}
              color={item.firstScanAt ? '#22c55e' : '#94a3b8'}
            />
            <Text style={styles.scanText}>Scan début</Text>
          </View>
          <View style={styles.progressLine} />
          <View style={styles.scanIndicator}>
            <Ionicons
              name={item.secondScanAt ? 'checkmark-circle' : 'ellipse-outline'}
              size={20}
              color={item.secondScanAt ? '#22c55e' : '#94a3b8'}
            />
            <Text style={styles.scanText}>Scan fin</Text>
          </View>
        </View>
      )}

      {/* Blocked warning */}
      {item.isBlocked && (
        <View style={styles.blockedWarning}>
          <Ionicons name="warning" size={16} color="#dc2626" />
          <Text style={styles.blockedText}>Soudure bloquée</Text>
        </View>
      )}
    </TouchableOpacity>
  );

  // Loading state
  if (loading && welds.length === 0) {
    return <LoadingSpinner message="Chargement des soudures..." />;
  }

  return (
    <View style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.title}>Mes Soudures</Text>
        <Text style={styles.subtitle}>
          {user ? `${user.firstName} ${user.lastName}` : 'Soudeur'}
        </Text>
      </View>

      {/* Filters */}
      <View style={styles.filterContainer}>
        {[
          { key: 'all', label: 'Toutes' },
          { key: 'planned', label: 'Planifiées' },
          { key: 'in_progress', label: 'En cours' },
          { key: 'pending_ndt', label: 'Attente CND' }
        ].map(({ key, label }) => (
          <TouchableOpacity
            key={key}
            style={[styles.filterButton, filter === key && styles.filterButtonActive]}
            onPress={() => setFilter(key as FilterType)}
          >
            <Text style={[styles.filterText, filter === key && styles.filterTextActive]}>
              {label}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      {/* Count */}
      <View style={styles.countContainer}>
        <Text style={styles.countText}>
          {filteredWelds.length} soudure{filteredWelds.length !== 1 ? 's' : ''}
        </Text>
      </View>

      {/* Weld list */}
      <FlatList
        data={filteredWelds}
        keyExtractor={(item) => item.id}
        renderItem={renderWeldCard}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Ionicons name="flame-outline" size={48} color="#94a3b8" />
            <Text style={styles.emptyText}>Aucune soudure assignée</Text>
          </View>
        }
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={handleRefresh} />
        }
        contentContainerStyle={styles.listContent}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#0f172a'
  },
  header: {
    backgroundColor: '#1e293b',
    padding: 20,
    paddingTop: 60,
    borderBottomWidth: 1,
    borderBottomColor: '#334155'
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#f97316',
    marginBottom: 4
  },
  subtitle: {
    fontSize: 14,
    color: '#94a3b8'
  },
  filterContainer: {
    flexDirection: 'row',
    padding: 12,
    gap: 8,
    backgroundColor: '#1e293b'
  },
  filterButton: {
    flex: 1,
    paddingVertical: 8,
    paddingHorizontal: 8,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#334155',
    backgroundColor: '#0f172a',
    alignItems: 'center'
  },
  filterButtonActive: {
    backgroundColor: '#f97316',
    borderColor: '#f97316'
  },
  filterText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#94a3b8'
  },
  filterTextActive: {
    color: '#fff'
  },
  countContainer: {
    paddingHorizontal: 16,
    paddingVertical: 8,
    backgroundColor: '#1e293b'
  },
  countText: {
    fontSize: 13,
    color: '#64748b',
    fontWeight: '500'
  },
  listContent: {
    padding: 16
  },
  card: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    borderWidth: 1,
    borderColor: '#334155'
  },
  cardBlocked: {
    opacity: 0.6,
    borderColor: '#dc2626'
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12
  },
  referenceContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8
  },
  reference: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#f1f5f9'
  },
  statusBadge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12
  },
  statusText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#fff'
  },
  cardBody: {
    gap: 8
  },
  infoRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8
  },
  infoText: {
    fontSize: 14,
    color: '#94a3b8'
  },
  progressContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 16,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: '#334155'
  },
  scanIndicator: {
    alignItems: 'center',
    gap: 4
  },
  scanText: {
    fontSize: 11,
    color: '#64748b'
  },
  progressLine: {
    flex: 1,
    height: 2,
    backgroundColor: '#334155',
    marginHorizontal: 16
  },
  blockedWarning: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: '#dc2626'
  },
  blockedText: {
    fontSize: 13,
    color: '#dc2626',
    fontWeight: '600'
  },
  emptyContainer: {
    padding: 40,
    alignItems: 'center',
    gap: 12
  },
  emptyText: {
    fontSize: 16,
    color: '#64748b',
    textAlign: 'center'
  }
});
