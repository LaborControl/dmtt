/**
 * Supervisor - Team View Screen (Phase 2)
 *
 * Features:
 * - View all team members
 * - See their current tasks
 * - Filter by status, technician, date
 * - Real-time updates
 * - Task reassignment quick access
 */

import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  TouchableOpacity,
  RefreshControl,
  ActivityIndicator
} from 'react-native';
import { useAuth } from '@/contexts/AuthContext';
import { useTaskStore, selectTasksByStatus } from '@/store';
import TaskCard from '@/components/tasks/TaskCard';
import LoadingSpinner from '@/components/shared/LoadingSpinner';

// Types for team view
interface TeamMember {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  taskCount: number;
  pendingCount: number;
  inProgressCount: number;
  overdueCount: number;
}

export default function TeamScreen() {
  const { user, token } = useAuth();
  const { tasks, loading, fetchTasks } = useTaskStore();

  const [selectedFilter, setSelectedFilter] = useState<'all' | 'pending' | 'in_progress' | 'overdue'>('all');
  const [selectedTechnician, setSelectedTechnician] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  // ==========================================================================
  // Fetch tasks on mount
  // ==========================================================================
  useEffect(() => {
    if (user && token) {
      fetchTasks(user.customerId, token); // Fetch all team tasks
    }
  }, [user, token]);

  // ==========================================================================
  // Refresh handler
  // ==========================================================================
  const handleRefresh = async () => {
    setRefreshing(true);
    if (user && token) {
      await fetchTasks(user.customerId, token, true); // Force refresh
    }
    setRefreshing(false);
  };

  // ==========================================================================
  // Filter tasks
  // ==========================================================================
  const filteredTasks = React.useMemo(() => {
    let filtered = tasks;

    // Filter by status
    if (selectedFilter !== 'all') {
      const statusMap = {
        pending: 'PENDING',
        in_progress: 'IN_PROGRESS',
        overdue: 'OVERDUE'
      };
      filtered = filtered.filter(task => task.status === statusMap[selectedFilter]);
    }

    // Filter by technician
    if (selectedTechnician) {
      filtered = filtered.filter(task => task.assignedTo?.id === selectedTechnician);
    }

    return filtered;
  }, [tasks, selectedFilter, selectedTechnician]);

  // ==========================================================================
  // Calculate team stats
  // ==========================================================================
  const teamStats = React.useMemo(() => {
    const pending = tasks.filter(t => t.status === 'PENDING').length;
    const inProgress = tasks.filter(t => t.status === 'IN_PROGRESS').length;
    const overdue = tasks.filter(t => t.status === 'OVERDUE').length;
    const completed = tasks.filter(t => t.status === 'COMPLETED').length;

    return { pending, inProgress, overdue, completed, total: tasks.length };
  }, [tasks]);

  // ==========================================================================
  // Render stats cards
  // ==========================================================================
  const renderStatsCard = () => (
    <View style={styles.statsContainer}>
      <View style={styles.statCard}>
        <Text style={styles.statNumber}>{teamStats.total}</Text>
        <Text style={styles.statLabel}>Total</Text>
      </View>
      <View style={[styles.statCard, styles.statPending]}>
        <Text style={styles.statNumber}>{teamStats.pending}</Text>
        <Text style={styles.statLabel}>En attente</Text>
      </View>
      <View style={[styles.statCard, styles.statInProgress]}>
        <Text style={styles.statNumber}>{teamStats.inProgress}</Text>
        <Text style={styles.statLabel}>En cours</Text>
      </View>
      <View style={[styles.statCard, styles.statOverdue]}>
        <Text style={styles.statNumber}>{teamStats.overdue}</Text>
        <Text style={styles.statLabel}>En retard</Text>
      </View>
    </View>
  );

  // ==========================================================================
  // Render filter buttons
  // ==========================================================================
  const renderFilters = () => (
    <View style={styles.filterContainer}>
      <TouchableOpacity
        style={[styles.filterButton, selectedFilter === 'all' && styles.filterButtonActive]}
        onPress={() => setSelectedFilter('all')}
      >
        <Text style={[styles.filterText, selectedFilter === 'all' && styles.filterTextActive]}>
          Toutes
        </Text>
      </TouchableOpacity>
      <TouchableOpacity
        style={[styles.filterButton, selectedFilter === 'pending' && styles.filterButtonActive]}
        onPress={() => setSelectedFilter('pending')}
      >
        <Text style={[styles.filterText, selectedFilter === 'pending' && styles.filterTextActive]}>
          En attente
        </Text>
      </TouchableOpacity>
      <TouchableOpacity
        style={[styles.filterButton, selectedFilter === 'in_progress' && styles.filterButtonActive]}
        onPress={() => setSelectedFilter('in_progress')}
      >
        <Text style={[styles.filterText, selectedFilter === 'in_progress' && styles.filterTextActive]}>
          En cours
        </Text>
      </TouchableOpacity>
      <TouchableOpacity
        style={[styles.filterButton, selectedFilter === 'overdue' && styles.filterButtonActive]}
        onPress={() => setSelectedFilter('overdue')}
      >
        <Text style={[styles.filterText, selectedFilter === 'overdue' && styles.filterTextActive]}>
          En retard
        </Text>
      </TouchableOpacity>
    </View>
  );

  // ==========================================================================
  // Loading state
  // ==========================================================================
  if (loading && tasks.length === 0) {
    return <LoadingSpinner message="Chargement de l'équipe..." />;
  }

  // ==========================================================================
  // Main render
  // ==========================================================================
  return (
    <View style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.title}>Vue Équipe</Text>
        <Text style={styles.subtitle}>Supervision des tâches en temps réel</Text>
      </View>

      {/* Stats Cards */}
      {renderStatsCard()}

      {/* Filters */}
      {renderFilters()}

      {/* Task List */}
      <FlatList
        data={filteredTasks}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <TaskCard
            task={item}
            onPress={() => {
              // TODO: Navigate to task detail or reassignment screen
              console.log('Task selected:', item.id);
            }}
          />
        )}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Text style={styles.emptyText}>
              {selectedFilter === 'all'
                ? 'Aucune tâche pour l\'équipe'
                : `Aucune tâche ${selectedFilter === 'pending' ? 'en attente' : selectedFilter === 'in_progress' ? 'en cours' : 'en retard'}`}
            </Text>
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
    backgroundColor: '#f8fafc'
  },
  header: {
    backgroundColor: '#f59e0b',
    padding: 20,
    paddingTop: 60
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#fff',
    marginBottom: 4
  },
  subtitle: {
    fontSize: 14,
    color: '#fef3c7'
  },
  statsContainer: {
    flexDirection: 'row',
    padding: 16,
    gap: 8
  },
  statCard: {
    flex: 1,
    backgroundColor: '#fff',
    padding: 12,
    borderRadius: 12,
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3
  },
  statPending: {
    borderLeftWidth: 3,
    borderLeftColor: '#64748b'
  },
  statInProgress: {
    borderLeftWidth: 3,
    borderLeftColor: '#2563eb'
  },
  statOverdue: {
    borderLeftWidth: 3,
    borderLeftColor: '#dc2626'
  },
  statNumber: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#1e293b',
    marginBottom: 4
  },
  statLabel: {
    fontSize: 11,
    color: '#64748b',
    textAlign: 'center'
  },
  filterContainer: {
    flexDirection: 'row',
    paddingHorizontal: 16,
    gap: 8,
    marginBottom: 16
  },
  filterButton: {
    flex: 1,
    paddingVertical: 8,
    paddingHorizontal: 12,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#e2e8f0',
    backgroundColor: '#fff',
    alignItems: 'center'
  },
  filterButtonActive: {
    backgroundColor: '#f59e0b',
    borderColor: '#f59e0b'
  },
  filterText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#64748b'
  },
  filterTextActive: {
    color: '#fff'
  },
  listContent: {
    padding: 16
  },
  emptyContainer: {
    padding: 32,
    alignItems: 'center'
  },
  emptyText: {
    fontSize: 16,
    color: '#94a3b8',
    textAlign: 'center'
  }
});
