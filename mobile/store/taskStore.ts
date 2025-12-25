/**
 * Task Store (Zustand)
 *
 * Global state management for tasks
 * Features:
 * - Cache with 30-second TTL
 * - Persistence with MMKV (10x faster than AsyncStorage)
 * - Optimistic updates
 * - Error handling
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import { mmkvStorage } from '@/utils/storage';
import { getScheduledTasks, ScheduledTask } from '@/services/api/apiService';

// ============================================================================
// TYPES
// ============================================================================

interface TaskState {
  // State
  tasks: ScheduledTask[];
  selectedTask: ScheduledTask | null;
  loading: boolean;
  error: string | null;
  lastFetch: number | null; // Timestamp

  // Actions
  fetchTasks: (userId: string, token: string, forceRefresh?: boolean) => Promise<void>;
  selectTask: (taskId: string) => void;
  updateTaskStatus: (taskId: string, status: string) => void;
  addTask: (task: ScheduledTask) => void;
  removeTask: (taskId: string) => void;
  clearTasks: () => void;
  clearError: () => void;
}

// ============================================================================
// CONSTANTS
// ============================================================================

const CACHE_TTL = 30 * 1000; // 30 seconds

// ============================================================================
// STORE
// ============================================================================

export const useTaskStore = create<TaskState>()(
  persist(
    (set, get) => ({
      // ==========================================================================
      // INITIAL STATE
      // ==========================================================================
      tasks: [],
      selectedTask: null,
      loading: false,
      error: null,
      lastFetch: null,

      // ==========================================================================
      // ACTION: Fetch tasks with cache
      // ==========================================================================
      fetchTasks: async (userId: string, token: string, forceRefresh = false) => {
        const { lastFetch, tasks } = get();

        // Check cache (30 seconds TTL)
        if (!forceRefresh && lastFetch && Date.now() - lastFetch < CACHE_TTL) {
          console.log('[TaskStore] Using cached tasks (', tasks.length, 'tasks)');
          return;
        }

        set({ loading: true, error: null });

        try {
          console.log('[TaskStore] Fetching tasks from API for userId:', userId);
          const fetchedTasks = await getScheduledTasks(userId, token);

          console.log(`[TaskStore] ✅ Fetched ${fetchedTasks.length} tasks`);

          set({
            tasks: fetchedTasks,
            loading: false,
            lastFetch: Date.now()
          });
        } catch (error: any) {
          console.error('[TaskStore] ❌ Error fetching tasks:', error);
          set({
            error: error.message || 'Erreur lors du chargement des tâches',
            loading: false
          });
        }
      },

      // ==========================================================================
      // ACTION: Select task
      // ==========================================================================
      selectTask: (taskId: string) => {
        const { tasks } = get();
        const task = tasks.find(t => t.id === taskId);

        console.log('[TaskStore] Selecting task:', taskId);
        set({ selectedTask: task || null });
      },

      // ==========================================================================
      // ACTION: Update task status (optimistic)
      // ==========================================================================
      updateTaskStatus: (taskId: string, status: string) => {
        console.log(`[TaskStore] Updating task ${taskId} status to ${status}`);

        set(state => ({
          tasks: state.tasks.map(task =>
            task.id === taskId
              ? { ...task, status }
              : task
          ),
          selectedTask: state.selectedTask?.id === taskId
            ? { ...state.selectedTask, status }
            : state.selectedTask
        }));
      },

      // ==========================================================================
      // ACTION: Add task (optimistic)
      // ==========================================================================
      addTask: (task: ScheduledTask) => {
        console.log('[TaskStore] Adding task:', task.id);

        set(state => ({
          tasks: [task, ...state.tasks]
        }));
      },

      // ==========================================================================
      // ACTION: Remove task (optimistic)
      // ==========================================================================
      removeTask: (taskId: string) => {
        console.log('[TaskStore] Removing task:', taskId);

        set(state => ({
          tasks: state.tasks.filter(t => t.id !== taskId),
          selectedTask: state.selectedTask?.id === taskId ? null : state.selectedTask
        }));
      },

      // ==========================================================================
      // ACTION: Clear all tasks
      // ==========================================================================
      clearTasks: () => {
        console.log('[TaskStore] Clearing all tasks');

        set({
          tasks: [],
          selectedTask: null,
          error: null,
          lastFetch: null
        });
      },

      // ==========================================================================
      // ACTION: Clear error
      // ==========================================================================
      clearError: () => {
        set({ error: null });
      }
    }),
    {
      name: 'task-storage', // MMKV key
      storage: createJSONStorage(() => mmkvStorage),
      // Only persist tasks, not loading/error state
      partialize: (state) => ({
        tasks: state.tasks,
        lastFetch: state.lastFetch
      })
    }
  )
);

// ============================================================================
// SELECTORS (for performance)
// ============================================================================

/**
 * Get tasks by status
 */
export const selectTasksByStatus = (status: string) => (state: TaskState) =>
  state.tasks.filter(task => task.status === status);

/**
 * Get pending tasks count
 */
export const selectPendingTasksCount = (state: TaskState) =>
  state.tasks.filter(task => task.status === 'PENDING').length;

/**
 * Get overdue tasks
 */
export const selectOverdueTasks = (state: TaskState) =>
  state.tasks.filter(task => task.status === 'OVERDUE');

/**
 * Get tasks for today
 */
export const selectTodayTasks = (state: TaskState) => {
  const today = new Date();
  today.setHours(0, 0, 0, 0);

  return state.tasks.filter(task => {
    const taskDate = new Date(task.scheduledFor);
    taskDate.setHours(0, 0, 0, 0);
    return taskDate.getTime() === today.getTime();
  });
};
