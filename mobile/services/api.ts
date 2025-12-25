/**
 * Labor Control DMTT - Mobile API Service
 * Handles all API communication with the backend
 */

import AsyncStorage from '@react-native-async-storage/async-storage';

// API Configuration
const API_BASE_URL = process.env.EXPO_PUBLIC_API_URL || 'https://laborcontrol-dmtt-api.azurewebsites.net';

// Storage keys
const ACCESS_TOKEN_KEY = 'access_token';
const REFRESH_TOKEN_KEY = 'refresh_token';
const USER_DATA_KEY = 'user_data';

// Types
export interface User {
  id: string;
  email: string;
  nom: string;
  prenom: string;
  role: UserRole;
  companyName?: string;
  matricule?: string;
}

export type UserRole =
  | 'SUBCONTRACTOR'
  | 'WELDER'
  | 'NDT_CONTROLLER'
  | 'CCPU'
  | 'WELDING_COORDINATOR'
  | 'QUALITY_MANAGER'
  | 'EDF_INSPECTOR'
  | 'PLANNER';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
  expiresIn: number;
}

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

export type WeldStatus =
  | 'PENDING_CCPU'
  | 'VALIDATED_CCPU'
  | 'IN_PROGRESS'
  | 'COMPLETED'
  | 'NDT_PENDING'
  | 'NDT_COMPLETED';

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

export type NDTControlType = 'VT' | 'PT' | 'MT' | 'RT' | 'UT';
export type NDTControlStatus = 'PENDING' | 'IN_PROGRESS' | 'COMPLETED';

export interface DefectInfo {
  type: string;
  location: string;
  size?: string;
  severity: 'MINOR' | 'MAJOR' | 'CRITICAL';
}

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

export type NCStatus =
  | 'OPEN'
  | 'ANALYSIS'
  | 'PENDING_ACTION'
  | 'ACTION_IN_PROGRESS'
  | 'PENDING_VERIFICATION'
  | 'CLOSED';

// API Error class
export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    public code?: string
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

// API Service class
class ApiService {
  private accessToken: string | null = null;
  private refreshToken: string | null = null;
  private user: User | null = null;

  constructor() {
    this.loadTokens();
  }

  private async loadTokens(): Promise<void> {
    try {
      const [accessToken, refreshToken, userData] = await Promise.all([
        AsyncStorage.getItem(ACCESS_TOKEN_KEY),
        AsyncStorage.getItem(REFRESH_TOKEN_KEY),
        AsyncStorage.getItem(USER_DATA_KEY),
      ]);

      this.accessToken = accessToken;
      this.refreshToken = refreshToken;
      this.user = userData ? JSON.parse(userData) : null;
    } catch (error) {
      console.error('Error loading tokens:', error);
    }
  }

  private async saveTokens(accessToken: string, refreshToken: string, user: User): Promise<void> {
    this.accessToken = accessToken;
    this.refreshToken = refreshToken;
    this.user = user;

    await Promise.all([
      AsyncStorage.setItem(ACCESS_TOKEN_KEY, accessToken),
      AsyncStorage.setItem(REFRESH_TOKEN_KEY, refreshToken),
      AsyncStorage.setItem(USER_DATA_KEY, JSON.stringify(user)),
    ]);
  }

  private async clearTokens(): Promise<void> {
    this.accessToken = null;
    this.refreshToken = null;
    this.user = null;

    await Promise.all([
      AsyncStorage.removeItem(ACCESS_TOKEN_KEY),
      AsyncStorage.removeItem(REFRESH_TOKEN_KEY),
      AsyncStorage.removeItem(USER_DATA_KEY),
    ]);
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${API_BASE_URL}${endpoint}`;
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      ...options.headers,
    };

    if (this.accessToken) {
      (headers as Record<string, string>)['Authorization'] = `Bearer ${this.accessToken}`;
    }

    const response = await fetch(url, {
      ...options,
      headers,
    });

    if (response.status === 401 && this.refreshToken) {
      // Try to refresh token
      const refreshed = await this.tryRefreshToken();
      if (refreshed) {
        // Retry request with new token
        (headers as Record<string, string>)['Authorization'] = `Bearer ${this.accessToken}`;
        const retryResponse = await fetch(url, { ...options, headers });
        if (!retryResponse.ok) {
          throw new ApiError(retryResponse.status, await retryResponse.text());
        }
        return retryResponse.json();
      }
      // Refresh failed, user needs to login again
      await this.clearTokens();
      throw new ApiError(401, 'Session expirée, veuillez vous reconnecter');
    }

    if (!response.ok) {
      const errorText = await response.text();
      throw new ApiError(response.status, errorText);
    }

    // Handle empty responses
    const text = await response.text();
    if (!text) return {} as T;

    return JSON.parse(text);
  }

  private async tryRefreshToken(): Promise<boolean> {
    if (!this.refreshToken) return false;

    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: this.refreshToken }),
      });

      if (!response.ok) return false;

      const data: LoginResponse = await response.json();
      await this.saveTokens(data.accessToken, data.refreshToken, data.user);
      return true;
    } catch {
      return false;
    }
  }

  // Auth
  async login(request: LoginRequest): Promise<LoginResponse> {
    const response = await this.request<LoginResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify(request),
    });
    await this.saveTokens(response.accessToken, response.refreshToken, response.user);
    return response;
  }

  async logout(): Promise<void> {
    try {
      await this.request('/api/auth/logout', { method: 'POST' });
    } finally {
      await this.clearTokens();
    }
  }

  async getCurrentUser(): Promise<User | null> {
    if (!this.accessToken) return null;
    return this.user;
  }

  isAuthenticated(): boolean {
    return !!this.accessToken;
  }

  // Welds
  async getWelds(params?: {
    status?: WeldStatus;
    page?: number;
    pageSize?: number;
  }): Promise<{ items: Weld[]; totalCount: number }> {
    const query = new URLSearchParams();
    if (params?.status) query.set('status', params.status);
    if (params?.page) query.set('page', params.page.toString());
    if (params?.pageSize) query.set('pageSize', params.pageSize.toString());

    return this.request(`/api/welds?${query}`);
  }

  async getWeld(id: string): Promise<Weld> {
    return this.request(`/api/welds/${id}`);
  }

  async getWeldsForCCPU(): Promise<Weld[]> {
    const response = await this.request<{ items: Weld[] }>('/api/welds?status=PENDING_CCPU');
    return response.items;
  }

  async validateWeldCCPU(weldId: string): Promise<Weld> {
    return this.request(`/api/welds/${weldId}/validate-ccpu`, {
      method: 'POST',
    });
  }

  async recordWeldScan(weldId: string, scanType: 'first' | 'second'): Promise<Weld> {
    return this.request(`/api/welds/${weldId}/scan`, {
      method: 'POST',
      body: JSON.stringify({ scanType }),
    });
  }

  // NDT Controls
  async getNDTControls(params?: {
    status?: NDTControlStatus;
    controlType?: NDTControlType;
    page?: number;
    pageSize?: number;
  }): Promise<{ items: NDTControl[]; totalCount: number }> {
    const query = new URLSearchParams();
    if (params?.status) query.set('status', params.status);
    if (params?.controlType) query.set('controlType', params.controlType);
    if (params?.page) query.set('page', params.page.toString());
    if (params?.pageSize) query.set('pageSize', params.pageSize.toString());

    return this.request(`/api/ndtcontrols?${query}`);
  }

  async getMyNDTControls(): Promise<NDTControl[]> {
    const response = await this.request<{ items: NDTControl[] }>('/api/ndtcontrols/my-controls');
    return response.items;
  }

  async recordNDTControl(
    controlId: string,
    result: 'ACCEPTABLE' | 'NOT_ACCEPTABLE',
    defects?: DefectInfo[]
  ): Promise<NDTControl> {
    return this.request(`/api/ndtcontrols/${controlId}/record`, {
      method: 'POST',
      body: JSON.stringify({ result, defectsFound: defects }),
    });
  }

  // Materials (CCPU)
  async getMaterialsForValidation(): Promise<Material[]> {
    return this.request('/api/materials/pending-validation');
  }

  async validateMaterial(materialId: string): Promise<Material> {
    return this.request(`/api/materials/${materialId}/validate`, {
      method: 'POST',
    });
  }

  async getMaterialByCertificate(certificateNumber: string): Promise<Material | null> {
    try {
      return await this.request(`/api/materials/by-certificate/${encodeURIComponent(certificateNumber)}`);
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        return null;
      }
      throw error;
    }
  }

  // Welder Qualifications
  async getMyQualifications(): Promise<WelderQualification[]> {
    return this.request('/api/welderqualifications/my-qualifications');
  }

  async getQualification(id: string): Promise<WelderQualification> {
    return this.request(`/api/welderqualifications/${id}`);
  }

  async preValidateQualification(
    documentBase64: string,
    mimeType: string,
    qualificationType: string
  ): Promise<{
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
  }> {
    return this.request('/api/welderqualifications/pre-validate', {
      method: 'POST',
      body: JSON.stringify({
        documentBase64,
        documentMimeType: mimeType,
        qualificationType,
      }),
    });
  }

  // Non-Conformities
  async getNonConformities(params?: {
    status?: NCStatus;
    severity?: string;
    page?: number;
    pageSize?: number;
  }): Promise<{ items: NonConformity[]; totalCount: number }> {
    const query = new URLSearchParams();
    if (params?.status) query.set('status', params.status);
    if (params?.severity) query.set('severity', params.severity);
    if (params?.page) query.set('page', params.page.toString());
    if (params?.pageSize) query.set('pageSize', params.pageSize.toString());

    return this.request(`/api/nonconformities?${query}`);
  }

  async createNonConformity(data: {
    weldId?: string;
    ncType: string;
    severity: string;
    description: string;
  }): Promise<NonConformity> {
    return this.request('/api/nonconformities', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  // AI Recommendations (for NC)
  async getNCRecommendation(data: {
    ncType: string;
    severity: string;
    description: string;
    defectsFound?: string;
    weldingProcess?: string;
  }): Promise<{
    success: boolean;
    correctiveActionRecommendation?: string;
    preventiveActionRecommendation?: string;
    rootCauseAnalysis?: string;
    error?: string;
  }> {
    return this.request('/api/ai/nc-recommendation', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  // Dashboard Stats
  async getDashboardStats(): Promise<{
    totalWelds: number;
    completedWelds: number;
    pendingNDT: number;
    openNonConformities: number;
    activeWelders: number;
    weeklyProgress: number;
    acceptanceRate: number;
  }> {
    return this.request('/api/dashboard/stats');
  }

  // Welder Profile Stats
  async getWelderStats(): Promise<{
    totalWelds: number;
    completedWelds: number;
    successRate: number;
    currentMonthWelds: number;
    qualificationsCount: number;
    validQualificationsCount: number;
  }> {
    return this.request('/api/welders/my-stats');
  }

  // NDT Controller Stats
  async getNDTControllerStats(): Promise<{
    totalControls: number;
    completedControls: number;
    pendingControls: number;
    defectsFound: number;
    currentMonthControls: number;
  }> {
    return this.request('/api/ndtcontrollers/my-stats');
  }

  // CCPU Stats
  async getCCPUStats(): Promise<{
    totalValidations: number;
    weldValidations: number;
    materialValidations: number;
    pendingValidations: number;
    currentMonthValidations: number;
  }> {
    return this.request('/api/ccpu/my-stats');
  }
}

// Export singleton instance
export const api = new ApiService();
