/**
 * API Service - Centralized API communication
 *
 * Handles all HTTP requests to the Labor Control backend
 * Includes automatic JWT refresh on 401 errors
 */

const API_BASE_URL = 'https://laborcontrol-api.azurewebsites.net/api';

// Global reference to refresh token function (will be set by AuthContext)
let globalRefreshTokenFn: (() => Promise<string>) | null = null;

/**
 * Set the global refresh token function
 * Called by AuthContext on initialization
 */
export function setRefreshTokenFunction(fn: () => Promise<string>) {
  globalRefreshTokenFn = fn;
}

/**
 * Wrapper for API calls with automatic token refresh on 401
 * Usage: await apiCallWithRefresh(() => getScheduledTasks(userId, token))
 */
export async function apiCallWithRefresh<T>(
  apiCall: () => Promise<T>
): Promise<T> {
  try {
    return await apiCall();
  } catch (error: any) {
    // If 401 Unauthorized, try to refresh token and retry
    if (error.status === 401 && globalRefreshTokenFn) {
      console.log('[API] Token expired (401), attempting refresh...');

      try {
        await globalRefreshTokenFn();
        console.log('[API] Token refreshed, retrying API call...');

        // Retry the original API call with new token
        return await apiCall();
      } catch (refreshError) {
        console.error('[API] Token refresh failed, cannot retry:', refreshError);
        throw refreshError;
      }
    }

    // For other errors, just throw
    throw error;
  }
}

export interface ApiError {
  message: string;
  status: number;
  details?: any;
}

/**
 * Make an authenticated API request
 */
async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {},
  token?: string
): Promise<T> {
  const url = `${API_BASE_URL}${endpoint}`;

  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...options.headers,
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  try {
    const response = await fetch(url, {
      ...options,
      headers,
    });

    const responseText = await response.text();

    if (!response.ok) {
      const error: ApiError = {
        message: 'Request failed',
        status: response.status,
      };

      try {
        const errorData = JSON.parse(responseText);
        error.message = errorData.error || errorData.message || 'Unknown error';
        error.details = errorData;
      } catch {
        error.message = responseText || `HTTP ${response.status}`;
      }

      throw error;
    }

    if (!responseText) {
      return {} as T;
    }

    return JSON.parse(responseText) as T;
  } catch (error: any) {
    if (error.status) {
      throw error;
    }

    throw {
      message: 'Network error',
      status: 0,
      details: error,
    } as ApiError;
  }
}

// ============================================================================
// AUTH API
// ============================================================================

export interface LoginResponse {
  token: string;
  userId: string;
  email: string;
  firstName?: string;
  lastName?: string;
  customerId: string;
  roles: string[];
}

export async function loginUser(email: string, password: string): Promise<LoginResponse> {
  return apiRequest<LoginResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  });
}

// ============================================================================
// SCHEDULED TASKS API
// ============================================================================

export interface ScheduledTask {
  id: string;
  scheduledDate: string;
  scheduledTimeStart: string;
  scheduledTimeEnd: string;
  status: string;
  requireDoubleScan: boolean;
  controlPointId: string;
  controlPointName?: string;
  controlPoint?: {
    id: string;
    name: string;
    locationDescription: string;
    rfidChip?: { chipId: string };
  };
  taskTemplate?: {
    id: string;
    name: string;
    formTemplate?: string; // JSON dynamic form
  };
}

export async function getScheduledTasks(userId: string, token: string): Promise<ScheduledTask[]> {
  return apiRequest<ScheduledTask[]>(`/scheduledtasks/user/${userId}`, {}, token);
}

// ============================================================================
// CONTROL POINTS API
// ============================================================================

export interface ControlPoint {
  id: string;
  name: string;
  locationDescription: string;
  rfidChip?: {
    chipId: string;
    uid: string;
  };
}

export async function getControlPointByUid(uid: string, token: string): Promise<ControlPoint> {
  return apiRequest<ControlPoint>(`/controlpoints/by-uid/${uid}`, {}, token);
}

// ============================================================================
// TASK EXECUTIONS API
// ============================================================================

export interface CreateTaskExecutionPayload {
  userId: string;
  controlPointId: string;
  scheduledTaskId?: string;
  scannedAt: string;
  submittedAt: string;
  formDataJson: string;
  type: 'SCHEDULED' | 'UNSCHEDULED';
  status: string;
}

export interface TaskExecution {
  id: string;
  scannedAt: string;
  submittedAt: string;
  controlPointName: string;
  type: 'SCHEDULED' | 'UNSCHEDULED';
  status: string;
  formDataJson: string;
}

export async function createTaskExecution(
  payload: CreateTaskExecutionPayload,
  token: string
): Promise<TaskExecution> {
  return apiRequest<TaskExecution>('/taskexecutions', {
    method: 'POST',
    body: JSON.stringify(payload),
  }, token);
}

export async function getTaskExecutions(
  userId: string,
  token: string,
  startDate?: string
): Promise<TaskExecution[]> {
  const url = startDate
    ? `/taskexecutions/${userId}?startDate=${startDate}`
    : `/taskexecutions/${userId}`;

  return apiRequest<TaskExecution[]>(url, {}, token);
}

// ============================================================================
// DOUBLE BORNAGE API
// ============================================================================

export interface FirstScanPayload {
  scheduledTaskId: string;
  controlPointId: string;
  userId: string;
  firstScanAt: string;
}

export interface FirstScanResponse {
  executionId: string;
  firstScanAt: string;
  message: string;
}

export async function firstScan(
  payload: FirstScanPayload,
  token: string
): Promise<FirstScanResponse> {
  return apiRequest<FirstScanResponse>('/taskexecutions/first-scan', {
    method: 'POST',
    body: JSON.stringify(payload),
  }, token);
}

export interface SecondScanPayload {
  executionId: string;
  secondScanAt: string;
  formData: string;
  photoUrl?: string | null;
}

export interface SecondScanResponse {
  executionId: string;
  totalWorkTime: string;
  message: string;
}

export async function secondScan(
  payload: SecondScanPayload,
  token: string
): Promise<SecondScanResponse> {
  return apiRequest<SecondScanResponse>('/taskexecutions/second-scan', {
    method: 'POST',
    body: JSON.stringify(payload),
  }, token);
}

// ============================================================================
// ANOMALIES API
// ============================================================================

export interface CreateAnomalyPayload {
  userId: string;
  controlPointId: string;
  detectedAt: string;
  severity: 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL';
  description: string;
  photoUrl?: string | null;
}

export interface Anomaly {
  id: string;
  userId: string;
  controlPointId: string;
  detectedAt: string;
  severity: string;
  description: string;
  photoUrl?: string;
  status: string;
}

export async function createAnomaly(
  payload: CreateAnomalyPayload,
  token: string
): Promise<Anomaly> {
  return apiRequest<Anomaly>('/anomalies', {
    method: 'POST',
    body: JSON.stringify(payload),
  }, token);
}

// ============================================================================
// RFID CHIPS API
// ============================================================================

export interface QuickRegisterChipPayload {
  uid: string;
}

export interface QuickRegisterChipResponse {
  chip: {
    chipId: string;
    uid: string;
    status: string;
  };
  message: string;
}

export async function quickRegisterChip(
  uid: string,
  token: string
): Promise<QuickRegisterChipResponse> {
  return apiRequest<QuickRegisterChipResponse>('/rfidchips/quick-register', {
    method: 'POST',
    body: JSON.stringify({ uid }),
  }, token);
}

// ============================================================================
// DMTT - NUCLEAR DECOMMISSIONING TRACEABILITY
// ============================================================================

// Types for DMTT
export type DMTTUserRole =
  | 'SUBCONTRACTOR'
  | 'WELDER'
  | 'NDT_CONTROLLER'
  | 'CCPU'
  | 'WELDING_COORDINATOR'
  | 'QUALITY_MANAGER'
  | 'EDF_INSPECTOR'
  | 'PLANNER';

export type WeldStatus =
  | 'PENDING_CCPU'
  | 'VALIDATED_CCPU'
  | 'IN_PROGRESS'
  | 'COMPLETED'
  | 'NDT_PENDING'
  | 'NDT_COMPLETED';

export type NDTControlType = 'VT' | 'PT' | 'MT' | 'RT' | 'UT';
export type NDTControlStatus = 'PENDING' | 'IN_PROGRESS' | 'COMPLETED';

export type NCStatus =
  | 'OPEN'
  | 'ANALYSIS'
  | 'PENDING_ACTION'
  | 'ACTION_IN_PROGRESS'
  | 'PENDING_VERIFICATION'
  | 'CLOSED';

// WELDS API
export interface Weld {
  id: string;
  reference: string;
  assetName: string;
  weldingProcess: string;
  welderName: string;
  status: WeldStatus;
  ccpuValidated: boolean;
  ndtProgress: string;
  firstScanAt?: string;
  secondScanAt?: string;
  executedAt?: string;
  createdAt: string;
}

export async function getWeldsForCCPU(token: string): Promise<Weld[]> {
  const response = await apiRequest<{ items: Weld[] }>('/welds?status=PENDING_CCPU', {}, token);
  return response.items || [];
}

export async function getWeldById(weldId: string, token: string): Promise<Weld> {
  return apiRequest<Weld>(`/welds/${weldId}`, {}, token);
}

export async function validateWeldCCPU(weldId: string, token: string): Promise<Weld> {
  return apiRequest<Weld>(`/welds/${weldId}/validate-ccpu`, { method: 'POST' }, token);
}

export async function recordWeldScan(
  weldId: string,
  scanType: 'first' | 'second',
  token: string
): Promise<Weld> {
  return apiRequest<Weld>(`/welds/${weldId}/scan`, {
    method: 'POST',
    body: JSON.stringify({ scanType }),
  }, token);
}

// NDT CONTROLS API
export interface DefectInfo {
  type: string;
  location: string;
  size?: string;
  severity: 'MINOR' | 'MAJOR' | 'CRITICAL';
}

export interface NDTControl {
  id: string;
  reference: string;
  controlType: NDTControlType;
  weldReference: string;
  weldId: string;
  controllerName: string;
  status: NDTControlStatus;
  result?: 'ACCEPTABLE' | 'NOT_ACCEPTABLE';
  defectsFound?: DefectInfo[];
  executedAt?: string;
  createdAt: string;
}

export async function getMyNDTControls(token: string): Promise<NDTControl[]> {
  const response = await apiRequest<{ items: NDTControl[] }>('/ndtcontrols/my-controls', {}, token);
  return response.items || [];
}

export async function getPendingNDTControls(
  controlType?: NDTControlType,
  token?: string
): Promise<NDTControl[]> {
  const query = controlType ? `?controlType=${controlType}&status=PENDING` : '?status=PENDING';
  const response = await apiRequest<{ items: NDTControl[] }>(`/ndtcontrols${query}`, {}, token || '');
  return response.items || [];
}

export async function recordNDTControl(
  controlId: string,
  result: 'ACCEPTABLE' | 'NOT_ACCEPTABLE',
  defects: DefectInfo[] | undefined,
  token: string
): Promise<NDTControl> {
  return apiRequest<NDTControl>(`/ndtcontrols/${controlId}/record`, {
    method: 'POST',
    body: JSON.stringify({ result, defectsFound: defects }),
  }, token);
}

// MATERIALS API (CCPU)
export interface Material {
  id: string;
  reference: string;
  designation: string;
  materialType: string;
  certificateNumber?: string;
  heatNumber?: string;
  validated: boolean;
  validatedAt?: string;
  expirationDate?: string;
}

export async function getMaterialsForValidation(token: string): Promise<Material[]> {
  return apiRequest<Material[]>('/materials/pending-validation', {}, token);
}

export async function validateMaterial(materialId: string, token: string): Promise<Material> {
  return apiRequest<Material>(`/materials/${materialId}/validate`, { method: 'POST' }, token);
}

export async function getMaterialByCertificate(
  certificateNumber: string,
  token: string
): Promise<Material | null> {
  try {
    return await apiRequest<Material>(
      `/materials/by-certificate/${encodeURIComponent(certificateNumber)}`,
      {},
      token
    );
  } catch (error: any) {
    if (error.status === 404) return null;
    throw error;
  }
}

// WELDER QUALIFICATIONS API
export interface WelderQualification {
  id: string;
  qualificationNumber: string;
  weldingProcess: string;
  qualificationStandard: string;
  qualifiedMaterials?: string;
  thicknessRange?: string;
  qualifiedPositions?: string;
  issueDate: string;
  expirationDate: string;
  status: 'VALID' | 'EXPIRING_SOON' | 'EXPIRED';
  aiPreValidated: boolean;
  aiConfidenceScore?: number;
}

export async function getMyQualifications(token: string): Promise<WelderQualification[]> {
  return apiRequest<WelderQualification[]>('/welderqualifications/my-qualifications', {}, token);
}

export interface QualificationPreValidationResponse {
  success: boolean;
  confidenceScore: number;
  extractedData?: {
    qualificationNumber?: string;
    holderName?: string;
    weldingProcess?: string;
    expirationDate?: string;
  };
  warnings: string[];
  validationIssues: string[];
}

export async function preValidateQualification(
  documentBase64: string,
  mimeType: string,
  qualificationType: string,
  token: string
): Promise<QualificationPreValidationResponse> {
  return apiRequest<QualificationPreValidationResponse>('/welderqualifications/pre-validate', {
    method: 'POST',
    body: JSON.stringify({
      documentBase64,
      documentMimeType: mimeType,
      qualificationType,
    }),
  }, token);
}

// NON-CONFORMITIES API
export interface NonConformity {
  id: string;
  reference: string;
  ncType: string;
  severity: 'MINOR' | 'MAJOR' | 'CRITICAL';
  weldReference?: string;
  description: string;
  status: NCStatus;
  createdAt: string;
  correctiveAction?: string;
  preventiveAction?: string;
}

export async function getNonConformities(
  params: { status?: NCStatus; severity?: string; page?: number; pageSize?: number },
  token: string
): Promise<{ items: NonConformity[]; totalCount: number }> {
  const query = new URLSearchParams();
  if (params.status) query.set('status', params.status);
  if (params.severity) query.set('severity', params.severity);
  if (params.page) query.set('page', params.page.toString());
  if (params.pageSize) query.set('pageSize', params.pageSize.toString());

  return apiRequest<{ items: NonConformity[]; totalCount: number }>(
    `/nonconformities?${query}`,
    {},
    token
  );
}

export async function createNonConformity(
  data: { weldId?: string; ncType: string; severity: string; description: string },
  token: string
): Promise<NonConformity> {
  return apiRequest<NonConformity>('/nonconformities', {
    method: 'POST',
    body: JSON.stringify(data),
  }, token);
}

// AI RECOMMENDATIONS API
export interface NCRecommendationResponse {
  success: boolean;
  correctiveActionRecommendation?: string;
  preventiveActionRecommendation?: string;
  rootCauseAnalysis?: string;
  error?: string;
}

export async function getNCRecommendation(
  data: {
    ncType: string;
    severity: string;
    description: string;
    defectsFound?: string;
    weldingProcess?: string;
  },
  token: string
): Promise<NCRecommendationResponse> {
  return apiRequest<NCRecommendationResponse>('/ai/nc-recommendation', {
    method: 'POST',
    body: JSON.stringify(data),
  }, token);
}

// STATS APIs
export interface WelderStats {
  totalWelds: number;
  completedWelds: number;
  successRate: number;
  currentMonthWelds: number;
  qualificationsCount: number;
  validQualificationsCount: number;
}

export async function getWelderStats(token: string): Promise<WelderStats> {
  return apiRequest<WelderStats>('/welders/my-stats', {}, token);
}

export interface NDTControllerStats {
  totalControls: number;
  completedControls: number;
  pendingControls: number;
  defectsFound: number;
  currentMonthControls: number;
}

export async function getNDTControllerStats(token: string): Promise<NDTControllerStats> {
  return apiRequest<NDTControllerStats>('/ndtcontrollers/my-stats', {}, token);
}

export interface CCPUStats {
  totalValidations: number;
  weldValidations: number;
  materialValidations: number;
  pendingValidations: number;
  currentMonthValidations: number;
}

export async function getCCPUStats(token: string): Promise<CCPUStats> {
  return apiRequest<CCPUStats>('/ccpu/my-stats', {}, token);
}
