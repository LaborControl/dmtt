/**
 * NDT Controller - Profile Screen
 *
 * Controller profile with NDT qualifications and statistics
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

interface ControllerStats {
  totalControls: number;
  passedControls: number;
  failedControls: number;
  monthlyControls: number;
  vtCount: number;
  ptCount: number;
  mtCount: number;
  rtCount: number;
  utCount: number;
}

interface NDTQualification {
  id: string;
  controlType: string;
  certificationLevel: number;
  standard: string;
  expirationDate: string;
  status: string;
}

export default function NDTProfileScreen() {
  const { user, token, logout } = useAuth();
  const [stats, setStats] = useState<ControllerStats | null>(null);
  const [qualifications, setQualifications] = useState<NDTQualification[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const fetchData = useCallback(async () => {
    try {
      const [statsRes, qualsRes] = await Promise.all([
        apiClient.get('/ndt-controls/my-statistics', {
          headers: { Authorization: `Bearer ${token}` }
        }),
        apiClient.get('/ndt-qualifications/my-qualifications', {
          headers: { Authorization: `Bearer ${token}` }
        })
      ]);
      setStats(statsRes.data);
      setQualifications(qualsRes.data);
    } catch (error) {
      console.error('Error fetching profile data:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [token]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

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

  const successRate = stats && stats.totalControls > 0
    ? Math.round((stats.passedControls / stats.totalControls) * 100)
    : 0;

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
      refreshControl={
        <RefreshControl
          refreshing={refreshing}
          onRefresh={() => {
            setRefreshing(true);
            fetchData();
          }}
          tintColor="#3b82f6"
        />
      }
    >
      {/* Profile Header */}
      <View style={styles.profileHeader}>
        <View style={styles.avatarContainer}>
          <Ionicons name="person" size={48} color="#3b82f6" />
        </View>
        <Text style={styles.userName}>{user?.name || 'Contrôleur CND'}</Text>
        <Text style={styles.userRole}>Contrôleur CND</Text>
      </View>

      {/* Statistics */}
      {stats && (
        <View style={styles.statsSection}>
          <Text style={styles.sectionTitle}>Mes Statistiques</Text>

          <View style={styles.mainStats}>
            <View style={styles.mainStatCard}>
              <Text style={styles.mainStatValue}>{stats.totalControls}</Text>
              <Text style={styles.mainStatLabel}>Contrôles effectués</Text>
            </View>
            <View style={styles.mainStatCard}>
              <Text style={[styles.mainStatValue, { color: '#3b82f6' }]}>{successRate}%</Text>
              <Text style={styles.mainStatLabel}>Taux conformité</Text>
            </View>
          </View>

          <View style={styles.controlTypeStats}>
            <Text style={styles.subSectionTitle}>Répartition par type</Text>
            <View style={styles.typeGrid}>
              <View style={styles.typeStatItem}>
                <Ionicons name="eye" size={20} color="#22c55e" />
                <Text style={styles.typeStatValue}>{stats.vtCount}</Text>
                <Text style={styles.typeStatLabel}>VT</Text>
              </View>
              <View style={styles.typeStatItem}>
                <Ionicons name="water" size={20} color="#f59e0b" />
                <Text style={styles.typeStatValue}>{stats.ptCount}</Text>
                <Text style={styles.typeStatLabel}>PT</Text>
              </View>
              <View style={styles.typeStatItem}>
                <Ionicons name="magnet" size={20} color="#8b5cf6" />
                <Text style={styles.typeStatValue}>{stats.mtCount}</Text>
                <Text style={styles.typeStatLabel}>MT</Text>
              </View>
              <View style={styles.typeStatItem}>
                <Ionicons name="radio" size={20} color="#ef4444" />
                <Text style={styles.typeStatValue}>{stats.rtCount}</Text>
                <Text style={styles.typeStatLabel}>RT</Text>
              </View>
              <View style={styles.typeStatItem}>
                <Ionicons name="pulse" size={20} color="#3b82f6" />
                <Text style={styles.typeStatValue}>{stats.utCount}</Text>
                <Text style={styles.typeStatLabel}>UT</Text>
              </View>
            </View>
          </View>
        </View>
      )}

      {/* NDT Qualifications */}
      <View style={styles.qualificationsSection}>
        <Text style={styles.sectionTitle}>Mes Qualifications CND</Text>

        {qualifications.length > 0 ? (
          qualifications.map((qual) => (
            <View key={qual.id} style={styles.qualificationCard}>
              <View style={styles.qualHeader}>
                <Text style={styles.qualType}>{qual.controlType}</Text>
                <View style={styles.levelBadge}>
                  <Text style={styles.levelText}>Niveau {qual.certificationLevel}</Text>
                </View>
              </View>
              <Text style={styles.qualStandard}>{qual.standard}</Text>
              <View style={styles.qualFooter}>
                <Ionicons
                  name="calendar"
                  size={14}
                  color={qual.status === 'VALID' ? '#22c55e' : '#f59e0b'}
                />
                <Text style={[
                  styles.qualExpiry,
                  qual.status !== 'VALID' && styles.qualExpiryWarning
                ]}>
                  Expire: {new Date(qual.expirationDate).toLocaleDateString('fr-FR')}
                </Text>
              </View>
            </View>
          ))
        ) : (
          <View style={styles.noQualifications}>
            <Ionicons name="document-text-outline" size={48} color="#64748b" />
            <Text style={styles.noQualText}>Aucune qualification enregistrée</Text>
          </View>
        )}
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
    borderColor: '#3b82f6'
  },
  userName: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#f1f5f9',
    marginTop: 16
  },
  userRole: {
    fontSize: 16,
    color: '#3b82f6',
    marginTop: 4
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
    color: '#f1f5f9'
  },
  mainStatLabel: {
    fontSize: 12,
    color: '#94a3b8',
    marginTop: 4,
    textAlign: 'center'
  },
  controlTypeStats: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    marginTop: 12
  },
  subSectionTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#94a3b8',
    marginBottom: 12
  },
  typeGrid: {
    flexDirection: 'row',
    justifyContent: 'space-between'
  },
  typeStatItem: {
    alignItems: 'center',
    flex: 1
  },
  typeStatValue: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#f1f5f9',
    marginTop: 4
  },
  typeStatLabel: {
    fontSize: 12,
    color: '#64748b',
    marginTop: 2
  },
  qualificationsSection: {
    marginBottom: 24
  },
  qualificationCard: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    marginBottom: 8
  },
  qualHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8
  },
  qualType: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#f1f5f9'
  },
  levelBadge: {
    backgroundColor: '#3b82f6',
    paddingVertical: 4,
    paddingHorizontal: 10,
    borderRadius: 12
  },
  levelText: {
    fontSize: 12,
    fontWeight: '600',
    color: '#fff'
  },
  qualStandard: {
    fontSize: 14,
    color: '#94a3b8'
  },
  qualFooter: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    marginTop: 8
  },
  qualExpiry: {
    fontSize: 13,
    color: '#22c55e'
  },
  qualExpiryWarning: {
    color: '#f59e0b'
  },
  noQualifications: {
    alignItems: 'center',
    padding: 32,
    backgroundColor: '#1e293b',
    borderRadius: 12
  },
  noQualText: {
    fontSize: 14,
    color: '#64748b',
    marginTop: 12
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
