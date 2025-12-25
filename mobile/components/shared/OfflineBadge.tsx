/**
 * Offline Badge Component
 *
 * Displays network status and pending sync count
 * Shows when offline or when there are pending actions to sync
 */

import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity } from 'react-native';
import { useOfflineQueue, selectPendingActionsCount } from '@/store/offlineQueue';

export default function OfflineBadge() {
  const { isOnline, isSyncing, processQueue } = useOfflineQueue();
  const pendingCount = useOfflineQueue(selectPendingActionsCount);

  // Don't show if online and no pending actions
  if (isOnline && pendingCount === 0 && !isSyncing) {
    return null;
  }

  return (
    <TouchableOpacity
      style={[
        styles.container,
        !isOnline && styles.offline,
        isSyncing && styles.syncing
      ]}
      onPress={() => {
        if (isOnline && !isSyncing) {
          // Manual sync trigger (optional)
          console.log('[OfflineBadge] Manual sync triggered');
        }
      }}
      activeOpacity={0.7}
    >
      <View style={styles.content}>
        {!isOnline ? (
          <>
            <Text style={styles.icon}>📡</Text>
            <Text style={styles.text}>Hors ligne</Text>
          </>
        ) : isSyncing ? (
          <>
            <Text style={styles.icon}>🔄</Text>
            <Text style={styles.text}>Synchronisation...</Text>
          </>
        ) : pendingCount > 0 ? (
          <>
            <Text style={styles.icon}>⏳</Text>
            <Text style={styles.text}>{pendingCount} en attente</Text>
          </>
        ) : null}
      </View>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: '#10b981',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16,
    margin: 8,
    alignSelf: 'center',
  },
  offline: {
    backgroundColor: '#ef4444',
  },
  syncing: {
    backgroundColor: '#f59e0b',
  },
  content: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  icon: {
    fontSize: 14,
  },
  text: {
    color: '#fff',
    fontSize: 12,
    fontWeight: '600',
  },
});
