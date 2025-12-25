/**
 * Anomaly Store (Zustand)
 *
 * Global state management for anomalies
 * Features:
 * - Track anomaly submissions
 * - Queue for offline mode (future)
 * - History of submitted anomalies
 * - MMKV persistence (10x faster than AsyncStorage)
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import { mmkvStorage } from '@/utils/storage';
import { createAnomaly, CreateAnomalyPayload } from '@/services/api/apiService';

// ============================================================================
// TYPES
// ============================================================================

interface SubmittedAnomaly {
  id: string;
  controlPointId: string;
  severity: 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL';
  description: string;
  detectedAt: string;
  photoUrl?: string | null;
}

interface AnomalyState {
  // State
  submitting: boolean;
  error: string | null;
  lastSubmitted: SubmittedAnomaly | null;
  history: SubmittedAnomaly[]; // Last 50 anomalies

  // Actions
  submit: (payload: CreateAnomalyPayload, token: string) => Promise<boolean>;
  clearError: () => void;
  clearHistory: () => void;
}

// ============================================================================
// STORE
// ============================================================================

export const useAnomalyStore = create<AnomalyState>()(
  persist(
    (set, get) => ({
      // ==========================================================================
      // INITIAL STATE
      // ==========================================================================
      submitting: false,
      error: null,
      lastSubmitted: null,
      history: [],

      // ==========================================================================
      // ACTION: Submit anomaly
      // ==========================================================================
      submit: async (payload: CreateAnomalyPayload, token: string) => {
        set({ submitting: true, error: null });

        try {
          console.log('[AnomalyStore] Submitting anomaly...');
          console.log('[AnomalyStore] Severity:', payload.severity);
          console.log('[AnomalyStore] ControlPoint:', payload.controlPointId);

          await createAnomaly(payload, token);

          console.log('[AnomalyStore] ✅ Anomaly submitted successfully');

          // Create anomaly record for history
          const submitted: SubmittedAnomaly = {
            id: `${Date.now()}`, // Temporary ID
            controlPointId: payload.controlPointId,
            severity: payload.severity,
            description: payload.description,
            detectedAt: payload.detectedAt,
            photoUrl: payload.photoUrl
          };

          // Add to history (keep last 50)
          set(state => ({
            submitting: false,
            lastSubmitted: submitted,
            history: [submitted, ...state.history].slice(0, 50)
          }));

          return true;
        } catch (error: any) {
          console.error('[AnomalyStore] ❌ Error submitting anomaly:', error);

          set({
            submitting: false,
            error: error.message || 'Erreur lors de la soumission de l\'anomalie'
          });

          return false;
        }
      },

      // ==========================================================================
      // ACTION: Clear error
      // ==========================================================================
      clearError: () => {
        set({ error: null });
      },

      // ==========================================================================
      // ACTION: Clear history
      // ==========================================================================
      clearHistory: () => {
        console.log('[AnomalyStore] Clearing history');
        set({
          history: [],
          lastSubmitted: null
        });
      }
    }),
    {
      name: 'anomaly-storage',
      storage: createJSONStorage(() => mmkvStorage),
      // Only persist history, not submitting/error state
      partialize: (state) => ({
        history: state.history,
        lastSubmitted: state.lastSubmitted
      })
    }
  )
);

// ============================================================================
// SELECTORS
// ============================================================================

/**
 * Get anomalies by severity
 */
export const selectAnomaliesBySeverity = (severity: string) => (state: AnomalyState) =>
  state.history.filter(anomaly => anomaly.severity === severity);

/**
 * Get anomalies count
 */
export const selectAnomaliesCount = (state: AnomalyState) => state.history.length;

/**
 * Get recent anomalies (last 10)
 */
export const selectRecentAnomalies = (state: AnomalyState) =>
  state.history.slice(0, 10);
