/**
 * Hook: useTaskList
 *
 * Manages task list fetching and state
 * Extracted from index.tsx to separate concerns
 */

import { useState, useEffect, useCallback } from 'react';
import { getScheduledTasks, ScheduledTask } from '@/services/api/apiService';
import { useAuth } from '@/contexts/auth-context';

interface UseTaskListReturn {
  tasks: ScheduledTask[];
  loading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
  filterByStatus: (status: string) => ScheduledTask[];
}

export function useTaskList(): UseTaskListReturn {
  const { user, token } = useAuth();
  const [tasks, setTasks] = useState<ScheduledTask[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchTasks = useCallback(async () => {
    if (!user || !token) {
      console.log('[useTaskList] No user or token, skipping fetch');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      console.log('[useTaskList] Fetching tasks for user:', user.id);
      const data = await getScheduledTasks(user.id, token);

      console.log(`[useTaskList] ✅ Fetched ${data.length} tasks`);
      setTasks(data);
    } catch (err: any) {
      console.error('[useTaskList] ❌ Error fetching tasks:', err);
      setError(err.message || 'Erreur lors du chargement des tâches');
    } finally {
      setLoading(false);
    }
  }, [user, token]);

  // Auto-fetch on mount and when user/token changes
  useEffect(() => {
    fetchTasks();
  }, [fetchTasks]);

  // Filter tasks by status
  const filterByStatus = useCallback((status: string): ScheduledTask[] => {
    return tasks.filter(task => task.status === status);
  }, [tasks]);

  return {
    tasks,
    loading,
    error,
    refetch: fetchTasks,
    filterByStatus
  };
}
