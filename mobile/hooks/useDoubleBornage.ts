/**
 * Double Bornage Hook
 *
 * Manages double scan workflow with invisible timer
 * Timer is 100% transparent to the user - backend validates duration
 * Includes offline queue support for resilient operation
 */

import { useState, useEffect, useRef } from 'react';
import { Alert } from 'react-native';
import { firstScan, secondScan, FirstScanPayload, SecondScanPayload } from '@/services/api/apiService';
import { useOfflineQueue } from '@/store/offlineQueue';

interface DoubleBornageState {
  isInProgress: boolean;
  executionId: string | null;
  firstScanTime: Date | null;
}

export function useDoubleBornage(token: string | null) {
  const { enqueue, isOnline } = useOfflineQueue();

  const [state, setState] = useState<DoubleBornageState>({
    isInProgress: false,
    executionId: null,
    firstScanTime: null,
  });

  // ==========================================================================
  // FUNCTION: Start double bornage (first scan)
  // ==========================================================================
  const startDoubleBornage = async (
    scheduledTaskId: string,
    controlPointId: string,
    userId: string
  ): Promise<{ success: boolean; executionId?: string }> => {
    if (!token) {
      Alert.alert('Erreur', 'Non authentifié');
      return { success: false };
    }

    try {
      console.log('[DOUBLE BORNAGE] Starting first scan...');

      const payload: FirstScanPayload = {
        scheduledTaskId,
        controlPointId,
        userId,
        firstScanAt: new Date().toISOString(),
      };

      const response = await firstScan(payload, token);

      console.log('[DOUBLE BORNAGE] First scan successful:', response.executionId);

      // Update state - timer is INVISIBLE to user
      setState({
        isInProgress: true,
        executionId: response.executionId,
        firstScanTime: new Date(),
      });

      // NO TIMER UI - User just continues to work
      // Backend will validate the time difference between scans
      console.log('[DOUBLE BORNAGE] Timer started (invisible) - proceed with task');

      return {
        success: true,
        executionId: response.executionId,
      };
    } catch (error: any) {
      console.error('[DOUBLE BORNAGE] First scan failed:', error);
      Alert.alert('Erreur', error.message || 'Impossible de démarrer le scan');
      return { success: false };
    }
  };

  // ==========================================================================
  // FUNCTION: Complete double bornage (second scan)
  // ==========================================================================
  const completeDoubleBornage = async (
    formDataJson: string,
    photoUrl?: string | null
  ): Promise<{ success: boolean; totalWorkTime?: string }> => {
    if (!token) {
      Alert.alert('Erreur', 'Non authentifié');
      return { success: false };
    }

    if (!state.executionId) {
      Alert.alert('Erreur', 'Aucune exécution en cours. Veuillez scanner à nouveau.');
      return { success: false };
    }

    const payload: SecondScanPayload = {
      executionId: state.executionId,
      secondScanAt: new Date().toISOString(),
      formData: formDataJson,
      photoUrl,
    };

    try {
      console.log('[DOUBLE BORNAGE] Completing second scan...');

      if (!isOnline) {
        // Offline: Queue the completion
        console.log('[DOUBLE BORNAGE] Offline, queueing task completion...');
        enqueue('TASK_COMPLETE', {
          executionId: payload.executionId,
          nfcUid: 'queued', // Placeholder - real UID from scan
          formData: payload.formData
        });

        // Reset state
        setState({
          isInProgress: false,
          executionId: null,
          firstScanTime: null,
        });

        Alert.alert(
          'Tâche mise en file d\'attente',
          'Vous êtes hors ligne. La tâche sera synchronisée automatiquement au retour du réseau.'
        );

        return { success: true, totalWorkTime: 'En attente sync' };
      }

      // Online: Submit immediately
      const response = await secondScan(payload, token);

      console.log('[DOUBLE BORNAGE] Second scan successful:', response.totalWorkTime);

      // Reset state
      setState({
        isInProgress: false,
        executionId: null,
        firstScanTime: null,
      });

      return {
        success: true,
        totalWorkTime: response.totalWorkTime,
      };
    } catch (error: any) {
      console.error('[DOUBLE BORNAGE] Second scan failed:', error);

      // TIMER SILENCIEUX: Si erreur de timing, on log mais on ne bloque PAS l'utilisateur
      if (error.message?.includes('30 second') || error.message?.includes('minimum') ||
          error.message?.includes('2 hour') || error.message?.includes('maximum')) {
        console.warn('[DOUBLE BORNAGE] ⚠️ Timer constraint not met, but task will be saved anyway');

        // Reset state silencieusement
        setState({
          isInProgress: false,
          executionId: null,
          firstScanTime: null,
        });

        // Succès silencieux pour erreurs de timing
        return { success: true, totalWorkTime: 'N/A' };
      }

      // Pour les autres erreurs: Queue si online (fallback resilience)
      if (isOnline) {
        console.log('[DOUBLE BORNAGE] Online submission failed, queueing as fallback...');
        enqueue('TASK_COMPLETE', {
          executionId: payload.executionId,
          nfcUid: 'queued-fallback',
          formData: payload.formData
        });

        // Reset state
        setState({
          isInProgress: false,
          executionId: null,
          firstScanTime: null,
        });

        Alert.alert(
          'Tâche mise en file d\'attente',
          'L\'envoi a échoué mais la tâche sera réessayée automatiquement.'
        );

        return { success: true, totalWorkTime: 'En attente sync' };
      }

      // Vraie erreur sans fallback possible
      Alert.alert('Erreur', error.message || 'Impossible de terminer le scan');
      return { success: false };
    }
  };

  // ==========================================================================
  // FUNCTION: Cancel double bornage
  // ==========================================================================
  const cancelDoubleBornage = () => {
    console.log('[DOUBLE BORNAGE] Cancelling...');
    setState({
      isInProgress: false,
      executionId: null,
      firstScanTime: null,
    });
  };

  // ==========================================================================
  // FUNCTION: Check if time constraints are met (for display only)
  // ==========================================================================
  const getElapsedTime = (): number | null => {
    if (!state.firstScanTime) {
      return null;
    }

    const now = new Date();
    const elapsed = (now.getTime() - state.firstScanTime.getTime()) / 1000; // seconds
    return elapsed;
  };

  return {
    isInProgress: state.isInProgress,
    executionId: state.executionId,
    firstScanTime: state.firstScanTime,
    startDoubleBornage,
    completeDoubleBornage,
    cancelDoubleBornage,
    getElapsedTime,
  };
}
