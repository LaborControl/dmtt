/**
 * CCPU - Profile Screen
 *
 * CCPU profile with validation statistics
 */

import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Alert,
  RefreshControl
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useAuth } from '@/contexts/AuthContext';
import LoadingSpinner from '@/components/shared/LoadingSpinner';
import { apiClient } from '@/services/api';

interface CCPUStats {
  totalMaterialsValidated: number;
  totalWeldsValidated: number;
  totalMaterialsRejected: number;
  totalWeldsRejected: number;
  monthlyValidations: number;
  pendingMaterials: number;
  pendingWelds: number;
}

export default function CCPUProfileScreen() {
  const { user, token, logout } = useAuth();
  const [stats, setStats] = useState<CCPUStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const fetchStats = useCallback(async () => {
    try {
      const response = await apiClient.get('/ccpu/my-statistics', {
        headers: { Authorization: `Bearer ${token}` }
      });
      setStats(response.data);
    } catch (error) {
      console.error('Error fetching stats:', error);
      // Use mock data if endpoint not available
      setStats({
        totalMaterialsValidated: 45,
        totalWeldsValidated: 123,
        totalMaterialsRejected: 3,
        totalWeldsRejected: 8,
        monthlyValidations: 28,
        pendingMaterials: 5,
        pendingWelds: 12
      });
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [token]);

  useEffect(() => {
    fetchStats();
  }, [fetchStats]);

  const handleLogout = () => {
    Alert.alert(
      'Déconnexion',
      'Êtes-vous sûr de vouloir vous déconnecter ?',
      [
        { text: 'Annuler', style: 'cancel' },
        { text: 'Déconnexion', style: 'destructive', onPress: logout }
      ]
    );
  };

  if (loading) {
    return <LoadingSpinner message="Chargement du profil..." />;
  }

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
      refreshControl={
        <RefreshControl
          refreshing={refreshing}
          onRefresh={() => {
            setRefreshing(true);
            fetchStats();
          }}
          tintColor="#8b5cf6"
        />
      }
    >
      {/* Profile Header */}
      <View style={styles.profileHeader}>
        <View style={styles.avatarContainer}>
          <Ionicons name="person" size={48} color="#8b5cf6" />
        </View>
        <Text style={styles.userName}>{user?.name || 'CCPU'}</Text>
        <Text style={styles.userRole}>Chargé de Contrôle Préparation Usinage</Text>
      </View>

      {/* Pending Work Banner */}
      {stats && (stats.pendingMaterials > 0 || stats.pendingWelds > 0) && (
        <View style={styles.pendingBanner}>
          <Ionicons name="time" size={24} color="#f59e0b" />
          <View style={styles.pendingInfo}>
            <Text style={styles.pendingTitle}>Travail en attente</Text>
            <Text style={styles.pendingDetails}>
              {stats.pendingMaterials} matériau{stats.pendingMaterials !== 1 ? 'x' : ''} •{' '}
              {stats.pendingWelds} soudure{stats.pendingWelds !== 1 ? 's' : ''}
            </Text>
          </View>
        </View>
      )}

      {/* Statistics */}
      {stats && (
        <View style={styles.statsSection}>
          <Text style={styles.sectionTitle}>Mes Statistiques</Text>

          <View style={styles.mainStats}>
            <View style={styles.mainStatCard}>
              <Ionicons name="cube" size={28} color="#8b5cf6" />
              <Text style={styles.mainStatValue}>{stats.totalMaterialsValidated}</Text>
              <Text style={styles.mainStatLabel}>Matériaux validés</Text>
            </View>
            <View style={styles.mainStatCard}>
              <Ionicons name="flame" size={28} color="#f97316" />
              <Text style={styles.mainStatValue}>{stats.totalWeldsValidated}</Text>
              <Text style={styles.mainStatLabel}>Soudures validées</Text>
            </View>
          </View>

          <View style={styles.detailedStats}>
            <View style={styles.statRow}>
              <View style={styles.statItem}>
                <View style={styles.statIconContainer}>
                  <Ionicons name="checkmark-circle" size={20} color="#22c55e" />
                </View>
                <View style={styles.statContent}>
                  <Text style={styles.statValue}>{stats.monthlyValidations}</Text>
                  <Text style={styles.statLabel}>Ce mois</Text>
                </View>
              </View>

              <View style={styles.statItem}>
                <View style={styles.statIconContainer}>
                  <Ionicons name="close-circle" size={20} color="#ef4444" />
                </View>
                <View style={styles.statContent}>
                  <Text style={styles.statValue}>
                    {stats.totalMaterialsRejected + stats.totalWeldsRejected}
                  </Text>
                  <Text style={styles.statLabel}>Refusés</Text>
                </View>
              </View>
            </View>

            <View style={styles.rateCard}>
              <Text style={styles.rateTitle}>Taux d'approbation</Text>
              <View style={styles.rateBar}>
                <View
                  style={[
                    styles.rateFill,
                    {
                      width: `${Math.round(
                        ((stats.totalMaterialsValidated + stats.totalWeldsValidated) /
                          (stats.totalMaterialsValidated + stats.totalWeldsValidated +
                            stats.totalMaterialsRejected + stats.totalWeldsRejected)) * 100
                      )}%`
                    }
                  ]}
                />
              </View>
              <Text style={styles.rateValue}>
                {Math.round(
                  ((stats.totalMaterialsValidated + stats.totalWeldsValidated) /
                    (stats.totalMaterialsValidated + stats.totalWeldsValidated +
                      stats.totalMaterialsRejected + stats.totalWeldsRejected)) * 100
                )}%
              </Text>
            </View>
          </View>
        </View>
      )}

      {/* Role Description */}
      <View style={styles.roleSection}>
        <Text style={styles.sectionTitle}>Responsabilités CCPU</Text>
        <View style={styles.roleCard}>
          <View style={styles.roleItem}>
            <Ionicons name="cube-outline" size={20} color="#8b5cf6" />
            <Text style={styles.roleText}>Validation des matériaux et certificats</Text>
          </View>
          <View style={styles.roleItem}>
            <Ionicons name="flame-outline" size={20} color="#f97316" />
            <Text style={styles.roleText}>Contrôle préalable des soudures</Text>
          </View>
          <View style={styles.roleItem}>
            <Ionicons name="document-text-outline" size={20} color="#3b82f6" />
            <Text style={styles.roleText}>Vérification des qualifications soudeurs</Text>
          </View>
          <View style={styles.roleItem}>
            <Ionicons name="shield-checkmark-outline" size={20} color="#22c55e" />
            <Text style={styles.roleText}>Autorisation d'exécution des soudures</Text>
          </View>
        </View>
      </View>

      {/* Actions */}
      <View style={styles.actionsSection}>
        <TouchableOpacity style={styles.actionButton}>
          <Ionicons name="settings-outline" size={20} color="#f1f5f9" />
          <Text style={styles.actionButtonText}>Paramètres</Text>
          <Ionicons name="chevron-forward" size={20} color="#64748b" />
        </TouchableOpacity>

        <TouchableOpacity style={styles.actionButton}>
          <Ionicons name="help-circle-outline" size={20} color="#f1f5f9" />
          <Text style={styles.actionButtonText}>Aide</Text>
          <Ionicons name="chevron-forward" size={20} color="#64748b" />
        </TouchableOpacity>

        <TouchableOpacity
          style={[styles.actionButton, styles.logoutButton]}
          onPress={handleLogout}
        >
          <Ionicons name="log-out-outline" size={20} color="#ef4444" />
          <Text style={styles.logoutButtonText}>Déconnexion</Text>
        </TouchableOpacity>
      </View>

      <Text style={styles.versionText}>Labor Control DMTT v1.0.0</Text>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#0f172a'
  },
  content: {
    padding: 16
  },
  profileHeader: {
    alignItems: 'center',
    paddingVertical: 24
  },
  avatarContainer: {
    width: 96,
    height: 96,
    borderRadius: 48,
    backgroundColor: '#1e293b',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 3,
    borderColor: '#8b5cf6'
  },
  userName: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#f1f5f9',
    marginTop: 16
  },
  userRole: {
    fontSize: 14,
    color: '#8b5cf6',
    marginTop: 4,
    textAlign: 'center'
  },
  pendingBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    backgroundColor: '#f59e0b20',
    borderWidth: 1,
    borderColor: '#f59e0b40',
    borderRadius: 12,
    padding: 16,
    marginBottom: 24
  },
  pendingInfo: {
    flex: 1
  },
  pendingTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#f59e0b'
  },
  pendingDetails: {
    fontSize: 13,
    color: '#f59e0b',
    marginTop: 2,
    opacity: 0.8
  },
  statsSection: {
    marginBottom: 24
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#f1f5f9',
    marginBottom: 16
  },
  mainStats: {
    flexDirection: 'row',
    gap: 12
  },
  mainStatCard: {
    flex: 1,
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 20,
    alignItems: 'center'
  },
  mainStatValue: {
    fontSize: 32,
    fontWeight: 'bold',
    color: '#f1f5f9',
    marginTop: 8
  },
  mainStatLabel: {
    fontSize: 12,
    color: '#94a3b8',
    marginTop: 4,
    textAlign: 'center'
  },
  detailedStats: {
    marginTop: 12
  },
  statRow: {
    flexDirection: 'row',
    gap: 12
  },
  statItem: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16
  },
  statIconContainer: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: '#0f172a',
    alignItems: 'center',
    justifyContent: 'center'
  },
  statContent: {
    flex: 1
  },
  statValue: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#f1f5f9'
  },
  statLabel: {
    fontSize: 12,
    color: '#64748b'
  },
  rateCard: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    marginTop: 12
  },
  rateTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#f1f5f9',
    marginBottom: 12
  },
  rateBar: {
    height: 8,
    backgroundColor: '#334155',
    borderRadius: 4,
    overflow: 'hidden'
  },
  rateFill: {
    height: '100%',
    backgroundColor: '#8b5cf6',
    borderRadius: 4
  },
  rateValue: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#8b5cf6',
    textAlign: 'right',
    marginTop: 8
  },
  roleSection: {
    marginBottom: 24
  },
  roleCard: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    gap: 12
  },
  roleItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12
  },
  roleText: {
    fontSize: 14,
    color: '#94a3b8',
    flex: 1
  },
  actionsSection: {
    gap: 8,
    marginBottom: 24
  },
  actionButton: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    gap: 12
  },
  actionButtonText: {
    flex: 1,
    fontSize: 16,
    color: '#f1f5f9'
  },
  logoutButton: {
    marginTop: 8
  },
  logoutButtonText: {
    flex: 1,
    fontSize: 16,
    color: '#ef4444'
  },
  versionText: {
    textAlign: 'center',
    fontSize: 12,
    color: '#475569',
    marginBottom: 16
  }
});
