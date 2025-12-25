/**
 * Recent Tasks Screen (Supervisor)
 *
 * Features:
 * - View recent completed tasks (requires online connection)
 * - Filter by user
 * - Filter by time period
 * - View task details
 */

import React, { useState, useEffect } from 'react';
import {
  StyleSheet,
  Text,
  View,
  FlatList,
  Alert,
  TouchableOpacity,
  RefreshControl,
  ActivityIndicator,
} from 'react-native';
import NetInfo from '@react-native-community/netinfo';
import { useAuth } from '@/contexts/AuthContext';

const API_BASE_URL = 'https://laborcontrol-api.azurewebsites.net/api';

type TimeFilter = 'TODAY' | 'WEEK' | 'MONTH';

interface TaskExecution {
  id: string;
  scannedAt: string;
  submittedAt: string;
  controlPointName: string;
  userName: string;
  type: 'SCHEDULED' | 'UNSCHEDULED';
  status: string;
  formDataJson: string;
}

export default function RecentTasksScreen() {
  const { token, user } = useAuth();

  const [tasks, setTasks] = useState<TaskExecution[]>([]);
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [isOnline, setIsOnline] = useState(true);
  const [timeFilter, setTimeFilter] = useState<TimeFilter>('TODAY');

  // ==========================================================================
  // EFFECT: Monitor network connectivity
  // ==========================================================================
  useEffect(() => {
    const unsubscribe = NetInfo.addEventListener((state) => {
      setIsOnline(state.isConnected ?? true);
    });

    return () => unsubscribe();
  }, []);

  // ==========================================================================
  // EFFECT: Load tasks on mount
  // ==========================================================================
  useEffect(() => {
    if (isOnline) {
      loadRecentTasks();
    }
  }, [isOnline, timeFilter]);

  // ==========================================================================
  // FUNCTION: Load recent tasks
  // ==========================================================================
  const loadRecentTasks = async () => {
    if (!isOnline) {
      Alert.alert(
        '📴 Hors ligne',
        'Cette fonctionnalité nécessite une connexion Internet'
      );
      return;
    }

    setRefreshing(true);

    try {
      // Calculate date range
      const now = new Date();
      let startDate: string;

      switch (timeFilter) {
        case 'TODAY':
          startDate = new Date(now.getFullYear(), now.getMonth(), now.getDate()).toISOString();
          break;
        case 'WEEK':
          const weekAgo = new Date(now);
          weekAgo.setDate(weekAgo.getDate() - 7);
          startDate = weekAgo.toISOString();
          break;
        case 'MONTH':
          const monthAgo = new Date(now);
          monthAgo.setDate(monthAgo.getDate() - 30);
          startDate = monthAgo.toISOString();
          break;
      }

      // Fetch recent tasks for this customer
      const response = await fetch(
        `${API_BASE_URL}/taskexecutions/customer/${user?.customerId}?startDate=${startDate}`,
        {
          headers: { Authorization: `Bearer ${token}` },
        }
      );

      if (response.ok) {
        const data = await response.json();
        setTasks(data);
      } else {
        Alert.alert('Erreur', 'Impossible de charger les tâches récentes');
      }
    } catch (error) {
      console.error('[RECENT TASKS] Load error:', error);
      Alert.alert('Erreur', 'Problème de connexion');
    } finally {
      setRefreshing(false);
    }
  };

  // ==========================================================================
  // FUNCTION: Get time filter label
  // ==========================================================================
  const getTimeFilterLabel = (filter: TimeFilter): string => {
    switch (filter) {
      case 'TODAY':
        return "Aujourd'hui";
      case 'WEEK':
        return '7 derniers jours';
      case 'MONTH':
        return '30 derniers jours';
    }
  };

  // ==========================================================================
  // RENDER: Offline state
  // ==========================================================================
  if (!isOnline) {
    return (
      <View style={styles.offlineContainer}>
        <Text style={styles.offlineIcon}>📴</Text>
        <Text style={styles.offlineTitle}>Hors ligne</Text>
        <Text style={styles.offlineText}>
          Cette fonctionnalité nécessite une connexion Internet pour afficher l'activité
          récente.
        </Text>
      </View>
    );
  }

  // ==========================================================================
  // RENDER
  // ==========================================================================
  return (
    <View style={styles.container}>
      {/* TIME FILTER */}
      <View style={styles.filterContainer}>
        {(['TODAY', 'WEEK', 'MONTH'] as TimeFilter[]).map((filter) => (
          <TouchableOpacity
            key={filter}
            style={[
              styles.filterButton,
              timeFilter === filter && styles.filterButtonActive,
            ]}
            onPress={() => setTimeFilter(filter)}
          >
            <Text
              style={[
                styles.filterButtonText,
                timeFilter === filter && styles.filterButtonTextActive,
              ]}
            >
              {getTimeFilterLabel(filter)}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      {/* STATS SUMMARY */}
      <View style={styles.statsContainer}>
        <View style={styles.statCard}>
          <Text style={styles.statValue}>{tasks.length}</Text>
          <Text style={styles.statLabel}>Tâches réalisées</Text>
        </View>
        <View style={styles.statCard}>
          <Text style={styles.statValue}>
            {tasks.filter((t) => t.type === 'SCHEDULED').length}
          </Text>
          <Text style={styles.statLabel}>Planifiées</Text>
        </View>
        <View style={styles.statCard}>
          <Text style={styles.statValue}>
            {tasks.filter((t) => t.type === 'UNSCHEDULED').length}
          </Text>
          <Text style={styles.statLabel}>Libres</Text>
        </View>
      </View>

      {/* TASKS LIST */}
      <FlatList
        data={tasks}
        keyExtractor={(item) => item.id}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={loadRecentTasks} />
        }
        renderItem={({ item }) => {
          const formData = JSON.parse(item.formDataJson || '{}');
          const scanDate = new Date(item.scannedAt);

          return (
            <View style={styles.card}>
              <View style={styles.cardHeader}>
                <Text style={styles.cardTitle}>{item.controlPointName}</Text>
                <View
                  style={[
                    styles.badge,
                    {
                      backgroundColor:
                        item.type === 'SCHEDULED' ? '#3b82f6' : '#10b981',
                    },
                  ]}
                >
                  <Text style={styles.badgeText}>
                    {item.type === 'SCHEDULED' ? 'Planifiée' : 'Libre'}
                  </Text>
                </View>
              </View>

              <Text style={styles.cardUser}>👤 {item.userName}</Text>

              <Text style={styles.cardTime}>
                📅 {scanDate.toLocaleDateString('fr-FR')} à{' '}
                {scanDate.toLocaleTimeString('fr-FR', {
                  hour: '2-digit',
                  minute: '2-digit',
                })}
              </Text>

              {formData.etatGeneral && (
                <Text style={styles.cardInfo}>
                  État:{' '}
                  {formData.etatGeneral === 'OK'
                    ? '✅ OK'
                    : formData.etatGeneral === 'A_SURVEILLER'
                    ? '⚠️ À surveiller'
                    : '🚨 Alerter IDE'}
                </Text>
              )}

              {formData.observations && (
                <Text style={styles.cardObservations} numberOfLines={2}>
                  💬 {formData.observations}
                </Text>
              )}

              {formData.interceptedBy && (
                <View style={styles.interceptedBadge}>
                  <Text style={styles.interceptedText}>
                    ⚠️ Interceptée par {formData.interceptedBy}
                  </Text>
                </View>
              )}
            </View>
          );
        }}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            {refreshing ? (
              <ActivityIndicator size="large" color="#f59e0b" />
            ) : (
              <>
                <Text style={styles.emptyText}>Aucune tâche récente</Text>
                <Text style={styles.emptySubtext}>
                  {getTimeFilterLabel(timeFilter)}
                </Text>
              </>
            )}
          </View>
        }
        contentContainerStyle={styles.listContent}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8fafc' },
  offlineContainer: {
    flex: 1,
    backgroundColor: '#f8fafc',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 40,
  },
  offlineIcon: {
    fontSize: 72,
    marginBottom: 20,
  },
  offlineTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#1e293b',
    marginBottom: 12,
  },
  offlineText: {
    fontSize: 16,
    color: '#64748b',
    textAlign: 'center',
    lineHeight: 24,
  },
  filterContainer: {
    flexDirection: 'row',
    padding: 16,
    gap: 8,
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#e2e8f0',
  },
  filterButton: {
    flex: 1,
    paddingVertical: 10,
    borderRadius: 8,
    borderWidth: 2,
    borderColor: '#e2e8f0',
    alignItems: 'center',
  },
  filterButtonActive: {
    borderColor: '#f59e0b',
    backgroundColor: '#fef3c7',
  },
  filterButtonText: {
    fontSize: 13,
    fontWeight: '600',
    color: '#64748b',
  },
  filterButtonTextActive: {
    color: '#f59e0b',
    fontWeight: 'bold',
  },
  statsContainer: {
    flexDirection: 'row',
    padding: 16,
    gap: 12,
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#e2e8f0',
  },
  statCard: {
    flex: 1,
    backgroundColor: '#fef3c7',
    borderRadius: 12,
    padding: 16,
    alignItems: 'center',
  },
  statValue: {
    fontSize: 32,
    fontWeight: 'bold',
    color: '#f59e0b',
    marginBottom: 4,
  },
  statLabel: {
    fontSize: 12,
    color: '#92400e',
    textAlign: 'center',
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
    marginBottom: 12,
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
  cardUser: {
    fontSize: 14,
    color: '#64748b',
    marginBottom: 8,
  },
  cardTime: {
    fontSize: 14,
    color: '#f59e0b',
    fontWeight: '600',
    marginBottom: 8,
  },
  cardInfo: {
    fontSize: 14,
    color: '#64748b',
    marginBottom: 8,
  },
  cardObservations: {
    fontSize: 13,
    color: '#94a3b8',
    marginTop: 8,
    lineHeight: 18,
  },
  interceptedBadge: {
    marginTop: 12,
    backgroundColor: '#fef3c7',
    paddingVertical: 8,
    paddingHorizontal: 12,
    borderRadius: 8,
  },
  interceptedText: {
    fontSize: 12,
    color: '#d97706',
    fontWeight: '600',
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
});
