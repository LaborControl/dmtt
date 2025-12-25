/**
 * Component: TaskStatusBadge
 *
 * Displays task status as a colored badge
 * Supports: PENDING, IN_PROGRESS, COMPLETED, OVERDUE
 */

import React from 'react';
import { View, Text, StyleSheet } from 'react-native';

interface TaskStatusBadgeProps {
  status: string;
}

const STATUS_CONFIG = {
  PENDING: {
    label: 'En attente',
    color: '#64748b',
    backgroundColor: '#f1f5f9'
  },
  IN_PROGRESS: {
    label: 'En cours',
    color: '#2563eb',
    backgroundColor: '#dbeafe'
  },
  COMPLETED: {
    label: 'Terminée',
    color: '#16a34a',
    backgroundColor: '#dcfce7'
  },
  OVERDUE: {
    label: 'En retard',
    color: '#dc2626',
    backgroundColor: '#fee2e2'
  }
};

export default function TaskStatusBadge({ status }: TaskStatusBadgeProps) {
  const config = STATUS_CONFIG[status as keyof typeof STATUS_CONFIG] || STATUS_CONFIG.PENDING;

  return (
    <View
      style={[
        styles.badge,
        { backgroundColor: config.backgroundColor }
      ]}
    >
      <Text
        style={[
          styles.text,
          { color: config.color }
        ]}
      >
        {config.label}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  badge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12
  },
  text: {
    fontSize: 12,
    fontWeight: '600'
  }
});
