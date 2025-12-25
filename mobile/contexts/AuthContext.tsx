/**
 * Authentication Context for Labor Control Mobile App
 *
 * Manages:
 * - User authentication state
 * - JWT token storage
 * - User role management (Admin/Supervisor/User)
 * - Whitelist synchronization
 */

import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { keychain as Keychain } from '@/utils/keychain';
import { syncWhitelist, clearWhitelist } from '@/services/storage/whitelistService';
import { setRefreshTokenFunction, loginUser } from '@/services/api/apiService';
import { initNetworkListener } from '@/store/offlineQueue';

// ============================================================================
// TYPES
// ============================================================================

export type UserRole = 'ADMIN' | 'SUPERVISOR' | 'USER';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  customerId: string;
  roles: UserRole[];
}

export interface AuthState {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: User | null;
  token: string | null;
  refreshToken: string | null;
  selectedRole: UserRole | null;
}

export interface AuthContextValue extends AuthState {
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  selectRole: (role: UserRole) => void;
  refreshToken: () => Promise<void>;
}

// ============================================================================
// CONTEXT
// ============================================================================

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

// ============================================================================
// CONFIGURATION
// ============================================================================

const API_BASE_URL = 'https://laborcontrol-api.azurewebsites.net/api';
const KEYCHAIN_SERVICE = 'laborcontrol';

// ============================================================================
// PROVIDER
// ============================================================================

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({
    isAuthenticated: false,
    isLoading: true,
    user: null,
    token: null,
    refreshToken: null,
    selectedRole: null,
  });

  // ==========================================================================
  // EFFECT: Check saved credentials on mount
  // ==========================================================================
  useEffect(() => {
    checkSavedCredentials();

    // Register refresh token function with apiService
    setRefreshTokenFunction(refreshTokenFn);
  }, []);

  // ==========================================================================
  // EFFECT: Initialize network listener when authenticated
  // ==========================================================================
  useEffect(() => {
    let unsubscribe: (() => void) | undefined;

    if (state.isAuthenticated && state.token) {
      console.log('[AUTH] Initializing offline queue network listener...');
      unsubscribe = initNetworkListener(state.token);
    }

    return () => {
      if (unsubscribe) {
        console.log('[AUTH] Cleaning up network listener...');
        unsubscribe();
      }
    };
  }, [state.isAuthenticated, state.token]);

  // ==========================================================================
  // FUNCTION: Check if user has saved credentials
  // ==========================================================================
  const checkSavedCredentials = async () => {
    try {
      console.log('[AUTH] ✅ Starting checkSavedCredentials...');
      // AUTO-LOGIN DISABLED: User must manually log in each time
      console.log('[AUTH] Auto-login disabled - user must login manually');
      console.log('[AUTH] ✅ checkSavedCredentials completed');
    } catch (error) {
      console.error('[AUTH] ❌ Error checking saved credentials:', error);
    } finally {
      console.log('[AUTH] ✅ Setting isLoading to false');
      setState(prev => ({ ...prev, isLoading: false }));
    }
  };

  // ==========================================================================
  // FUNCTION: Login
  // ==========================================================================
  const login = async (email: string, password: string) => {
    setState(prev => ({ ...prev, isLoading: true }));

    try {
      // Call backend login API using apiService
      const data = await loginUser(email, password);

      // DEBUG: Log API response to investigate empty profile data
      console.log('[AUTH] Login API response:', JSON.stringify(data, null, 2));

      // Save credentials to Keychain
      await Keychain.setInternetCredentials(KEYCHAIN_SERVICE, email, password);

      // Extract user data from response
      // API returns: { user: { id, email, prenom, nom, role, customerId } }
      const apiUser = data.user || data; // Support both nested and flat structure

      const user: User = {
        id: apiUser.id || data.userId,
        email: apiUser.email || data.email,
        firstName: apiUser.prenom || data.firstName || '',
        lastName: apiUser.nom || data.lastName || '',
        customerId: apiUser.customerId || data.customerId,
        roles: apiUser.role ? [apiUser.role] : (data.roles || ['USER']), // Convert single role to array
      };

      // DEBUG: Log extracted user data
      console.log('[AUTH] Extracted user:', JSON.stringify(user, null, 2));

      // TODO: Synchronize whitelist after successful login
      // DISABLED: Backend endpoint /api/rfidchips/whitelist/{customerId} not implemented yet
      // Will be enabled in Phase 2 after backend implementation
      /*
      try {
        console.log('[AUTH] Synchronizing whitelist...');
        const chipCount = await syncWhitelist(API_BASE_URL, data.token, data.customerId);
        console.log(`[AUTH] ✅ Whitelist synchronized: ${chipCount} chips`);
      } catch (error) {
        console.error('[AUTH] ⚠️ Whitelist sync failed:', error);
        // Continue anyway - whitelist sync is not critical for login
      }
      */
      console.log('[AUTH] ⚠️ Whitelist sync disabled - backend endpoint not implemented yet');

      setState({
        isAuthenticated: true,
        isLoading: false,
        user,
        token: data.token,
        refreshToken: data.refreshToken || null,
        selectedRole: null, // User will select role on next screen
      });
    } catch (error) {
      setState(prev => ({ ...prev, isLoading: false }));
      throw error;
    }
  };

  // ==========================================================================
  // FUNCTION: Logout
  // ==========================================================================
  const logout = async () => {
    try {
      // Clear Keychain credentials
      await Keychain.resetInternetCredentials(KEYCHAIN_SERVICE);

      // Clear whitelist
      await clearWhitelist();

      // Reset state
      setState({
        isAuthenticated: false,
        isLoading: false,
        user: null,
        token: null,
        refreshToken: null,
        selectedRole: null,
      });
    } catch (error) {
      console.error('[AUTH] Error during logout:', error);
    }
  };

  // ==========================================================================
  // FUNCTION: Select role
  // ==========================================================================
  const selectRole = (role: UserRole) => {
    // ADMIN can select any role
    const isAdmin = state.user?.roles.includes('ADMIN');
    const isSupervisor = state.user?.roles.includes('SUPERVISOR');
    const hasRole = state.user?.roles.includes(role);

    // Verify user has access to this role
    if (!isAdmin && !hasRole && !(isSupervisor && role === 'USER')) {
      throw new Error(`User does not have access to ${role} role`);
    }

    setState(prev => ({
      ...prev,
      selectedRole: role,
    }));
  };

  // ==========================================================================
  // FUNCTION: Refresh token
  // ==========================================================================
  const refreshTokenFn = async () => {
    try {
      const currentRefreshToken = state.refreshToken;

      if (!currentRefreshToken) {
        console.warn('[AUTH] No refresh token available');
        throw new Error('No refresh token available');
      }

      console.log('[AUTH] Refreshing access token...');

      const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: currentRefreshToken })
      });

      if (!response.ok) {
        throw new Error('Token refresh failed');
      }

      const data = await response.json();

      console.log('[AUTH] ✅ Token refreshed successfully');

      // Update state with new tokens
      setState(prev => ({
        ...prev,
        token: data.token,
        refreshToken: data.refreshToken || prev.refreshToken
      }));

      return data.token;
    } catch (error: any) {
      console.error('[AUTH] ❌ Token refresh failed:', error.message);

      // If refresh fails, logout the user
      await logout();
      throw error;
    }
  };

  // ==========================================================================
  // CONTEXT VALUE
  // ==========================================================================
  const value: AuthContextValue = {
    ...state,
    login,
    logout,
    selectRole,
    refreshToken: refreshTokenFn,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// ============================================================================
// HOOK
// ============================================================================

export function useAuth() {
  const context = useContext(AuthContext);

  if (context === undefined) {
    throw new Error('useAuth must be used within AuthProvider');
  }

  return context;
}
