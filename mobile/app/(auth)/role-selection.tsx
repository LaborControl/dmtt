/**
 * Role Selection Screen
 *
 * After login, users choose which interface to use:
 * - USER: Standard task scanning interface
 * - SUPERVISOR: Intercept delayed tasks, declare anomalies, view recent tasks
 * - ADMIN: Equipment creation, control points, chip assignment, chronos
 */

import React, { useEffect } from 'react';
import { StyleSheet, Text, View, TouchableOpacity, Image, Alert, ScrollView } from 'react-native';
import { router } from 'expo-router';
import { useAuth, UserRole } from '@/contexts/AuthContext';

export default function RoleSelectionScreen() {
  const { user, isAuthenticated, isLoading, selectRole, logout } = useAuth();

  // ==========================================================================
  // EFFECT: Redirect to login if not authenticated
  // ==========================================================================
  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace('/login');
    }
  }, [isAuthenticated, isLoading]);

  // ==========================================================================
  // LOADING STATE
  // ==========================================================================
  if (isLoading) {
    return (
      <View style={[styles.container, styles.centerContent]}>
        <Text style={styles.loadingText}>Chargement...</Text>
      </View>
    );
  }

  // ==========================================================================
  // NOT AUTHENTICATED (should redirect but show nothing meanwhile)
  // ==========================================================================
  if (!isAuthenticated || !user) {
    return null;
  }

  // ==========================================================================
  // FUNCTION: Handle role selection
  // ==========================================================================
  const handleRoleSelection = (role: UserRole) => {
    try {
      // Check if user has access to this role
      if (!hasRole(role)) {
        Alert.alert(
          'Accès refusé',
          `Vous n'avez pas les permissions pour accéder à l'interface ${getRoleName(role)}.`
        );
        return;
      }

      // Select role in context
      selectRole(role);

      // Navigate to appropriate interface
      switch (role) {
        case 'ADMIN':
          router.replace('/(admin)/equipment');
          break;
        case 'SUPERVISOR':
          router.replace('/(supervisor)/team');
          break;
        case 'USER':
          router.replace('/(user)/tasks');
          break;
      }
    } catch (error) {
      Alert.alert('Erreur', 'Impossible de sélectionner ce rôle');
    }
  };

  // ==========================================================================
  // FUNCTION: Get role display name
  // ==========================================================================
  const getRoleName = (role: UserRole): string => {
    switch (role) {
      case 'ADMIN':
        return 'Administrateur';
      case 'SUPERVISOR':
        return 'Superviseur';
      case 'USER':
        return 'Intervenant';
    }
  };

  // ==========================================================================
  // FUNCTION: Get role description
  // ==========================================================================
  const getRoleDescription = (role: UserRole): string => {
    switch (role) {
      case 'ADMIN':
        return 'Création équipements, points de contrôle, affectation puces, chronos';
      case 'SUPERVISOR':
        return 'Intercepter tâches en retard, déclarer anomalies, consulter activité';
      case 'USER':
        return 'Scanner et réaliser les tâches planifiées';
    }
  };

  // ==========================================================================
  // FUNCTION: Get role icon
  // ==========================================================================
  const getRoleIcon = (role: UserRole): string => {
    switch (role) {
      case 'ADMIN':
        return 'A';
      case 'SUPERVISOR':
        return 'S';
      case 'USER':
        return 'U';
    }
  };

  // ==========================================================================
  // FUNCTION: Get role color
  // ==========================================================================
  const getRoleColor = (role: UserRole): string => {
    switch (role) {
      case 'ADMIN':
        return '#8b5cf6'; // Purple
      case 'SUPERVISOR':
        return '#f59e0b'; // Amber
      case 'USER':
        return '#10b981'; // Green
    }
  };

  // ==========================================================================
  // FUNCTION: Check if user has role
  // ==========================================================================
  const hasRole = (role: UserRole): boolean => {
    // ADMIN has access to all roles
    if (user?.roles.includes('ADMIN')) {
      return true;
    }
    // SUPERVISOR has access to USER role
    if (user?.roles.includes('SUPERVISOR') && role === 'USER') {
      return true;
    }
    // Otherwise check if user has the specific role
    return user?.roles.includes(role) || false;
  };

  // ==========================================================================
  // RENDER
  // ==========================================================================
  return (
    <View style={styles.container}>
      <ScrollView contentContainerStyle={styles.scrollContent}>
        {/* HEADER */}
        <View style={styles.header}>
          <Image
            source={require('@/assets/images/logo.png')}
            style={styles.logo}
            resizeMode="contain"
          />
          <Text style={styles.title}>LABOR CONTROL</Text>
          <Text style={styles.subtitle}>Choisissez votre interface</Text>
          <Text style={styles.userName}>
            {user?.firstName} {user?.lastName}
          </Text>
        </View>

        {/* ROLE BUTTONS */}
        <View style={styles.rolesContainer}>
          {/* USER BUTTON - Always first */}
          <TouchableOpacity
            style={[
              styles.roleButton,
              styles.roleButtonUser,
              !hasRole('USER') && styles.roleButtonDisabled,
            ]}
            onPress={() => handleRoleSelection('USER')}
            disabled={!hasRole('USER')}
          >
            <Text style={styles.roleButtonIcon}>{getRoleIcon('USER')}</Text>
            <View style={styles.roleButtonTextContainer}>
              <Text style={styles.roleButtonTitle}>{getRoleName('USER')}</Text>
              <Text style={styles.roleButtonDescription}>{getRoleDescription('USER')}</Text>
            </View>
            {!hasRole('USER') && <Text style={styles.lockIcon}>X</Text>}
          </TouchableOpacity>

          {/* SUPERVISOR BUTTON */}
          <TouchableOpacity
            style={[
              styles.roleButton,
              styles.roleButtonSupervisor,
              !hasRole('SUPERVISOR') && styles.roleButtonDisabled,
            ]}
            onPress={() => handleRoleSelection('SUPERVISOR')}
            disabled={!hasRole('SUPERVISOR')}
          >
            <Text style={styles.roleButtonIcon}>{getRoleIcon('SUPERVISOR')}</Text>
            <View style={styles.roleButtonTextContainer}>
              <Text style={styles.roleButtonTitle}>{getRoleName('SUPERVISOR')}</Text>
              <Text style={styles.roleButtonDescription}>{getRoleDescription('SUPERVISOR')}</Text>
            </View>
            {!hasRole('SUPERVISOR') && <Text style={styles.lockIcon}>X</Text>}
          </TouchableOpacity>

          {/* ADMIN BUTTON */}
          <TouchableOpacity
            style={[
              styles.roleButton,
              styles.roleButtonAdmin,
              !hasRole('ADMIN') && styles.roleButtonDisabled,
            ]}
            onPress={() => handleRoleSelection('ADMIN')}
            disabled={!hasRole('ADMIN')}
          >
            <Text style={styles.roleButtonIcon}>{getRoleIcon('ADMIN')}</Text>
            <View style={styles.roleButtonTextContainer}>
              <Text style={styles.roleButtonTitle}>{getRoleName('ADMIN')}</Text>
              <Text style={styles.roleButtonDescription}>{getRoleDescription('ADMIN')}</Text>
            </View>
            {!hasRole('ADMIN') && <Text style={styles.lockIcon}>X</Text>}
          </TouchableOpacity>
        </View>

        {/* LOGOUT BUTTON */}
        <TouchableOpacity style={styles.logoutButton} onPress={logout}>
          <Text style={styles.logoutText}>Se déconnecter</Text>
        </TouchableOpacity>
      </ScrollView>
    </View>
  );
}

// ============================================================================
// STYLES
// ============================================================================

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8fafc',
  },
  scrollContent: {
    flexGrow: 1,
  },
  centerContent: {
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    fontSize: 18,
    color: '#64748b',
  },
  header: {
    backgroundColor: '#fff',
    paddingTop: 60,
    paddingBottom: 30,
    alignItems: 'center',
    borderBottomWidth: 1,
    borderBottomColor: '#e2e8f0',
  },
  logo: {
    width: 100,
    height: 100,
    marginBottom: 16,
  },
  title: {
    fontSize: 32,
    fontWeight: 'bold',
    color: '#2563eb',
    marginBottom: 8,
  },
  subtitle: {
    fontSize: 16,
    color: '#64748b',
    marginBottom: 12,
  },
  userName: {
    fontSize: 18,
    fontWeight: '600',
    color: '#1e293b',
  },
  rolesContainer: {
    padding: 20,
    gap: 16,
  },
  roleButton: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#fff',
    borderRadius: 16,
    padding: 20,
    borderWidth: 2,
    elevation: 2,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
  },
  roleButtonUser: {
    borderColor: '#10b981',
  },
  roleButtonSupervisor: {
    borderColor: '#f59e0b',
  },
  roleButtonAdmin: {
    borderColor: '#8b5cf6',
  },
  roleButtonDisabled: {
    opacity: 0.4,
    backgroundColor: '#f1f5f9',
  },
  roleButtonIcon: {
    fontSize: 28,
    fontWeight: 'bold',
    marginRight: 16,
    color: '#64748b',
  },
  roleButtonTextContainer: {
    flex: 1,
  },
  roleButtonTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#1e293b',
    marginBottom: 4,
  },
  roleButtonDescription: {
    fontSize: 13,
    color: '#64748b',
    lineHeight: 18,
  },
  lockIcon: {
    fontSize: 18,
    fontWeight: 'bold',
    marginLeft: 12,
    color: '#94a3b8',
  },
  logoutButton: {
    margin: 20,
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
});
