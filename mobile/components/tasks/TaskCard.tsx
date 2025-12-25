/**
 * Component: TaskCard
 *
 * Displays a task in a card format with:
 * - Task name
 * - Control point location
 * - Scheduled time
 * - Status badge
 */

import React from 'react';
import { View, Text, TouchableOpacity, StyleSheet } from 'react-native';
import { ScheduledTask } from '@/services/api/apiService';
import TaskStatusBadge from './TaskStatusBadge';

interface TaskCardProps {
  task: ScheduledTask;
  onPress: () => void;
}

export default function TaskCard({ task, onPress }: TaskCardProps) {
  // Format scheduled time
  const formatTime = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleTimeString('fr-FR', {
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  // Format date
  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const today = new Date();
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);

    if (date.toDateString() === today.toDateString()) {
      return "Aujourd'hui";
    } else if (date.toDateString() === tomorrow.toDateString()) {
      return 'Demain';
    } else {
      return date.toLocaleDateString('fr-FR', {
        day: '2-digit',
        month: 'short'
      });
    }
  };

  return (
    <TouchableOpacity
      style={styles.container}
      onPress={onPress}
      activeOpacity={0.7}
    >
      {/* Header: Title + Status Badge */}
      <View style={styles.header}>
        <Text style={styles.title} numberOfLines={2}>
          {task.taskTemplate.name}
        </Text>
        <TaskStatusBadge status={task.status} />
      </View>

      {/* Control Point Location */}
      <View style={styles.row}>
        <Text style={styles.icon}>📍</Text>
        <Text style={styles.location} numberOfLines={1}>
          {task.controlPoint.location}
        </Text>
      </View>

      {/* Scheduled Time */}
      <View style={styles.row}>
        <Text style={styles.icon}>🕐</Text>
        <Text style={styles.time}>
          {formatDate(task.scheduledFor)} à {formatTime(task.scheduledFor)}
        </Text>
      </View>

      {/* Description (if exists) */}
      {task.taskTemplate.description && (
        <Text style={styles.description} numberOfLines={2}>
          {task.taskTemplate.description}
        </Text>
      )}
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: '#fff',
    padding: 16,
    borderRadius: 12,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 12,
    gap: 8
  },
  title: {
    fontSize: 16,
    fontWeight: '700',
    color: '#1e293b',
    flex: 1
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 6
  },
  icon: {
    fontSize: 14,
    marginRight: 6
  },
  location: {
    fontSize: 14,
    color: '#64748b',
    flex: 1
  },
  time: {
    fontSize: 14,
    color: '#64748b'
  },
  description: {
    fontSize: 13,
    color: '#94a3b8',
    marginTop: 8,
    fontStyle: 'italic'
  }
});
