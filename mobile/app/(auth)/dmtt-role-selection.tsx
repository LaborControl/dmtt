/**
 * DMTT Role Selection Screen
 *
 * Nuclear decommissioning specific role selection
 * 8 user profiles: Subcontractor, Welder, NDT Controller, CCPU,
 * Welding Coordinator, Quality Manager, EDF Inspector, Planner
 */

import React, { useEffect } from 'react';
import {
  StyleSheet,
  Text,
  View,
  TouchableOpacity,
  ScrollView,
  ActivityIndicator,
  Alert,
} from 'react-native';
import { router } from 'expo-router';
import { useAuth } from '@/contexts/AuthContext';
import { DMTTUserRole } from '@/services/api/apiService';

// Role configuration
interface RoleConfig {
  id: DMTTUserRole;
  name: string;
  description: string;
  icon: string;
  color: string;
  route: string;
}

const DMTT_ROLES: RoleConfig[] = [
  {
    id: 'WELDER',
    name: 'Soudeur',
    description: 'Exécution des soudures, scan NFC, consultation des qualifications',
    icon: 'S',
    color: '#f97316', // Orange
    route: '/(welder)/dashboard',
  },
  {
    id: 'NDT_CONTROLLER',
    name: 'Contrôleur CND',
    description: 'Contrôles non destructifs VT/PT/MT/RT/UT, enregistrement des résultats',
    icon: 'CND',
    color: '#3b82f6', // Blue
    route: '/(ndt-controller)/controls',
  },
  {
    id: 'CCPU',
    name: 'CCPU',
    description: 'Validation des prérequis, contrôle des matériaux et qualifications',
    icon: 'CC',
    color: '#8b5cf6', // Purple
    route: '/(ccpu)/welds',
  },
  {
    id: 'SUBCONTRACTOR',
    name: 'Sous-traitant',
    description: 'Gestion des ressources, suivi des travaux, documentation',
    icon: 'ST',
    color: '#10b981', // Green
    route: '/(subcontractor)/dashboard',
  },
  {
    id: 'WELDING_COORDINATOR',
    name: 'Coordinateur Soudage',
    description: 'Validation des qualifications, supervision technique, DMOS',
    icon: 'CS',
    color: '#eab308', // Yellow
    route: '/(coordinator)/dashboard',
  },
  {
    id: 'QUALITY_MANAGER',
    name: 'Responsable Qualité',
    description: 'Gestion des non-conformités, audits, indicateurs qualité',
    icon: 'Q',
    color: '#ef4444', // Red
    route: '/(admin)/dashboard',
  },
  {
    id: 'EDF_INSPECTOR',
    name: 'Inspecteur EDF',
    description: 'Supervision client, points d\'arrêt, validation finale',
    icon: 'I',
    color: '#06b6d4', // Cyan
    route: '/(admin)/dashboard',
  },
  {
    id: 'PLANNER',
    name: 'Planificateur',
    description: 'Planning des soudures et CND, allocation des ressources',
    icon: 'P',
    color: '#84cc16', // Lime
    route: '/(planner)/dashboard',
  },
];

export default function DMTTRoleSelectionScreen() {
  const { user, isAuthenticated, isLoading, logout } = useAuth();

  // Redirect to login if not authenticated
  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace('/login');
    }
  }, [isAuthenticated, isLoading]);

  // Loading state
  if (isLoading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#8b5cf6" />
        <Text style={styles.loadingText}>Chargement...</Text>
      </View>
    );
  }

  // Not authenticated
  if (!isAuthenticated || !user) {
    return null;
  }

  // Get user's available roles
  const getUserRoles = (): DMTTUserRole[] => {
    // For demo, map old roles to DMTT roles
    const roleMapping: Record<string, DMTTUserRole[]> = {
      ADMIN: ['QUALITY_MANAGER', 'EDF_INSPECTOR', 'WELDING_COORDINATOR', 'PLANNER'],
      SUPERVISOR: ['CCPU', 'WELDING_COORDINATOR'],
      USER: ['WELDER', 'NDT_CONTROLLER', 'SUBCONTRACTOR'],
    };

    const availableRoles: DMTTUserRole[] = [];
    user.roles.forEach((role) => {
      const mappedRoles = roleMapping[role];
      if (mappedRoles) {
        availableRoles.push(...mappedRoles);
      }
    });

    // Remove duplicates
    return [...new Set(availableRoles)];
  };

  const availableRoles = getUserRoles();

  // Handle role selection
  const handleRoleSelection = (role: RoleConfig) => {
    if (!availableRoles.includes(role.id)) {
      Alert.alert(
        'Accès refusé',
        `Vous n'avez pas les permissions pour accéder à l'interface ${role.name}.`
      );
      return;
    }

    // Navigate to role-specific interface
    router.replace(role.route as any);
  };

  // Check if user has access to role
  const hasAccess = (roleId: DMTTUserRole): boolean => {
    return availableRoles.includes(roleId);
  };

  return (
    <View style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.dmttBadge}>DMTT</Text>
        <Text style={styles.title}>Traçabilité Nucléaire</Text>
        <Text style={styles.subtitle}>Sélectionnez votre interface</Text>
        <Text style={styles.userName}>
          {user?.firstName || user?.lastName
            ? `${user?.firstName || ''} ${user?.lastName || ''}`.trim()
            : user?.email}
        </Text>
      </View>

      {/* Role Grid */}
      <ScrollView contentContainerStyle={styles.scrollContent}>
        <View style={styles.rolesGrid}>
          {DMTT_ROLES.map((role) => {
            const isAccessible = hasAccess(role.id);
            return (
              <TouchableOpacity
                key={role.id}
                style={[
                  styles.roleCard,
                  { borderColor: role.color },
                  !isAccessible && styles.roleCardDisabled,
                ]}
                onPress={() => handleRoleSelection(role)}
                disabled={!isAccessible}
                activeOpacity={0.7}
              >
                <View style={[styles.roleIconContainer, { backgroundColor: role.color }]}>
                  <Text style={styles.roleIcon}>{role.icon}</Text>
                </View>
                <Text style={styles.roleName}>{role.name}</Text>
                <Text style={styles.roleDescription} numberOfLines={2}>
                  {role.description}
                </Text>
                {!isAccessible && (
                  <View style={styles.lockOverlay}>
                    <Text style={styles.lockIcon}>X</Text>
                  </View>
                )}
              </TouchableOpacity>
            );
          })}
        </View>

        {/* Logout Button */}
        <TouchableOpacity style={styles.logoutButton} onPress={logout}>
          <Text style={styles.logoutText}>Se déconnecter</Text>
        </TouchableOpacity>

        {/* Footer */}
        <Text style={styles.footer}>
          Labor Control DMTT v1.0 - Tricastin
        </Text>
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#0f172a',
  },
  loadingContainer: {
    flex: 1,
    backgroundColor: '#0f172a',
    alignItems: 'center',
    justifyContent: 'center',
  },
  loadingText: {
    marginTop: 16,
    fontSize: 16,
    color: '#94a3b8',
  },
  header: {
    backgroundColor: '#1e293b',
    paddingTop: 60,
    paddingBottom: 24,
    paddingHorizontal: 20,
    alignItems: 'center',
    borderBottomWidth: 1,
    borderBottomColor: '#334155',
  },
  dmttBadge: {
    backgroundColor: '#8b5cf6',
    color: '#fff',
    fontSize: 12,
    fontWeight: 'bold',
    paddingHorizontal: 12,
    paddingVertical: 4,
    borderRadius: 12,
    overflow: 'hidden',
    marginBottom: 12,
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#f1f5f9',
    marginBottom: 4,
  },
  subtitle: {
    fontSize: 14,
    color: '#94a3b8',
    marginBottom: 12,
  },
  userName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#8b5cf6',
  },
  scrollContent: {
    padding: 16,
  },
  rolesGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-between',
    gap: 12,
  },
  roleCard: {
    width: '48%',
    backgroundColor: '#1e293b',
    borderRadius: 16,
    padding: 16,
    borderWidth: 2,
    marginBottom: 4,
    position: 'relative',
    overflow: 'hidden',
  },
  roleCardDisabled: {
    opacity: 0.4,
  },
  roleIconContainer: {
    width: 48,
    height: 48,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 12,
  },
  roleIcon: {
    fontSize: 14,
    fontWeight: 'bold',
    color: '#fff',
  },
  roleName: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#f1f5f9',
    marginBottom: 4,
  },
  roleDescription: {
    fontSize: 11,
    color: '#94a3b8',
    lineHeight: 14,
  },
  lockOverlay: {
    position: 'absolute',
    top: 8,
    right: 8,
  },
  lockIcon: {
    fontSize: 14,
    fontWeight: 'bold',
    color: '#64748b',
  },
  logoutButton: {
    marginTop: 24,
    backgroundColor: '#ef4444',
    paddingVertical: 16,
    borderRadius: 12,
    alignItems: 'center',
  },
  logoutText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
  },
  footer: {
    textAlign: 'center',
    color: '#475569',
    fontSize: 12,
    marginTop: 24,
    marginBottom: 32,
  },
});
