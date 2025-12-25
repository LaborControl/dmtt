/**
 * User History Screen
 *
 * Display task execution history
 * Features:
 * - Completed tasks
 * - Filter by date
 * - Stats
 */

import React from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  TouchableOpacity
} from 'react-native';
import { useTaskStore } from '@/store';
import TaskCard from '@/components/tasks/TaskCard';

export default function HistoryScreen() {
  const { tasks } = useTaskStore();

  // Filter completed tasks
  const completedTasks = tasks.filter(task => task.status === 'COMPLETED');

  // Calculate stats
  const stats = {
    today: completedTasks.filter(task => {
      const today = new Date();
      const taskDate = new Date(task.scheduledFor);
      return taskDate.toDateString() === today.toDateString();
    }).length,
    week: completedTasks.filter(task => {
      const weekAgo = new Date();
      weekAgo.setDate(weekAgo.getDate() - 7);
      const taskDate = new Date(task.scheduledFor);
      return taskDate >= weekAgo;
    }).length,
    total: completedTasks.length
  };

  return (
    <View style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.title}>Historique</Text>
        <Text style={styles.subtitle}>Tâches complétées</Text>
      </View>

      {/* Stats */}
      <View style={styles.statsContainer}>
        <View style={styles.statCard}>
          <Text style={styles.statNumber}>{stats.today}</Text>
          <Text style={styles.statLabel}>Aujourd'hui</Text>
        </View>
        <View style={styles.statCard}>
          <Text style={styles.statNumber}>{stats.week}</Text>
          <Text style={styles.statLabel}>Cette semaine</Text>
        </View>
        <View style={styles.statCard}>
          <Text style={styles.statNumber}>{stats.total}</Text>
          <Text style={styles.statLabel}>Total</Text>
        </View>
      </View>

      {/* List */}
      <FlatList
        data={completedTasks}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <TaskCard task={item} onPress={() => {}} />
        )}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Text style={styles.emptyText}>
              Aucune tâche complétée
            </Text>
          </View>
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
  statsContainer: {
    flexDirection: 'row',
    padding: 16,
    gap: 12
  },
  statCard: {
    flex: 1,
    backgroundColor: '#fff',
    padding: 16,
    borderRadius: 12,
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3
  },
  statNumber: {
    fontSize: 32,
    fontWeight: 'bold',
    color: '#2563eb',
    marginBottom: 4
  },
  statLabel: {
    fontSize: 12,
    color: '#64748b',
    textAlign: 'center'
  },
  listContent: {
    padding: 16
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
