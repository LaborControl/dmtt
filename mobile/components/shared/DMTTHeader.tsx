/**
 * DMTT Header Component
 *
 * Shared header for all DMTT screens
 * Shows role badge, user info, and navigation
 */

import React from 'react';
import {
  StyleSheet,
  Text,
  View,
  TouchableOpacity,
  StatusBar,
} from 'react-native';
import { router } from 'expo-router';
import { useAuth } from '@/contexts/AuthContext';

interface DMTTHeaderProps {
  title: string;
  subtitle?: string;
  role: 'WELDER' | 'NDT_CONTROLLER' | 'CCPU' | 'SUBCONTRACTOR' | 'COORDINATOR' | 'QUALITY' | 'INSPECTOR' | 'PLANNER';
  showBack?: boolean;
  showProfile?: boolean;
  rightAction?: {
    icon: string;
    onPress: () => void;
  };
}

const ROLE_CONFIG = {
  WELDER: { name: 'Soudeur', color: '#f97316', icon: 'S' },
  NDT_CONTROLLER: { name: 'Contrôleur CND', color: '#3b82f6', icon: 'CND' },
  CCPU: { name: 'CCPU', color: '#8b5cf6', icon: 'CC' },
  SUBCONTRACTOR: { name: 'Sous-traitant', color: '#10b981', icon: 'ST' },
  COORDINATOR: { name: 'Coordinateur', color: '#eab308', icon: 'CS' },
  QUALITY: { name: 'Qualité', color: '#ef4444', icon: 'Q' },
  INSPECTOR: { name: 'Inspecteur', color: '#06b6d4', icon: 'I' },
  PLANNER: { name: 'Planificateur', color: '#84cc16', icon: 'P' },
};

export default function DMTTHeader({
  title,
  subtitle,
  role,
  showBack = false,
  showProfile = true,
  rightAction,
}: DMTTHeaderProps) {
  const { user, logout } = useAuth();
  const roleConfig = ROLE_CONFIG[role];

  const handleBack = () => {
    if (router.canGoBack()) {
      router.back();
    } else {
      router.replace('/dmtt-role-selection');
    }
  };

  const handleProfile = () => {
    // Navigate to role-specific profile
    const profileRoutes: Record<string, string> = {
      WELDER: '/(welder)/profile',
      NDT_CONTROLLER: '/(ndt-controller)/profile',
      CCPU: '/(ccpu)/profile',
    };
    const profileRoute = profileRoutes[role];
    if (profileRoute) {
      router.push(profileRoute as any);
    }
  };

  return (
    <>
      <StatusBar barStyle="light-content" backgroundColor="#0f172a" />
      <View style={[styles.container, { borderBottomColor: roleConfig.color }]}>
        {/* Top Row: Back + Title + Actions */}
        <View style={styles.topRow}>
          {/* Left: Back Button or Role Badge */}
          {showBack ? (
            <TouchableOpacity style={styles.backButton} onPress={handleBack}>
              <Text style={styles.backIcon}>←</Text>
            </TouchableOpacity>
          ) : (
            <View style={[styles.roleBadge, { backgroundColor: roleConfig.color }]}>
              <Text style={styles.roleIcon}>{roleConfig.icon}</Text>
              <Text style={styles.roleName}>{roleConfig.name}</Text>
            </View>
          )}

          {/* Right: Actions */}
          <View style={styles.actions}>
            {rightAction && (
              <TouchableOpacity style={styles.actionButton} onPress={rightAction.onPress}>
                <Text style={styles.actionIcon}>{rightAction.icon}</Text>
              </TouchableOpacity>
            )}
            {showProfile && (
              <TouchableOpacity style={styles.profileButton} onPress={handleProfile}>
                <View style={[styles.avatar, { backgroundColor: roleConfig.color }]}>
                  <Text style={styles.avatarText}>
                    {user?.firstName?.[0] || user?.email?.[0] || '?'}
                  </Text>
                </View>
              </TouchableOpacity>
            )}
          </View>
        </View>

        {/* Bottom Row: Page Title */}
        <View style={styles.titleRow}>
          <Text style={styles.title}>{title}</Text>
          {subtitle && <Text style={styles.subtitle}>{subtitle}</Text>}
        </View>
      </View>
    </>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: '#1e293b',
    paddingTop: 50,
    paddingBottom: 16,
    paddingHorizontal: 16,
    borderBottomWidth: 3,
  },
  topRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  backButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: '#334155',
    alignItems: 'center',
    justifyContent: 'center',
  },
  backIcon: {
    fontSize: 20,
    color: '#f1f5f9',
  },
  roleBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 20,
    gap: 6,
  },
  roleIcon: {
    fontSize: 10,
    fontWeight: 'bold',
    color: '#fff',
  },
  roleName: {
    fontSize: 12,
    fontWeight: 'bold',
    color: '#fff',
  },
  actions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  actionButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: '#334155',
    alignItems: 'center',
    justifyContent: 'center',
  },
  actionIcon: {
    fontSize: 18,
  },
  profileButton: {
    padding: 2,
  },
  avatar: {
    width: 40,
    height: 40,
    borderRadius: 20,
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarText: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#fff',
  },
  titleRow: {
    marginTop: 4,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#f1f5f9',
  },
  subtitle: {
    fontSize: 14,
    color: '#94a3b8',
    marginTop: 2,
  },
});
