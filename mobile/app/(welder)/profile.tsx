/**
 * Welder - Profile Screen
 *
 * Displays welder information and statistics
 * Allows logout and settings access
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

interface WelderStats {
  totalWelds: number;
  completedWelds: number;
  inProgressWelds: number;
  pendingWelds: number;
  validQualifications: number;
  expiringQualifications: number;
  monthlyWelds: number;
  successRate: number;
}

interface UserProfile {
  id: string;
  nom: string;
  prenom: string;
  email: string;
  employeeNumber: string | null;
  subcontractorName: string | null;
  role: string;
  createdAt: string;
}

export default function ProfileScreen() {
  const { user, token, logout } = useAuth();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [stats, setStats] = useState<WelderStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const fetchProfileData = useCallback(async () => {
    try {
      const [profileRes, statsRes] = await Promise.all([
        apiClient.get('/users/me', {
          headers: { Authorization: `Bearer ${token}` }
        }),
        apiClient.get('/welds/my-statistics', {
          headers: { Authorization: `Bearer ${token}` }
        })
      ]);
      setProfile(profileRes.data);
      setStats(statsRes.data);
    } catch (error) {
      console.error('Error fetching profile:', error);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [token]);

  useEffect(() => {
    fetchProfileData();
  }, [fetchProfileData]);

  const onRefresh = () => {
    setRefreshing(true);
    fetchProfileData();
  };

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

  const displayName = profile ? `${profile.prenom} ${profile.nom}` : user?.name || 'Soudeur';

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
      refreshControl={
        <RefreshControl
          refreshing={refreshing}
          onRefresh={onRefresh}
          tintColor="#f97316"
        />
      }
    >
      {/* Profile Header */}
      <View style={styles.profileHeader}>
        <View style={styles.avatarContainer}>
          <Ionicons name="person" size={48} color="#f97316" />
        </View>
        <Text style={styles.userName}>{displayName}</Text>
        <Text style={styles.userRole}>Soudeur</Text>
        {profile?.subcontractorName && (
          <View style={styles.subcontractorBadge}>
            <Ionicons name="business" size={14} color="#64748b" />
            <Text style={styles.subcontractorText}>{profile.subcontractorName}</Text>
          </View>
        )}
      </View>

      {/* Statistics Grid */}
      {stats && (
        <View style={styles.statsSection}>
          <Text style={styles.sectionTitle}>Mes Statistiques</Text>

          <View style={styles.statsGrid}>
            <View style={styles.statCard}>
              <Text style={styles.statValue}>{stats.completedWelds}</Text>
              <Text style={styles.statLabel}>Soudures terminées</Text>
            </View>
            <View style={styles.statCard}>
              <Text style={styles.statValue}>{stats.inProgressWelds}</Text>
              <Text style={styles.statLabel}>En cours</Text>
            </View>
            <View style={styles.statCard}>
              <Text style={styles.statValue}>{stats.pendingWelds}</Text>
              <Text style={styles.statLabel}>En attente</Text>
            </View>
            <View style={styles.statCard}>
              <Text style={styles.statValue}>{stats.monthlyWelds}</Text>
              <Text style={styles.statLabel}>Ce mois</Text>
            </View>
          </View>

          {/* Success Rate */}
          <View style={styles.successRateCard}>
            <View style={styles.successRateHeader}>
              <Ionicons name="trending-up" size={20} color="#22c55e" />
              <Text style={styles.successRateTitle}>Taux de réussite CND</Text>
            </View>
            <View style={styles.successRateBar}>
              <View
                style={[
                  styles.successRateFill,
                  { width: `${stats.successRate}%` }
                ]}
              />
            </View>
            <Text style={styles.successRateValue}>{stats.successRate}%</Text>
          </View>

          {/* Qualifications Summary */}
          <View style={styles.qualificationsCard}>
            <View style={styles.qualificationRow}>
              <View style={styles.qualificationItem}>
                <Ionicons name="checkmark-circle" size={20} color="#22c55e" />
                <Text style={styles.qualificationValue}>{stats.validQualifications}</Text>
                <Text style={styles.qualificationLabel}>Qualifications valides</Text>
              </View>
              <View style={styles.qualificationDivider} />
              <View style={styles.qualificationItem}>
                <Ionicons name="warning" size={20} color="#f59e0b" />
                <Text style={styles.qualificationValue}>{stats.expiringQualifications}</Text>
                <Text style={styles.qualificationLabel}>Expirent bientôt</Text>
              </View>
            </View>
          </View>
        </View>
      )}

      {/* Profile Details */}
      <View style={styles.detailsSection}>
        <Text style={styles.sectionTitle}>Informations</Text>

        <View style={styles.detailCard}>
          <View style={styles.detailRow}>
            <Ionicons name="mail" size={20} color="#64748b" />
            <View style={styles.detailContent}>
              <Text style={styles.detailLabel}>Email</Text>
              <Text style={styles.detailValue}>{profile?.email}</Text>
            </View>
          </View>

          {profile?.employeeNumber && (
            <View style={styles.detailRow}>
              <Ionicons name="id-card" size={20} color="#64748b" />
              <View style={styles.detailContent}>
                <Text style={styles.detailLabel}>Matricule</Text>
                <Text style={styles.detailValue}>{profile.employeeNumber}</Text>
              </View>
            </View>
          )}

          <View style={styles.detailRow}>
            <Ionicons name="calendar" size={20} color="#64748b" />
            <View style={styles.detailContent}>
              <Text style={styles.detailLabel}>Membre depuis</Text>
              <Text style={styles.detailValue}>
                {profile?.createdAt
                  ? new Date(profile.createdAt).toLocaleDateString('fr-FR', {
                      year: 'numeric',
                      month: 'long'
                    })
                  : '-'}
              </Text>
            </View>
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

      {/* App Version */}
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
    borderColor: '#f97316'
  },
  userName: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#f1f5f9',
    marginTop: 16
  },
  userRole: {
    fontSize: 16,
    color: '#f97316',
    marginTop: 4
  },
  subcontractorBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    backgroundColor: '#1e293b',
    paddingVertical: 6,
    paddingHorizontal: 12,
    borderRadius: 16,
    marginTop: 12
  },
  subcontractorText: {
    fontSize: 13,
    color: '#94a3b8'
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
  statsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 12
  },
  statCard: {
    width: '47%',
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    alignItems: 'center'
  },
  statValue: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#f97316'
  },
  statLabel: {
    fontSize: 12,
    color: '#94a3b8',
    marginTop: 4,
    textAlign: 'center'
  },
  successRateCard: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    marginTop: 12
  },
  successRateHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginBottom: 12
  },
  successRateTitle: {
    fontSize: 14,
    color: '#f1f5f9',
    fontWeight: '600'
  },
  successRateBar: {
    height: 8,
    backgroundColor: '#334155',
    borderRadius: 4,
    overflow: 'hidden'
  },
  successRateFill: {
    height: '100%',
    backgroundColor: '#22c55e',
    borderRadius: 4
  },
  successRateValue: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#22c55e',
    textAlign: 'right',
    marginTop: 8
  },
  qualificationsCard: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    padding: 16,
    marginTop: 12
  },
  qualificationRow: {
    flexDirection: 'row',
    alignItems: 'center'
  },
  qualificationItem: {
    flex: 1,
    alignItems: 'center'
  },
  qualificationDivider: {
    width: 1,
    height: 40,
    backgroundColor: '#334155'
  },
  qualificationValue: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#f1f5f9',
    marginTop: 8
  },
  qualificationLabel: {
    fontSize: 12,
    color: '#64748b',
    marginTop: 4,
    textAlign: 'center'
  },
  detailsSection: {
    marginBottom: 24
  },
  detailCard: {
    backgroundColor: '#1e293b',
    borderRadius: 12,
    overflow: 'hidden'
  },
  detailRow: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 16,
    gap: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#334155'
  },
  detailContent: {
    flex: 1
  },
  detailLabel: {
    fontSize: 12,
    color: '#64748b'
  },
  detailValue: {
    fontSize: 14,
    color: '#f1f5f9',
    marginTop: 2
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
