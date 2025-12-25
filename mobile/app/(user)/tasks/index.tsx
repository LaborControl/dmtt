/**
 * User Tasks Screen
 *
 * Main screen for USER role
 * Features:
 * - Task list with Zustand store
 * - Pull to refresh
 * - Filter by status
 * - Navigate to task execution
 */

import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  TouchableOpacity,
  RefreshControl
} from 'react-native';
import { useRouter } from 'expo-router';
import { useAuth } from '@/contexts/AuthContext';
import { useTaskStore, selectTodayTasks } from '@/store';
import TaskCard from '@/components/tasks/TaskCard';
import LoadingSpinner from '@/components/shared/LoadingSpinner';

export default function TasksScreen() {
  const router = useRouter();
  const { user, token } = useAuth();
  const { tasks, loading, fetchTasks, selectTask } = useTaskStore();

  const [refreshing, setRefreshing] = useState(false);
  const [filter, setFilter] = useState<'all' | 'today' | 'pending'>('today');

  // ==========================================================================
  // Fetch tasks on mount
  // ==========================================================================
  useEffect(() => {
    if (user && token) {
      fetchTasks(user.id, token);
    }
  }, [user, token]);

  // ==========================================================================
  // Refresh handler
  // ==========================================================================
  const handleRefresh = async () => {
    setRefreshing(true);
    if (user && token) {
      await fetchTasks(user.id, token, true); // Force refresh
    }
    setRefreshing(false);
  };

  // ==========================================================================
  // Filter tasks
  // ==========================================================================
  const filteredTasks = React.useMemo(() => {
    if (filter === 'all') return tasks;

    if (filter === 'today') {
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      return tasks.filter(task => {
        const taskDate = new Date(task.scheduledFor);
        taskDate.setHours(0, 0, 0, 0);
        return taskDate.getTime() === today.getTime();
      });
    }

    if (filter === 'pending') {
      return tasks.filter(task => task.status === 'PENDING');
    }

    return tasks;
  }, [tasks, filter]);

  // ==========================================================================
  // Handle task press
  // ==========================================================================
  const handleTaskPress = (taskId: string) => {
    selectTask(taskId);
    // TODO: Navigate to task execution screen
    console.log('Task selected:', taskId);
  };

  // ==========================================================================
  // Render filters
  // ==========================================================================
  const renderFilters = () => (
    <View style={styles.filterContainer}>
      <TouchableOpacity
        style={[styles.filterButton, filter === 'today' && styles.filterButtonActive]}
        onPress={() => setFilter('today')}
      >
        <Text style={[styles.filterText, filter === 'today' && styles.filterTextActive]}>
          Aujourd'hui
        </Text>
      </TouchableOpacity>
      <TouchableOpacity
        style={[styles.filterButton, filter === 'pending' && styles.filterButtonActive]}
        onPress={() => setFilter('pending')}
      >
        <Text style={[styles.filterText, filter === 'pending' && styles.filterTextActive]}>
          En attente
        </Text>
      </TouchableOpacity>
      <TouchableOpacity
        style={[styles.filterButton, filter === 'all' && styles.filterButtonActive]}
        onPress={() => setFilter('all')}
      >
        <Text style={[styles.filterText, filter === 'all' && styles.filterTextActive]}>
          Toutes
        </Text>
      </TouchableOpacity>
    </View>
  );

  // ==========================================================================
  // Loading state
  // ==========================================================================
  if (loading && tasks.length === 0) {
    return <LoadingSpinner message="Chargement des tâches..." />;
  }

  // ==========================================================================
  // Main render
  // ==========================================================================
  return (
    <View style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.title}>Mes Tâches</Text>
        <Text style={styles.subtitle}>
          {user ? `${user.firstName} ${user.lastName}` : ''}
        </Text>
      </View>

      {/* Filters */}
      {renderFilters()}

      {/* Task count */}
      <View style={styles.countContainer}>
        <Text style={styles.countText}>
          {filteredTasks.length} tâche{filteredTasks.length !== 1 ? 's' : ''}
        </Text>
      </View>

      {/* Task list */}
      <FlatList
        data={filteredTasks}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <TaskCard
            task={item}
            onPress={() => handleTaskPress(item.id)}
          />
        )}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Text style={styles.emptyText}>
              {filter === 'today'
                ? '🎉 Aucune tâche pour aujourd\'hui'
                : filter === 'pending'
                ? '✅ Toutes les tâches sont complétées'
                : 'Aucune tâche assignée'}
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
    backgroundColor: '#2563eb',
    padding: 20,
    paddingTop: 60
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#fff',
    marginBottom: 4
  },
  subtitle: {
    fontSize: 14,
    color: '#dbeafe'
  },
  filterContainer: {
    flexDirection: 'row',
    padding: 16,
    gap: 8
  },
  filterButton: {
    flex: 1,
    paddingVertical: 10,
    paddingHorizontal: 16,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#e2e8f0',
    backgroundColor: '#fff',
    alignItems: 'center'
  },
  filterButtonActive: {
    backgroundColor: '#2563eb',
    borderColor: '#2563eb'
  },
  filterText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#64748b'
  },
  filterTextActive: {
    color: '#fff'
  },
  countContainer: {
    paddingHorizontal: 16,
    paddingBottom: 8
  },
  countText: {
    fontSize: 13,
    color: '#64748b',
    fontWeight: '500'
  },
  listContent: {
    padding: 16,
    paddingTop: 0
  },
  emptyContainer: {
    padding: 40,
    alignItems: 'center'
  },
  emptyText: {
    fontSize: 16,
    color: '#94a3b8',
    textAlign: 'center'
  }
});
