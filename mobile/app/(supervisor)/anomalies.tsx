/**
 * Anomalies Screen (Supervisor)
 *
 * Features:
 * - Declare new anomaly
 * - Launch alert
 * - View anomalies history
 * - Add photos and descriptions
 */

import React, { useState, useEffect } from 'react';
import {
  StyleSheet,
  Text,
  View,
  TextInput,
  TouchableOpacity,
  FlatList,
  Alert,
  Modal,
  ScrollView,
  Image,
  ActivityIndicator,
} from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { useAuth } from '@/contexts/AuthContext';

const API_BASE_URL = 'https://laborcontrol-api.azurewebsites.net/api';

type AnomalySeverity = 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL';

interface Anomaly {
  id: string;
  title: string;
  description: string;
  severity: AnomalySeverity;
  photoUrl: string | null;
  createdAt: string;
  resolvedAt: string | null;
  createdByName: string;
}

export default function AnomaliesScreen() {
  const { token, user } = useAuth();

  const [anomalies, setAnomalies] = useState<Anomaly[]>([]);
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);

  // Form state
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [severity, setSeverity] = useState<AnomalySeverity>('MEDIUM');
  const [photo, setPhoto] = useState<string | null>(null);

  useEffect(() => {
    loadAnomalies();
  }, []);

  const loadAnomalies = async () => {
    setRefreshing(true);
    try {
      const response = await fetch(`${API_BASE_URL}/anomalies/customer/${user?.customerId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (response.ok) {
        const data = await response.json();
        setAnomalies(data);
      }
    } catch (error) {
      console.error('[ANOMALIES] Load error:', error);
    } finally {
      setRefreshing(false);
    }
  };

  const takePhoto = async () => {
    try {
      const { status } = await ImagePicker.requestCameraPermissionsAsync();
      if (status !== 'granted') {
        Alert.alert('Permission refusée', 'Accès à la caméra nécessaire');
        return;
      }

      const result = await ImagePicker.launchCameraAsync({
        allowsEditing: true,
        quality: 0.7,
        base64: true,
      });

      if (!result.canceled && result.assets[0].base64) {
        setPhoto(result.assets[0].base64);
      }
    } catch (error) {
      console.error('[ANOMALIES] Photo error:', error);
      Alert.alert('Erreur', 'Impossible de prendre la photo');
    }
  };

  const handleCreateAnomaly = async () => {
    if (!title || !description) {
      Alert.alert('Erreur', 'Veuillez remplir tous les champs');
      return;
    }

    setLoading(true);

    try {
      const payload = {
        customerId: user?.customerId,
        title,
        description,
        severity,
        photoUrl: photo,
        createdById: user?.id,
      };

      const response = await fetch(`${API_BASE_URL}/anomalies`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(payload),
      });

      if (response.ok) {
        // Send webhook to AI (Cyrille) for anomaly analysis
        console.log('[ANOMALIES] Sending webhook to AI for analysis...');

        Alert.alert(
          '✅ Anomalie déclarée',
          severity === 'CRITICAL'
            ? 'Une alerte a été envoyée à l\'équipe de direction.'
            : 'L\'anomalie a été enregistrée et sera traitée.'
        );

        resetForm();
        setShowCreateModal(false);
        loadAnomalies();
      } else {
        Alert.alert('❌ Erreur', 'Impossible de créer l\'anomalie');
      }
    } catch (error) {
      console.error('[ANOMALIES] Create error:', error);
      Alert.alert('❌ Erreur', 'Problème de connexion');
    } finally {
      setLoading(false);
    }
  };

  const resetForm = () => {
    setTitle('');
    setDescription('');
    setSeverity('MEDIUM');
    setPhoto(null);
  };

  const getSeverityConfig = (sev: AnomalySeverity) => {
    switch (sev) {
      case 'LOW':
        return { color: '#10b981', label: 'Faible', icon: 'ℹ️' };
      case 'MEDIUM':
        return { color: '#f59e0b', label: 'Moyenne', icon: '⚠️' };
      case 'HIGH':
        return { color: '#ef4444', label: 'Élevée', icon: '🚨' };
      case 'CRITICAL':
        return { color: '#991b1b', label: 'Critique', icon: '🆘' };
    }
  };

  return (
    <View style={styles.container}>
      <View style={styles.createButtonContainer}>
        <TouchableOpacity
          style={styles.createButton}
          onPress={() => setShowCreateModal(true)}
        >
          <Text style={styles.createButtonText}>➕ Déclarer une anomalie</Text>
        </TouchableOpacity>
      </View>

      <FlatList
        data={anomalies}
        keyExtractor={(item) => item.id}
        refreshing={refreshing}
        onRefresh={loadAnomalies}
        renderItem={({ item }) => {
          const severityConfig = getSeverityConfig(item.severity);

          return (
            <View style={styles.card}>
              <View style={styles.cardHeader}>
                <Text style={styles.cardTitle}>{item.title}</Text>
                <View style={[styles.badge, { backgroundColor: severityConfig.color }]}>
                  <Text style={styles.badgeText}>
                    {severityConfig.icon} {severityConfig.label}
                  </Text>
                </View>
              </View>

              <Text style={styles.cardDescription}>{item.description}</Text>

              {item.photoUrl && (
                <Text style={styles.cardInfo}>📷 Photo attachée</Text>
              )}

              <Text style={styles.cardFooter}>
                Déclarée par {item.createdByName} le{' '}
                {new Date(item.createdAt).toLocaleDateString('fr-FR')}
              </Text>

              {item.resolvedAt && (
                <View style={styles.resolvedBadge}>
                  <Text style={styles.resolvedText}>
                    ✅ Résolue le {new Date(item.resolvedAt).toLocaleDateString('fr-FR')}
                  </Text>
                </View>
              )}
            </View>
          );
        }}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Text style={styles.emptyText}>Aucune anomalie déclarée</Text>
          </View>
        }
        contentContainerStyle={styles.listContent}
      />

      <Modal
        visible={showCreateModal}
        animationType="slide"
        onRequestClose={() => setShowCreateModal(false)}
      >
        <ScrollView style={styles.modalContainer}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>Nouvelle Anomalie</Text>
            <TouchableOpacity onPress={() => setShowCreateModal(false)}>
              <Text style={styles.closeButton}>✕</Text>
            </TouchableOpacity>
          </View>

          <View style={styles.form}>
            <Text style={styles.label}>Titre *</Text>
            <TextInput
              style={styles.input}
              placeholder="Ex: Fuite d'eau au 2ème étage"
              value={title}
              onChangeText={setTitle}
            />

            <Text style={styles.label}>Description *</Text>
            <TextInput
              style={[styles.input, styles.textArea]}
              placeholder="Décrivez l'anomalie en détail..."
              value={description}
              onChangeText={setDescription}
              multiline
              numberOfLines={5}
            />

            <Text style={styles.label}>Gravité</Text>
            <View style={styles.severityContainer}>
              {(['LOW', 'MEDIUM', 'HIGH', 'CRITICAL'] as AnomalySeverity[]).map((sev) => {
                const config = getSeverityConfig(sev);
                return (
                  <TouchableOpacity
                    key={sev}
                    style={[
                      styles.severityButton,
                      severity === sev && {
                        borderColor: config.color,
                        backgroundColor: `${config.color}20`,
                      },
                    ]}
                    onPress={() => setSeverity(sev)}
                  >
                    <Text
                      style={[
                        styles.severityButtonText,
                        severity === sev && { color: config.color },
                      ]}
                    >
                      {config.icon} {config.label}
                    </Text>
                  </TouchableOpacity>
                );
              })}
            </View>

            <Text style={styles.label}>Photo (optionnel)</Text>
            {photo ? (
              <View style={styles.photoWrapper}>
                <Image
                  source={{ uri: `data:image/jpeg;base64,${photo}` }}
                  style={styles.photoPreview}
                />
                <TouchableOpacity
                  style={styles.removePhotoButton}
                  onPress={() => setPhoto(null)}
                >
                  <Text style={styles.removePhotoText}>✕</Text>
                </TouchableOpacity>
              </View>
            ) : (
              <TouchableOpacity style={styles.addPhotoButton} onPress={takePhoto}>
                <Text style={styles.addPhotoIcon}>📷</Text>
                <Text style={styles.addPhotoText}>Prendre une photo</Text>
              </TouchableOpacity>
            )}

            <TouchableOpacity
              style={[styles.submitButton, loading && styles.buttonDisabled]}
              onPress={handleCreateAnomaly}
              disabled={loading}
            >
              {loading ? (
                <ActivityIndicator color="#fff" />
              ) : (
                <Text style={styles.submitButtonText}>✅ Déclarer l'anomalie</Text>
              )}
            </TouchableOpacity>

            <TouchableOpacity
              style={styles.cancelButton}
              onPress={() => {
                resetForm();
                setShowCreateModal(false);
              }}
            >
              <Text style={styles.cancelButtonText}>Annuler</Text>
            </TouchableOpacity>
          </View>
        </ScrollView>
      </Modal>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8fafc' },
  createButtonContainer: {
    padding: 16,
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#e2e8f0',
  },
  createButton: {
    backgroundColor: '#f59e0b',
    paddingVertical: 14,
    borderRadius: 12,
    alignItems: 'center',
  },
  createButtonText: {
    color: '#fff',
    fontSize: 18,
    fontWeight: 'bold',
  },
  listContent: { padding: 16 },
  card: {
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    elevation: 3,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  cardTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#1e293b',
    flex: 1,
  },
  badge: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 12,
  },
  badgeText: {
    color: '#fff',
    fontSize: 11,
    fontWeight: 'bold',
  },
  cardDescription: {
    fontSize: 14,
    color: '#64748b',
    marginBottom: 12,
    lineHeight: 20,
  },
  cardInfo: {
    fontSize: 12,
    color: '#10b981',
    marginBottom: 8,
  },
  cardFooter: {
    fontSize: 12,
    color: '#94a3b8',
    marginTop: 8,
  },
  resolvedBadge: {
    marginTop: 12,
    backgroundColor: '#d1fae5',
    paddingVertical: 8,
    paddingHorizontal: 12,
    borderRadius: 8,
  },
  resolvedText: {
    fontSize: 12,
    color: '#059669',
    fontWeight: '600',
  },
  emptyContainer: {
    alignItems: 'center',
    paddingVertical: 60,
  },
  emptyText: {
    fontSize: 18,
    color: '#94a3b8',
  },

  // Modal
  modalContainer: { flex: 1, backgroundColor: '#f8fafc' },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 20,
    paddingTop: 60,
    backgroundColor: '#f59e0b',
  },
  modalTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#fff',
  },
  closeButton: {
    fontSize: 32,
    color: '#fff',
    fontWeight: 'bold',
  },
  form: { padding: 20 },
  label: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1e293b',
    marginBottom: 8,
    marginTop: 16,
  },
  input: {
    backgroundColor: '#fff',
    borderWidth: 2,
    borderColor: '#e2e8f0',
    borderRadius: 12,
    padding: 14,
    fontSize: 16,
  },
  textArea: {
    minHeight: 120,
    textAlignVertical: 'top',
  },
  severityContainer: {
    gap: 8,
  },
  severityButton: {
    paddingVertical: 14,
    borderRadius: 12,
    borderWidth: 2,
    borderColor: '#e2e8f0',
    alignItems: 'center',
  },
  severityButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#64748b',
  },
  photoWrapper: {
    position: 'relative',
    alignSelf: 'flex-start',
    marginBottom: 16,
  },
  photoPreview: {
    width: 200,
    height: 200,
    borderRadius: 12,
    borderWidth: 2,
    borderColor: '#e2e8f0',
  },
  removePhotoButton: {
    position: 'absolute',
    top: -10,
    right: -10,
    backgroundColor: '#ef4444',
    width: 32,
    height: 32,
    borderRadius: 16,
    alignItems: 'center',
    justifyContent: 'center',
  },
  removePhotoText: {
    color: '#fff',
    fontSize: 20,
    fontWeight: 'bold',
  },
  addPhotoButton: {
    backgroundColor: '#fff',
    borderWidth: 2,
    borderColor: '#f59e0b',
    borderStyle: 'dashed',
    borderRadius: 12,
    padding: 40,
    alignItems: 'center',
    marginBottom: 16,
  },
  addPhotoIcon: {
    fontSize: 48,
    marginBottom: 8,
  },
  addPhotoText: {
    fontSize: 16,
    color: '#f59e0b',
    fontWeight: '600',
  },
  submitButton: {
    backgroundColor: '#f59e0b',
    paddingVertical: 16,
    borderRadius: 12,
    alignItems: 'center',
    marginTop: 24,
  },
  submitButtonText: {
    color: '#fff',
    fontSize: 18,
    fontWeight: 'bold',
  },
  buttonDisabled: {
    backgroundColor: '#fcd34d',
  },
  cancelButton: {
    paddingVertical: 16,
    borderRadius: 12,
    alignItems: 'center',
    marginTop: 12,
    borderWidth: 2,
    borderColor: '#e2e8f0',
  },
  cancelButtonText: {
    color: '#64748b',
    fontSize: 16,
    fontWeight: '600',
  },
});
