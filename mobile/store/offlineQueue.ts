/**
 * Offline Queue Store
 *
 * Queues actions when offline and syncs automatically when network returns
 * Features:
 * - Auto-retry with exponential backoff
 * - Network detection (@react-native-community/netinfo)
 * - MMKV persistence
 * - Conflict resolution (server wins)
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import NetInfo from '@react-native-community/netinfo';
import { mmkvStorage } from '@/utils/storage';
import {
  createAnomaly,
  CreateAnomalyPayload,
  startTaskExecution,
  completeTaskExecution
} from '@/services/api/apiService';

// ============================================================================
// TYPES
// ============================================================================

export type QueueActionType = 'ANOMALY' | 'TASK_START' | 'TASK_COMPLETE';

export interface QueuedAction {
  id: string; // Unique ID (timestamp + random)
  type: QueueActionType;
  payload: any;
  timestamp: number;
  retries: number;
  status: 'PENDING' | 'SYNCING' | 'FAILED' | 'SUCCESS';
  error?: string;
}

interface OfflineQueueState {
  // State
  queue: QueuedAction[];
  isOnline: boolean;
  isSyncing: boolean;
  lastSyncAttempt: number | null;

  // Actions
  enqueue: (type: QueueActionType, payload: any) => string;
  dequeue: (id: string) => void;
  processQueue: (token: string) => Promise<void>;
  clearQueue: () => void;
  setOnlineStatus: (isOnline: boolean) => void;

  // Getters
  getPendingCount: () => number;
  getFailedCount: () => number;
}

// ============================================================================
// CONSTANTS
// ============================================================================

const MAX_QUEUE_SIZE = 100;
const MAX_RETRIES = 3;
const INITIAL_RETRY_DELAY = 1000; // 1 second

// ============================================================================
// HELPERS
// ============================================================================

/**
 * Generate unique ID for queued action
 */
const generateId = (): string => {
  return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
};

/**
 * Calculate exponential backoff delay
 */
const getRetryDelay = (retries: number): number => {
  return INITIAL_RETRY_DELAY * Math.pow(2, retries);
};

/**
 * Execute queued action
 */
const executeAction = async (action: QueuedAction, token: string): Promise<void> => {
  switch (action.type) {
    case 'ANOMALY':
      await createAnomaly(action.payload as CreateAnomalyPayload, token);
      break;

    case 'TASK_START':
      await startTaskExecution(
        action.payload.taskId,
        action.payload.nfcUid,
        token
      );
      break;

    case 'TASK_COMPLETE':
      await completeTaskExecution(
        action.payload.executionId,
        action.payload.nfcUid,
        action.payload.formData,
        token
      );
      break;

    default:
      throw new Error(`Unknown action type: ${action.type}`);
  }
};

// ============================================================================
// STORE
// ============================================================================

export const useOfflineQueue = create<OfflineQueueState>()(
  persist(
    (set, get) => ({
      // ==========================================================================
      // INITIAL STATE
      // ==========================================================================
      queue: [],
      isOnline: true,
      isSyncing: false,
      lastSyncAttempt: null,

      // ==========================================================================
      // ACTION: Enqueue
      // ==========================================================================
      enqueue: (type: QueueActionType, payload: any) => {
        const { queue } = get();

        // Check max queue size
        if (queue.length >= MAX_QUEUE_SIZE) {
          console.warn('[OfflineQueue] ⚠️ Queue full, removing oldest item');
          set({ queue: queue.slice(1) });
        }

        const action: QueuedAction = {
          id: generateId(),
          type,
          payload,
          timestamp: Date.now(),
          retries: 0,
          status: 'PENDING'
        };

        console.log(`[OfflineQueue] ➕ Enqueued ${type} action:`, action.id);

        set(state => ({
          queue: [...state.queue, action]
        }));

        return action.id;
      },

      // ==========================================================================
      // ACTION: Dequeue
      // ==========================================================================
      dequeue: (id: string) => {
        console.log('[OfflineQueue] ➖ Dequeuing action:', id);

        set(state => ({
          queue: state.queue.filter(action => action.id !== id)
        }));
      },

      // ==========================================================================
      // ACTION: Process Queue
      // ==========================================================================
      processQueue: async (token: string) => {
        const { queue, isOnline, isSyncing } = get();

        if (!isOnline) {
          console.log('[OfflineQueue] ⚠️ Offline, skipping sync');
          return;
        }

        if (isSyncing) {
          console.log('[OfflineQueue] ⏳ Already syncing, skipping');
          return;
        }

        const pendingActions = queue.filter(a => a.status === 'PENDING' || a.status === 'FAILED');

        if (pendingActions.length === 0) {
          console.log('[OfflineQueue] ✅ Queue empty, nothing to sync');
          return;
        }

        console.log(`[OfflineQueue] 🔄 Processing ${pendingActions.length} queued actions...`);
        set({ isSyncing: true, lastSyncAttempt: Date.now() });

        for (const action of pendingActions) {
          try {
            // Mark as syncing
            set(state => ({
              queue: state.queue.map(a =>
                a.id === action.id ? { ...a, status: 'SYNCING' as const } : a
              )
            }));

            // Execute action
            console.log(`[OfflineQueue] ⚡ Executing ${action.type}:`, action.id);
            await executeAction(action, token);

            // Mark as success and remove from queue
            console.log(`[OfflineQueue] ✅ Success ${action.type}:`, action.id);
            get().dequeue(action.id);

          } catch (error: any) {
            console.error(`[OfflineQueue] ❌ Error executing ${action.type}:`, error);

            const newRetries = action.retries + 1;

            if (newRetries >= MAX_RETRIES) {
              // Max retries reached, mark as failed
              console.error(`[OfflineQueue] 🚫 Max retries reached for ${action.id}`);

              set(state => ({
                queue: state.queue.map(a =>
                  a.id === action.id
                    ? { ...a, status: 'FAILED' as const, error: error.message, retries: newRetries }
                    : a
                )
              }));
            } else {
              // Retry later with exponential backoff
              const delay = getRetryDelay(newRetries);
              console.log(`[OfflineQueue] ⏰ Retry ${newRetries}/${MAX_RETRIES} in ${delay}ms`);

              set(state => ({
                queue: state.queue.map(a =>
                  a.id === action.id
                    ? { ...a, status: 'PENDING' as const, retries: newRetries }
                    : a
                )
              }));

              // Wait before next retry
              await new Promise(resolve => setTimeout(resolve, delay));
            }
          }
        }

        set({ isSyncing: false });
        console.log('[OfflineQueue] 🏁 Sync complete');
      },

      // ==========================================================================
      // ACTION: Clear Queue
      // ==========================================================================
      clearQueue: () => {
        console.log('[OfflineQueue] 🗑️ Clearing queue');
        set({ queue: [] });
      },

      // ==========================================================================
      // ACTION: Set Online Status
      // ==========================================================================
      setOnlineStatus: (isOnline: boolean) => {
        console.log('[OfflineQueue] 📡 Network status:', isOnline ? 'ONLINE' : 'OFFLINE');
        set({ isOnline });
      },

      // ==========================================================================
      // GETTER: Pending Count
      // ==========================================================================
      getPendingCount: () => {
        return get().queue.filter(a => a.status === 'PENDING').length;
      },

      // ==========================================================================
      // GETTER: Failed Count
      // ==========================================================================
      getFailedCount: () => {
        return get().queue.filter(a => a.status === 'FAILED').length;
      }
    }),
    {
      name: 'offline-queue',
      storage: createJSONStorage(() => mmkvStorage),
      // Persist queue, but not sync state
      partialize: (state) => ({
        queue: state.queue
      })
    }
  )
);

// ============================================================================
// NETWORK LISTENER
// ============================================================================

/**
 * Initialize network listener
 * Call this once in App.tsx or root _layout.tsx
 */
export const initNetworkListener = (token: string) => {
  console.log('[OfflineQueue] 📡 Initializing network listener...');

  const unsubscribe = NetInfo.addEventListener(state => {
    const isOnline = state.isConnected && state.isInternetReachable !== false;
    useOfflineQueue.getState().setOnlineStatus(isOnline);

    // Auto-sync when coming back online
    if (isOnline) {
      console.log('[OfflineQueue] 🌐 Back online, triggering auto-sync...');
      setTimeout(() => {
        useOfflineQueue.getState().processQueue(token);
      }, 1000); // Wait 1 second before syncing
    }
  });

  return unsubscribe;
};

// ============================================================================
// SELECTORS
// ============================================================================

/**
 * Get pending actions count
 */
export const selectPendingActionsCount = (state: OfflineQueueState) =>
  state.queue.filter(a => a.status === 'PENDING').length;

/**
 * Get failed actions count
 */
export const selectFailedActionsCount = (state: OfflineQueueState) =>
  state.queue.filter(a => a.status === 'FAILED').length;

/**
 * Get all pending actions
 */
export const selectPendingActions = (state: OfflineQueueState) =>
  state.queue.filter(a => a.status === 'PENDING');

/**
 * Get all failed actions
 */
export const selectFailedActions = (state: OfflineQueueState) =>
  state.queue.filter(a => a.status === 'FAILED');
