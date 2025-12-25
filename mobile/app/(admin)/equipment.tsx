/**
 * Equipment Creation Screen (Admin)
 *
 * Features:
 * - Create new equipment
 * - Photo capture (equipment + nameplate)
 * - Equipment list
 * - Photos sent to AI for OCR processing
 */

import React, { useState, useEffect } from 'react';
import {
  StyleSheet,
  Text,
  View,
  TextInput,
  TouchableOpacity,
  FlatList,
  Image,
  Alert,
  Modal,
  ScrollView,
  ActivityIndicator,
} from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { useAuth } from '@/contexts/AuthContext';

// ============================================================================
// TYPES
// ============================================================================

interface Equipment {
  id: string;
  name: string;
  description: string;
  location: string;
  photoUrl: string | null;
  nameplatePhotoUrl: string | null;
  createdAt: string;
}

// ============================================================================
// CONFIGURATION
// ============================================================================

const API_BASE_URL = 'https://laborcontrol-api.azurewebsites.net/api';

// ============================================================================
// COMPONENT
// ============================================================================

export default function EquipmentScreen() {
  const { token, user } = useAuth();

  // --------------------------------------------------------------------------
  // STATE
  // --------------------------------------------------------------------------
  const [equipments, setEquipments] = useState<Equipment[]>([]);
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);

  // Form state
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [location, setLocation] = useState('');
  const [equipmentPhoto, setEquipmentPhoto] = useState<string | null>(null);
  const [nameplatePhoto, setNameplatePhoto] = useState<string | null>(null);

  // --------------------------------------------------------------------------
  // EFFECTS
  // --------------------------------------------------------------------------
  useEffect(() => {
    loadEquipments();
  }, []);

  // --------------------------------------------------------------------------
  // FUNCTION: Load equipments
  // --------------------------------------------------------------------------
  const loadEquipments = async () => {
    setRefreshing(true);
    try {
      const response = await fetch(`${API_BASE_URL}/equipments/customer/${user?.customerId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (response.ok) {
        const data = await response.json();
        setEquipments(data);
      } else {
        Alert.alert('Erreur', 'Impossible de charger les équipements');
      }
    } catch (error) {
      console.error('[EQUIPMENT] Load error:', error);
      Alert.alert('Erreur', 'Problème de connexion');
    } finally {
      setRefreshing(false);
    }
  };

  // --------------------------------------------------------------------------
  // FUNCTION: Take photo
  // --------------------------------------------------------------------------
  const takePhoto = async (type: 'equipment' | 'nameplate') => {
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
        if (type === 'equipment') {
          setEquipmentPhoto(result.assets[0].base64);
        } else {
          setNameplatePhoto(result.assets[0].base64);
          // TODO: Send nameplate photo to AI (Jean-Claude/Cyrille) for OCR
          console.log('[EQUIPMENT] Nameplate photo ready for AI processing');
        }
      }
    } catch (error) {
      console.error('[EQUIPMENT] Photo error:', error);
      Alert.alert('Erreur', 'Impossible de prendre la photo');
    }
  };

  // --------------------------------------------------------------------------
  // FUNCTION: Create equipment
  // --------------------------------------------------------------------------
  const handleCreateEquipment = async () => {
    if (!name || !location) {
      Alert.alert('Erreur', 'Veuillez remplir les champs obligatoires');
      return;
    }

    setLoading(true);

    try {
      const payload = {
        customerId: user?.customerId,
        name,
        description,
        location,
        photoUrl: equipmentPhoto,
        nameplatePhotoUrl: nameplatePhoto,
      };

      const response = await fetch(`${API_BASE_URL}/equipments`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(payload),
      });

      if (response.ok) {
        Alert.alert('✅ Succès', 'Équipement créé avec succès');
        resetForm();
        setShowCreateModal(false);
        loadEquipments();

        // TODO: Send photos to AI webhook
        if (equipmentPhoto || nameplatePhoto) {
          console.log('[EQUIPMENT] Sending photos to AI webhook...');
        }
      } else {
        const errorData = await response.json();
        Alert.alert('❌ Erreur', errorData.error || 'Impossible de créer l\'équipement');
      }
    } catch (error) {
      console.error('[EQUIPMENT] Create error:', error);
      Alert.alert('❌ Erreur', 'Problème de connexion');
    } finally {
      setLoading(false);
    }
  };

  // --------------------------------------------------------------------------
  // FUNCTION: Reset form
  // --------------------------------------------------------------------------
  const resetForm = () => {
    setName('');
    setDescription('');
    setLocation('');
    setEquipmentPhoto(null);
    setNameplatePhoto(null);
  };

  // --------------------------------------------------------------------------
  // RENDER: Create modal
  // --------------------------------------------------------------------------
  const renderCreateModal = () => (
    <Modal
      visible={showCreateModal}
      animationType="slide"
      onRequestClose={() => setShowCreateModal(false)}
    >
      <ScrollView style={styles.modalContainer}>
        <View style={styles.modalHeader}>
          <Text style={styles.modalTitle}>Nouvel Équipement</Text>
          <TouchableOpacity onPress={() => setShowCreateModal(false)}>
            <Text style={styles.closeButton}>✕</Text>
          </TouchableOpacity>
        </View>

        <View style={styles.form}>
          <Text style={styles.label}>Nom *</Text>
          <TextInput
            style={styles.input}
            placeholder="Ex: Ascenseur principal"
            value={name}
            onChangeText={setName}
          />

          <Text style={styles.label}>Description</Text>
          <TextInput
            style={[styles.input, styles.textArea]}
            placeholder="Description de l'équipement"
            value={description}
            onChangeText={setDescription}
            multiline
            numberOfLines={3}
          />

          <Text style={styles.label}>Emplacement *</Text>
          <TextInput
            style={styles.input}
            placeholder="Ex: Bâtiment A, Étage 2"
            value={location}
            onChangeText={setLocation}
          />

          {/* EQUIPMENT PHOTO */}
          <Text style={styles.label}>Photo de l'équipement</Text>
          <View style={styles.photoSection}>
            {equipmentPhoto ? (
              <View style={styles.photoWrapper}>
                <Image
                  source={{ uri: `data:image/jpeg;base64,${equipmentPhoto}` }}
                  style={styles.photoPreview}
                />
                <TouchableOpacity
                  style={styles.removePhotoButton}
                  onPress={() => setEquipmentPhoto(null)}
                >
                  <Text style={styles.removePhotoText}>✕</Text>
                </TouchableOpacity>
              </View>
            ) : (
              <TouchableOpacity
                style={styles.addPhotoButton}
                onPress={() => takePhoto('equipment')}
              >
                <Text style={styles.addPhotoIcon}>📷</Text>
                <Text style={styles.addPhotoText}>Prendre une photo</Text>
              </TouchableOpacity>
            )}
          </View>

          {/* NAMEPLATE PHOTO */}
          <Text style={styles.label}>Photo de la plaque constructeur</Text>
          <Text style={styles.hint}>
            La photo sera analysée par IA pour extraction des données
          </Text>
          <View style={styles.photoSection}>
            {nameplatePhoto ? (
              <View style={styles.photoWrapper}>
                <Image
                  source={{ uri: `data:image/jpeg;base64,${nameplatePhoto}` }}
                  style={styles.photoPreview}
                />
                <TouchableOpacity
                  style={styles.removePhotoButton}
                  onPress={() => setNameplatePhoto(null)}
                >
                  <Text style={styles.removePhotoText}>✕</Text>
                </TouchableOpacity>
              </View>
            ) : (
              <TouchableOpacity
                style={styles.addPhotoButton}
                onPress={() => takePhoto('nameplate')}
              >
                <Text style={styles.addPhotoIcon}>📷</Text>
                <Text style={styles.addPhotoText}>Prendre une photo</Text>
              </TouchableOpacity>
            )}
          </View>

          {/* SUBMIT BUTTON */}
          <TouchableOpacity
            style={[styles.submitButton, loading && styles.buttonDisabled]}
            onPress={handleCreateEquipment}
            disabled={loading}
          >
            {loading ? (
              <ActivityIndicator color="#fff" />
            ) : (
              <Text style={styles.submitButtonText}>✅ Créer l'équipement</Text>
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
  );

  // --------------------------------------------------------------------------
  // RENDER
  // --------------------------------------------------------------------------
  return (
    <View style={styles.container}>
      {/* CREATE BUTTON */}
      <View style={styles.createButtonContainer}>
        <TouchableOpacity
          style={styles.createButton}
          onPress={() => setShowCreateModal(true)}
        >
          <Text style={styles.createButtonText}>➕ Créer un équipement</Text>
        </TouchableOpacity>
      </View>

      {/* EQUIPMENT LIST */}
      <FlatList
        data={equipments}
        keyExtractor={(item) => item.id}
        refreshing={refreshing}
        onRefresh={loadEquipments}
        renderItem={({ item }) => (
          <View style={styles.card}>
            <View style={styles.cardHeader}>
              <Text style={styles.cardTitle}>{item.name}</Text>
            </View>
            {item.description && (
              <Text style={styles.cardDescription}>{item.description}</Text>
            )}
            <Text style={styles.cardLocation}>📍 {item.location}</Text>
            {item.photoUrl && (
              <Text style={styles.cardInfo}>📷 Photo disponible</Text>
            )}
            {item.nameplatePhotoUrl && (
              <Text style={styles.cardInfo}>🏷️ Plaque scannée</Text>
            )}
            <Text style={styles.cardDate}>
              Créé le {new Date(item.createdAt).toLocaleDateString('fr-FR')}
            </Text>
          </View>
        )}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Text style={styles.emptyText}>Aucun équipement</Text>
            <Text style={styles.emptySubtext}>
              Créez votre premier équipement
            </Text>
          </View>
        }
        contentContainerStyle={styles.listContent}
      />

      {renderCreateModal()}
    </View>
  );
}

// ============================================================================
// STYLES
// ============================================================================

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
    marginBottom: 8,
  },
  cardTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#1e293b',
  },
  cardDescription: {
    fontSize: 14,
    color: '#64748b',
    marginBottom: 8,
  },
  cardLocation: {
    fontSize: 14,
    color: '#8b5cf6',
    fontWeight: '600',
    marginBottom: 8,
  },
  cardInfo: {
    fontSize: 12,
    color: '#10b981',
    marginBottom: 4,
  },
  cardDate: {
    fontSize: 12,
    color: '#94a3b8',
    marginTop: 8,
  },
  emptyContainer: {
    alignItems: 'center',
    paddingVertical: 60,
  },
  emptyText: {
    fontSize: 18,
    color: '#94a3b8',
    fontWeight: '600',
  },
  emptySubtext: {
    fontSize: 14,
    color: '#cbd5e1',
    marginTop: 8,
  },

  // Modal styles
  modalContainer: {
    flex: 1,
    backgroundColor: '#f8fafc',
  },
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
  form: {
    padding: 20,
  },
  label: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1e293b',
    marginBottom: 8,
    marginTop: 16,
  },
  hint: {
    fontSize: 12,
    color: '#64748b',
    marginBottom: 8,
    fontStyle: 'italic',
  },
  input: {
    backgroundColor: '#fff',
    borderWidth: 2,
    borderColor: '#e2e8f0',
    borderRadius: 12,
    padding: 14,
    fontSize: 16,
    color: '#1e293b',
  },
  textArea: {
    minHeight: 80,
    textAlignVertical: 'top',
  },
  photoSection: {
    marginBottom: 16,
  },
  photoWrapper: {
    position: 'relative',
    alignSelf: 'flex-start',
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
    borderColor: '#8b5cf6',
    borderStyle: 'dashed',
    borderRadius: 12,
    padding: 40,
    alignItems: 'center',
  },
  addPhotoIcon: {
    fontSize: 48,
    marginBottom: 8,
  },
  addPhotoText: {
    fontSize: 16,
    color: '#8b5cf6',
    fontWeight: '600',
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
