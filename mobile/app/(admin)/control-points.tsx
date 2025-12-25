/**
 * Control Points Management Screen (Admin)
 *
 * Features:
 * - Create control points
 * - View control points list
 * - Edit control points
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
  ActivityIndicator,
} from 'react-native';
import { useAuth } from '@/contexts/AuthContext';

const API_BASE_URL = 'https://laborcontrol-api.azurewebsites.net/api';

interface ControlPoint {
  id: string;
  name: string;
  locationDescription: string;
  rfidChipId: string | null;
  createdAt: string;
}

export default function ControlPointsScreen() {
  const { token, user } = useAuth();

  const [controlPoints, setControlPoints] = useState<ControlPoint[]>([]);
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);

  const [name, setName] = useState('');
  const [location, setLocation] = useState('');

  useEffect(() => {
    loadControlPoints();
  }, []);

  const loadControlPoints = async () => {
    setRefreshing(true);
    try {
      const response = await fetch(`${API_BASE_URL}/controlpoints/customer/${user?.customerId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (response.ok) {
        const data = await response.json();
        setControlPoints(data);
      }
    } catch (error) {
      console.error('[CONTROL POINTS] Load error:', error);
      Alert.alert('Erreur', 'Impossible de charger les points de contrôle');
    } finally {
      setRefreshing(false);
    }
  };

  const handleCreate = async () => {
    if (!name || !location) {
      Alert.alert('Erreur', 'Veuillez remplir tous les champs');
      return;
    }

    setLoading(true);

    try {
      const response = await fetch(`${API_BASE_URL}/controlpoints`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          customerId: user?.customerId,
          name,
          locationDescription: location,
        }),
      });

      if (response.ok) {
        Alert.alert('✅ Succès', 'Point de contrôle créé');
        setName('');
        setLocation('');
        setShowCreateModal(false);
        loadControlPoints();
      } else {
        Alert.alert('❌ Erreur', 'Impossible de créer le point de contrôle');
      }
    } catch (error) {
      console.error('[CONTROL POINTS] Create error:', error);
      Alert.alert('❌ Erreur', 'Problème de connexion');
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <View style={styles.createButtonContainer}>
        <TouchableOpacity
          style={styles.createButton}
          onPress={() => setShowCreateModal(true)}
        >
          <Text style={styles.createButtonText}>➕ Créer un point de contrôle</Text>
        </TouchableOpacity>
      </View>

      <FlatList
        data={controlPoints}
        keyExtractor={(item) => item.id}
        refreshing={refreshing}
        onRefresh={loadControlPoints}
        renderItem={({ item }) => (
          <View style={styles.card}>
            <View style={styles.cardHeader}>
              <Text style={styles.cardTitle}>{item.name}</Text>
              {item.rfidChipId && (
                <View style={styles.badge}>
                  <Text style={styles.badgeText}>✅ Puce affectée</Text>
                </View>
              )}
            </View>
            <Text style={styles.cardLocation}>📍 {item.locationDescription}</Text>
            <Text style={styles.cardDate}>
              Créé le {new Date(item.createdAt).toLocaleDateString('fr-FR')}
            </Text>
          </View>
        )}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Text style={styles.emptyText}>Aucun point de contrôle</Text>
          </View>
        }
        contentContainerStyle={styles.listContent}
      />

      <Modal
        visible={showCreateModal}
        animationType="slide"
        onRequestClose={() => setShowCreateModal(false)}
      >
        <View style={styles.modalContainer}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>Nouveau Point</Text>
            <TouchableOpacity onPress={() => setShowCreateModal(false)}>
              <Text style={styles.closeButton}>✕</Text>
            </TouchableOpacity>
          </View>

          <View style={styles.form}>
            <Text style={styles.label}>Nom *</Text>
            <TextInput
              style={styles.input}
              placeholder="Ex: Point A - Entrée"
              value={name}
              onChangeText={setName}
            />

            <Text style={styles.label}>Emplacement *</Text>
            <TextInput
              style={[styles.input, styles.textArea]}
              placeholder="Description de l'emplacement"
              value={location}
              onChangeText={setLocation}
              multiline
              numberOfLines={3}
            />

            <TouchableOpacity
              style={[styles.submitButton, loading && styles.buttonDisabled]}
              onPress={handleCreate}
              disabled={loading}
            >
              {loading ? (
                <ActivityIndicator color="#fff" />
              ) : (
                <Text style={styles.submitButtonText}>✅ Créer</Text>
              )}
            </TouchableOpacity>

            <TouchableOpacity
              style={styles.cancelButton}
              onPress={() => setShowCreateModal(false)}
            >
              <Text style={styles.cancelButtonText}>Annuler</Text>
            </TouchableOpacity>
          </View>
        </View>
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
    backgroundColor: '#8b5cf6',
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
    marginBottom: 8,
  },
  cardTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#1e293b',
    flex: 1,
  },
  badge: {
    backgroundColor: '#10b981',
    paddingHorizontal: 12,
    paddingVertical: 4,
    borderRadius: 12,
  },
  badgeText: {
    color: '#fff',
    fontSize: 11,
    fontWeight: 'bold',
  },
  cardLocation: {
    fontSize: 14,
    color: '#64748b',
    marginBottom: 8,
  },
  cardDate: {
    fontSize: 12,
    color: '#94a3b8',
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
    backgroundColor: '#8b5cf6',
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
    minHeight: 80,
    textAlignVertical: 'top',
  },
  submitButton: {
    backgroundColor: '#8b5cf6',
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
    backgroundColor: '#c4b5fd',
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
