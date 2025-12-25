/**
 * Hook: useTaskExecution
 *
 * Manages task execution with double bornage workflow
 * Extracted from index.tsx to separate execution logic
 */

import { useState } from 'react';
import { Alert } from 'react-native';
import { useAuth } from '@/contexts/auth-context';
import { useDoubleBornage } from '@/hooks/useDoubleBornage';

interface FormData {
  temperature: string;
  pressure: string;
  notes: string;
  photos: string[];
}

interface UseTaskExecutionReturn {
  // State
  formData: FormData;
  submitting: boolean;
  isInProgress: boolean;
  executionId: string | null;

  // Actions
  setFormData: (data: FormData) => void;
  updateFormField: (field: keyof FormData, value: any) => void;
  executeTask: (scheduledTaskId: string, controlPointId: string) => Promise<{ success: boolean; executionId?: string }>;
  submitForm: () => Promise<{ success: boolean; totalWorkTime?: string }>;
  cancelExecution: () => void;
  resetForm: () => void;
}

const initialFormData: FormData = {
  temperature: '',
  pressure: '',
  notes: '',
  photos: []
};

export function useTaskExecution(): UseTaskExecutionReturn {
  const { user, token } = useAuth();
  const [formData, setFormData] = useState<FormData>(initialFormData);
  const [submitting, setSubmitting] = useState(false);

  const doubleBornage = useDoubleBornage(token);

  // ==========================================================================
  // Update single form field
  // ==========================================================================
  const updateFormField = (field: keyof FormData, value: any) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  // ==========================================================================
  // Execute task (first scan - double bornage)
  // ==========================================================================
  const executeTask = async (
    scheduledTaskId: string,
    controlPointId: string
  ): Promise<{ success: boolean; executionId?: string }> => {
    if (!token || !user) {
      Alert.alert('Erreur', 'Non authentifié');
      return { success: false };
    }

    console.log('[useTaskExecution] Starting task execution...');
    console.log('[useTaskExecution] ScheduledTaskId:', scheduledTaskId);
    console.log('[useTaskExecution] ControlPointId:', controlPointId);

    try {
      // First scan (double bornage)
      const result = await doubleBornage.startDoubleBornage(
        scheduledTaskId,
        controlPointId,
        user.id
      );

      if (result.success) {
        console.log('[useTaskExecution] ✅ Task execution started');
        console.log('[useTaskExecution] ExecutionId:', result.executionId);
      } else {
        console.log('[useTaskExecution] ❌ Failed to start execution');
      }

      return result;
    } catch (error: any) {
      console.error('[useTaskExecution] Error:', error);
      Alert.alert('Erreur', error.message || 'Impossible de démarrer la tâche');
      return { success: false };
    }
  };

  // ==========================================================================
  // Submit form (second scan - double bornage)
  // ==========================================================================
  const submitForm = async (): Promise<{ success: boolean; totalWorkTime?: string }> => {
    if (!doubleBornage.executionId) {
      Alert.alert('Erreur', 'Aucune exécution en cours');
      return { success: false };
    }

    // Validation basique
    if (!formData.temperature.trim()) {
      Alert.alert('Erreur', 'La température est obligatoire');
      return { success: false };
    }

    if (!formData.pressure.trim()) {
      Alert.alert('Erreur', 'La pression est obligatoire');
      return { success: false };
    }

    setSubmitting(true);

    try {
      console.log('[useTaskExecution] Submitting form...');
      console.log('[useTaskExecution] FormData:', formData);

      // Second scan avec formulaire
      const result = await doubleBornage.completeDoubleBornage(
        JSON.stringify(formData),
        formData.photos[0] || null
      );

      if (result.success) {
        console.log('[useTaskExecution] ✅ Form submitted successfully');
        console.log('[useTaskExecution] Total work time:', result.totalWorkTime);

        // Reset form on success
        resetForm();
      } else {
        console.log('[useTaskExecution] ❌ Form submission failed');
      }

      return result;
    } catch (error: any) {
      console.error('[useTaskExecution] Error:', error);
      Alert.alert('Erreur', error.message || 'Erreur lors de la soumission');
      return { success: false };
    } finally {
      setSubmitting(false);
    }
  };

  // ==========================================================================
  // Cancel execution
  // ==========================================================================
  const cancelExecution = () => {
    console.log('[useTaskExecution] Cancelling execution...');
    doubleBornage.cancelDoubleBornage();
    resetForm();
  };

  // ==========================================================================
  // Reset form to initial state
  // ==========================================================================
  const resetForm = () => {
    console.log('[useTaskExecution] Resetting form...');
    setFormData(initialFormData);
    setSubmitting(false);
  };

  return {
    // State
    formData,
    submitting,
    isInProgress: doubleBornage.isInProgress,
    executionId: doubleBornage.executionId,

    // Actions
    setFormData,
    updateFormField,
    executeTask,
    submitForm,
    cancelExecution,
    resetForm
  };
}
