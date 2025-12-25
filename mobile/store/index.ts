/**
 * Store Index
 *
 * Centralized exports for all Zustand stores
 */

export {
  useTaskStore,
  selectTasksByStatus,
  selectPendingTasksCount,
  selectOverdueTasks,
  selectTodayTasks
} from './taskStore';

export {
  useAnomalyStore,
  selectAnomaliesBySeverity,
  selectAnomaliesCount,
  selectRecentAnomalies
} from './anomalyStore';
