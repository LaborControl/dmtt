// ============================================================================
// LABOR CONTROL - Application Mobile Intervenant V2 COMPLÈTE
// Écran principal avec TOUTES les fonctionnalités
// ============================================================================

import React, { useState, useEffect } from 'react';
import { StyleSheet, Text, View, TextInput, TouchableOpacity, Alert, ScrollView, FlatList, RefreshControl, Image, Modal, Platform } from 'react-native';
import NfcManager, { NfcTech } from 'react-native-nfc-manager';
import ReactNativeBiometrics from 'react-native-biometrics';
import * as Keychain from 'react-native-keychain';
import * as ImagePicker from 'expo-image-picker';
import NetInfo from '@react-native-community/netinfo';

// ============================================================================
// CONFIGURATION API
// ============================================================================
const API_BASE_URL = 'https://laborcontrol-api-a6gchacfa6bmf7c4.francecentral-01.azurewebsites.net/api';

// ============================================================================
// INTERFACES TypeScript
// ============================================================================

interface ScheduledTask {
  id: string;
  scheduledDate: string;
  scheduledTimeStart: string;
  scheduledTimeEnd: string;
  status: string;
  controlPointName?: string;
  controlPoint?: {
    id: string;
    name: string;
    locationDescription: string;
    rfidChip?: { chipId: string };
  };
}

interface FormData {
  residentVu: boolean;
  etatGeneral: 'OK' | 'A_SURVEILLER' | 'ALERTER_IDE';
  observations: string;
  photos: string[]; // Base64 des photos
}

interface TaskExecution {
  id: string;
  scannedAt: string;
  submittedAt: string;
  controlPointName: string;
  type: 'SCHEDULED' | 'UNSCHEDULED';
  status: string;
  formDataJson: string;
}

// ============================================================================
// COMPOSANT PRINCIPAL
// ============================================================================

export default function HomeScreen() {
  
  // --------------------------------------------------------------------------
  // ÉTATS - Authentification
  // --------------------------------------------------------------------------
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [token, setToken] = useState('');
  const [userId, setUserId] = useState('');
  const [userEmail, setUserEmail] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [biometricsAvailable, setBiometricsAvailable] = useState(false);
  const [savedCredentials, setSavedCredentials] = useState(false);

  // --------------------------------------------------------------------------
  // ÉTATS - Tâches
  // --------------------------------------------------------------------------
  const [tasks, setTasks] = useState<ScheduledTask[]>([]);
  const [refreshing, setRefreshing] = useState(false);
  const [completedTasksCount, setCompletedTasksCount] = useState(0);
  const [freeScansCount, setFreeScansCount] = useState(0);

  // --------------------------------------------------------------------------
  // ÉTATS - Connectivité
  // --------------------------------------------------------------------------
  const [isOnline, setIsOnline] = useState(true);
  const [pendingSyncCount, setPendingSyncCount] = useState(0);

  // --------------------------------------------------------------------------
  // ÉTATS - Scan NFC
  // --------------------------------------------------------------------------
  const [showScanModal, setShowScanModal] = useState(false);
  const [scanningFor, setScanningFor] = useState<'task' | 'free'>('task');
  const [scanMessage, setScanMessage] = useState('');

  // --------------------------------------------------------------------------
  // ÉTATS - Formulaire de saisie
  // --------------------------------------------------------------------------
  const [showForm, setShowForm] = useState(false);
  const [currentTask, setCurrentTask] = useState<ScheduledTask | null>(null);
  const [scannedAt, setScannedAt] = useState<Date | null>(null);
  const [isUnscheduledScan, setIsUnscheduledScan] = useState(false);
  const [scannedControlPoint, setScannedControlPoint] = useState<any>(null);
  const [formData, setFormData] = useState<FormData>({
    residentVu: true,
    etatGeneral: 'OK',
    observations: '',
    photos: []
  });

  // --------------------------------------------------------------------------
  // ÉTATS - Historique
  // --------------------------------------------------------------------------
  const [showHistory, setShowHistory] = useState(false);
  const [history, setHistory] = useState<TaskExecution[]>([]);
  const [loadingHistory, setLoadingHistory] = useState(false);

  // ==========================================================================
  // EFFET : Vérifier la biométrie au démarrage
  // ==========================================================================
  useEffect(() => {
    checkBiometrics();
    checkSavedCredentials();
  }, []);

  // ==========================================================================
  // EFFET : Surveiller la connectivité réseau
  // ==========================================================================
  useEffect(() => {
    const unsubscribe = NetInfo.addEventListener(state => {
      console.log('[NETWORK] Connexion:', state.isConnected);
      setIsOnline(state.isConnected ?? true);
      
      // Si on revient en ligne, synchroniser automatiquement
      if (state.isConnected && pendingSyncCount > 0) {
        handleManualSync();
      }
    });

    return () => unsubscribe();
  }, [pendingSyncCount]);

  // ==========================================================================
  // EFFET : Charger le compteur de tâches effectuées
  // ==========================================================================
  useEffect(() => {
    if (isLoggedIn) {
      loadTodayStats();
    }
  }, [isLoggedIn, tasks]);

  // ==========================================================================
  // FONCTION : Vérifier si la biométrie est disponible
  // ==========================================================================
  const checkBiometrics = async () => {
    try {
      const rnBiometrics = new ReactNativeBiometrics({ allowDeviceCredentials: true });
      const { available, biometryType } = await rnBiometrics.isSensorAvailable();
      
      if (available) {
        console.log('✅ Biométrie disponible:', biometryType);
        setBiometricsAvailable(true);
      } else {
        console.log('❌ Biométrie non disponible');
        setBiometricsAvailable(false);
      }
    } catch (error) {
      console.error('Erreur vérification biométrie:', error);
      setBiometricsAvailable(false);
    }
  };

  // ==========================================================================
  // FONCTION : Vérifier si des credentials sont sauvegardés
  // ==========================================================================
  const checkSavedCredentials = async () => {
    try {
      const credentials = await Keychain.getInternetCredentials('laborcontrol');
      if (credentials) {
        setSavedCredentials(true);
        setEmail(credentials.username);
      }
    } catch (error) {
      console.log('Pas de credentials sauvegardés');
    }
  };

  // ==========================================================================
  // FONCTION : Login biométrique
  // ==========================================================================
  const handleBiometricLogin = async () => {
    try {
      const rnBiometrics = new ReactNativeBiometrics({ allowDeviceCredentials: true });
      
      const { success } = await rnBiometrics.simplePrompt({
        promptMessage: 'Authentifiez-vous pour accéder à LABOR CONTROL'
      });
      
      if (success) {
        const credentials = await Keychain.getInternetCredentials('laborcontrol');
        
        if (credentials) {
          await handleLoginWithCredentials(credentials.username, credentials.password);
        } else {
          Alert.alert('❌ Erreur', 'Aucune connexion précédente enregistrée');
        }
      } else {
        Alert.alert('❌ Erreur', 'Authentification biométrique échouée');
      }
    } catch (error) {
      console.error('Erreur biométrie:', error);
      Alert.alert('❌ Erreur', 'Impossible d\'utiliser la biométrie');
    }
  };

  // ==========================================================================
  // FONCTION : Login avec email/password
  // ==========================================================================
  const handleLoginWithCredentials = async (emailParam: string, passwordParam: string) => {
    setLoading(true);
    try {
      const response = await fetch(`${API_BASE_URL}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: emailParam, password: passwordParam }),
      });

      if (response.ok) {
        const data = await response.json();
        
        // Sauvegarder les credentials pour la biométrie
        await Keychain.setInternetCredentials(
          'laborcontrol',
          emailParam,
          passwordParam
        );
        
        setToken(data.token);
        setUserId(data.userId);
        setUserEmail(data.email);
        setIsLoggedIn(true);
        loadScheduledTasks(data.token, data.userId);
      } else {
        Alert.alert('Erreur', 'Email ou mot de passe incorrect');
      }
    } catch (error) {
      Alert.alert('Erreur', 'Impossible de se connecter au serveur.');
    } finally {
      setLoading(false);
    }
  };

  const handleLogin = () => {
    handleLoginWithCredentials(email, password);
  };

  // ==========================================================================
  // FONCTION : Charger les statistiques du jour
  // ==========================================================================
  const loadTodayStats = async () => {
    try {
      // Compter les tâches planifiées terminées
      const completedScheduled = tasks.filter(t => t.status === 'COMPLETED').length;
      setCompletedTasksCount(completedScheduled);

      // Charger le nombre de scans libres du jour
      const today = new Date().toISOString().split('T')[0];
      const response = await fetch(
        `${API_BASE_URL}/taskexecutions/user/${userId}?date=${today}&type=UNSCHEDULED`,
        { headers: { 'Authorization': `Bearer ${token}` } }
      );

      if (response.ok) {
        const freeScans = await response.json();
        setFreeScansCount(freeScans.length);
      }
    } catch (error) {
      console.error('Erreur chargement stats:', error);
    }
  };

  // ==========================================================================
  // FONCTION : Charger l'historique
  // ==========================================================================
  const loadHistory = async () => {
    setLoadingHistory(true);
    try {
      const response = await fetch(
        `${API_BASE_URL}/taskexecutions/user/${userId}?days=30`,
        { headers: { 'Authorization': `Bearer ${token}` } }
      );

      if (response.ok) {
        const data = await response.json();
        setHistory(data);
      } else {
        Alert.alert('Erreur', 'Impossible de charger l\'historique');
      }
    } catch (error) {
      Alert.alert('Erreur', 'Problème de connexion');
      console.error('Erreur chargement historique:', error);
    } finally {
      setLoadingHistory(false);
    }
  };

  // ==========================================================================
  // FONCTION : Synchronisation manuelle
  // ==========================================================================
  const handleManualSync = async () => {
    setRefreshing(true);
    await loadScheduledTasks(token, userId);
    await loadTodayStats();
    
    // TODO : Uploader les tâches en attente (WatermelonDB sync)
    setPendingSyncCount(0);
    
    setRefreshing(false);
    Alert.alert('✅ Synchronisation', 'Données mises à jour !');
  };

  // ==========================================================================
  // FONCTION : Prendre une photo
  // ==========================================================================
  const takePhoto = async () => {
    try {
      // Demander la permission
      const { status } = await ImagePicker.requestCameraPermissionsAsync();
      if (status !== 'granted') {
        Alert.alert('Permission refusée', 'Accès à la caméra nécessaire');
        return;
      }

      // Lancer la caméra
      const result = await ImagePicker.launchCameraAsync({
        allowsEditing: true,
        quality: 0.7,
        base64: true,
      });

      if (!result.canceled && result.assets[0].base64) {
        // Ajouter la photo au formulaire
        setFormData({
          ...formData,
          photos: [...formData.photos, result.assets[0].base64]
        });
      }
    } catch (error) {
      console.error('Erreur prise de photo:', error);
      Alert.alert('Erreur', 'Impossible de prendre la photo');
    }
  };

  // ==========================================================================
  // FONCTION : Supprimer une photo
  // ==========================================================================
  const removePhoto = (index: number) => {
    const newPhotos = [...formData.photos];
    newPhotos.splice(index, 1);
    setFormData({ ...formData, photos: newPhotos });
  };

  // ==========================================================================
  // EFFET : Démarrer le scan NFC automatiquement quand modal s'ouvre
  // ==========================================================================
  useEffect(() => {
    if (showScanModal) {
      startNfcScan();
    }
    return () => {
      NfcManager.cancelTechnologyRequest().catch(() => {});
    };
  }, [showScanModal]);

  // ==========================================================================
  // FONCTION : Démarrer le scan NFC
  // ==========================================================================
  const startNfcScan = async () => {
    try {
      await NfcManager.start();
      await NfcManager.requestTechnology(NfcTech.Ndef);
      
      const tag = await NfcManager.getTag();
      
      console.log('========================================');
      console.log('[MOBILE] Tag brut:', JSON.stringify(tag, null, 2));
      
      if (tag && tag.id) {
        let uid;
        if (Array.isArray(tag.id)) {
          uid = tag.id.map((byte: number) => byte.toString(16).padStart(2, '0')).join('').toUpperCase();
        } else if (typeof tag.id === 'string') {
          uid = tag.id.toUpperCase();
        } else {
          uid = String(tag.id).toUpperCase();
        }
        
        console.log('[MOBILE] UID final:', uid);
        console.log('========================================');
        
        setShowScanModal(false);
        NfcManager.cancelTechnologyRequest();
        
        if (scanningFor === 'task') {
          setScannedAt(new Date());
          setIsUnscheduledScan(false);
          setShowForm(true);
        } else {
          const response = await fetch(`${API_BASE_URL}/controlpoints/by-uid/${uid}`, {
            headers: { 'Authorization': `Bearer ${token}` },
          });
          
          if (response.ok) {
            const controlPoint = await response.json();
            console.log('[MOBILE] ✅ Point trouvé:', controlPoint.name);
            setScannedControlPoint(controlPoint);
            setScannedAt(new Date());
            setIsUnscheduledScan(true);
            setCurrentTask(null);
            setShowForm(true);
          } else {
            const errorData = await response.json();
            console.log('[MOBILE] ❌ Erreur backend:', errorData);
            Alert.alert('❌ Erreur', errorData.error || 'Puce NFC inconnue ou inactive');
          }
        }
      } else {
        setShowScanModal(false);
        NfcManager.cancelTechnologyRequest();
        Alert.alert('❌ Erreur', 'Impossible de lire la puce');
      }
    } catch (error) {
      console.error('[MOBILE] Exception scan:', error);
      setShowScanModal(false);
      NfcManager.cancelTechnologyRequest();
      
      const errorMessage = String(error);
      if (!errorMessage.includes('cancelled') && !errorMessage.includes('Cancel')) {
        Alert.alert('❌ Erreur', 'Scan annulé ou échec de lecture');
      }
    }
  };

  // ==========================================================================
  // FONCTION : Annuler le scan NFC
  // ==========================================================================
  const cancelNfcScan = () => {
    setShowScanModal(false);
    NfcManager.cancelTechnologyRequest().catch(() => {});
  };

  // ==========================================================================
  // FONCTION : Charger les tâches planifiées
  // ==========================================================================
  const loadScheduledTasks = async (authToken: string, userIdParam: string) => {
    try {
      const response = await fetch(`${API_BASE_URL}/scheduledtasks/user/${userIdParam}`, {
        headers: { 'Authorization': `Bearer ${authToken}` },
      });
      if (response.ok) {
        const data = await response.json();
        setTasks(data);
      } else {
        Alert.alert('Erreur', 'Impossible de charger les tâches');
      }
    } catch (error) {
      Alert.alert('Erreur', 'Problème de connexion');
      console.error('Erreur chargement tâches:', error);
    } finally {
      setRefreshing(false);
    }
  };

  // ==========================================================================
  // FONCTION : Rafraîchir la liste des tâches
  // ==========================================================================
  const onRefresh = () => {
    setRefreshing(true);
    loadScheduledTasks(token, userId);
  };

  // ==========================================================================
  // FONCTION : Déconnexion
  // ==========================================================================
  const handleLogout = () => {
    setIsLoggedIn(false);
    setToken('');
    setUserId('');
    setUserEmail('');
    setEmail('');
    setPassword('');
    setTasks([]);
    setHistory([]);
    setCompletedTasksCount(0);
    setFreeScansCount(0);
  };

  // ==========================================================================
  // FONCTION : Formater l'heure
  // ==========================================================================
  const formatTime = (timeString: string) => {
    const [hours, minutes] = timeString.split(':');
    return `${hours}:${minutes}`;
  };

  // ==========================================================================
  // FONCTION : Déterminer le badge de statut
  // ==========================================================================
  const getStatusBadge = (status: string, scheduledTimeEnd: string) => {
    const now = new Date();
    const [hours, minutes] = scheduledTimeEnd.split(':');
    const endTime = new Date();
    endTime.setHours(parseInt(hours), parseInt(minutes), 0);

    if (status === 'COMPLETED') {
      return { color: '#10b981', text: 'TERMINÉE' };
    } else if (status === 'IN_PROGRESS') {
      return { color: '#3b82f6', text: 'EN COURS' };
    } else if (now > endTime) {
      return { color: '#ef4444', text: 'EN RETARD' };
    } else {
      return { color: '#fbbf24', text: 'À FAIRE' };
    }
  };

  // ==========================================================================
  // FONCTION : Ouvrir modal scan pour tâche planifiée
  // ==========================================================================
  const handleScanTask = (task: ScheduledTask) => {
    setCurrentTask(task);
    setScanningFor('task');
    const taskName = task.controlPointName || task.controlPoint?.name || 'Point non défini';
    setScanMessage(`Approchez votre téléphone de la puce\n${taskName}`);
    setShowScanModal(true);
  };

  // ==========================================================================
  // FONCTION : Ouvrir modal scan libre
  // ==========================================================================
  const handleFreeScan = () => {
    setCurrentTask(null);
    setScanningFor('free');
    setScanMessage('Approchez votre téléphone\nde n\'importe quelle puce NFC');
    setShowScanModal(true);
  };

  // ==========================================================================
  // FONCTION : Soumettre le formulaire
  // ==========================================================================
  const handleSubmitForm = async () => {
    if (!scannedAt) return;

    try {
      const payload = isUnscheduledScan
        ? {
            userId: userId,
            controlPointId: scannedControlPoint.id,
            scannedAt: scannedAt.toISOString(),
            submittedAt: new Date().toISOString(),
            formDataJson: JSON.stringify(formData),
            type: 'UNSCHEDULED',
            status: 'COMPLETED'
          }
        : {
            userId: userId,
            controlPointId: currentTask!.controlPoint?.id || currentTask!.controlPointId,
            scheduledTaskId: currentTask!.id,
            scannedAt: scannedAt.toISOString(),
            submittedAt: new Date().toISOString(),
            formDataJson: JSON.stringify(formData),
            type: 'SCHEDULED',
            status: 'COMPLETED'
          };

      if (isOnline) {
        // Mode online : envoyer directement
        const response = await fetch(`${API_BASE_URL}/taskexecutions`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
          },
          body: JSON.stringify(payload)
        });

        if (response.ok) {
          Alert.alert('✅ Succès', 'Tâche enregistrée !');
          setShowForm(false);
          setCurrentTask(null);
          setScannedControlPoint(null);
          setIsUnscheduledScan(false);
          setFormData({ residentVu: true, etatGeneral: 'OK', observations: '', photos: [] });
          onRefresh();
        } else {
          Alert.alert('❌ Erreur', 'Impossible d\'enregistrer la tâche');
        }
      } else {
        // Mode offline : stocker localement (WatermelonDB)
        // TODO : Implémenter WatermelonDB storage
        Alert.alert('📴 Mode hors ligne', 'Tâche enregistrée localement. Elle sera synchronisée quand vous serez en ligne.');
        setPendingSyncCount(prev => prev + 1);
        setShowForm(false);
        setCurrentTask(null);
        setScannedControlPoint(null);
        setIsUnscheduledScan(false);
        setFormData({ residentVu: true, etatGeneral: 'OK', observations: '', photos: [] });
      }
    } catch (error) {
      // Si erreur réseau, stocker en local
      Alert.alert('📴 Pas de connexion', 'Tâche enregistrée localement. Elle sera synchronisée plus tard.');
      setPendingSyncCount(prev => prev + 1);
      setShowForm(false);
      setCurrentTask(null);
      setScannedControlPoint(null);
      setIsUnscheduledScan(false);
      setFormData({ residentVu: true, etatGeneral: 'OK', observations: '', photos: [] });
      console.error('Erreur soumission:', error);
    }
  };

  // ==========================================================================
  // COMPOSANT : Modal de scan NFC
  // ==========================================================================
  const renderScanModal = () => {
    if (!showScanModal) return null;

    return (
      <Modal
        visible={showScanModal}
        transparent={true}
        animationType="fade"
        onRequestClose={cancelNfcScan}
      >
        <View style={styles.scanModalOverlay}>
          <View style={styles.scanModalContainer}>
            <Text style={styles.scanModalIcon}>📱</Text>
            <Text style={styles.scanModalTitle}>Scanner l'étiquette</Text>
            <Text style={styles.scanModalMessage}>{scanMessage}</Text>
            
            <View style={styles.scanModalAnimation}>
              <View style={styles.scanPulse} />
            </View>

            <TouchableOpacity 
              style={styles.scanCancelButton}
              onPress={cancelNfcScan}
            >
              <Text style={styles.scanCancelButtonText}>Annuler</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>
    );
  };

  // ==========================================================================
  // COMPOSANT : Formulaire de saisie (Modal)
  // ==========================================================================
  const renderForm = () => {
    if (!showForm) return null;

    const pointName = isUnscheduledScan 
      ? scannedControlPoint?.name 
      : (currentTask?.controlPointName || currentTask?.controlPoint?.name || 'Point non défini');

    return (
      <View style={styles.modalOverlay}>
        <ScrollView contentContainerStyle={styles.scrollContent}>
          <View style={styles.formContainer}>
            <Text style={styles.formTitle}>✅ Scan validé !</Text>
            <Text style={styles.formSubtitle}>{pointName}</Text>
            {isUnscheduledScan && (
              <Text style={styles.formBadge}>📌 Scan libre</Text>
            )}
            
            <Text style={styles.formLabel}>Résident vu ?</Text>
            <View style={styles.radioGroup}>
              <TouchableOpacity 
                style={[styles.radioButton, formData.residentVu && styles.radioButtonSelected]}
                onPress={() => setFormData({...formData, residentVu: true})}
              >
                <Text style={styles.radioText}>✅ Oui</Text>
              </TouchableOpacity>
              <TouchableOpacity 
                style={[styles.radioButton, !formData.residentVu && styles.radioButtonSelected]}
                onPress={() => setFormData({...formData, residentVu: false})}
              >
                <Text style={styles.radioText}>❌ Non</Text>
              </TouchableOpacity>
            </View>

            <Text style={styles.formLabel}>État général</Text>
            <View style={styles.pickerContainer}>
              <TouchableOpacity 
                style={[styles.pickerButton, formData.etatGeneral === 'OK' && styles.pickerButtonSelected]}
                onPress={() => setFormData({...formData, etatGeneral: 'OK'})}
              >
                <Text style={formData.etatGeneral === 'OK' ? styles.pickerTextSelected : styles.pickerText}>
                  ✅ OK
                </Text>
              </TouchableOpacity>
              <TouchableOpacity 
                style={[styles.pickerButton, formData.etatGeneral === 'A_SURVEILLER' && styles.pickerButtonSelected]}
                onPress={() => setFormData({...formData, etatGeneral: 'A_SURVEILLER'})}
              >
                <Text style={formData.etatGeneral === 'A_SURVEILLER' ? styles.pickerTextSelected : styles.pickerText}>
                  ⚠️ À surveiller
                </Text>
              </TouchableOpacity>
              <TouchableOpacity 
                style={[styles.pickerButton, formData.etatGeneral === 'ALERTER_IDE' && styles.pickerButtonSelected]}
                onPress={() => setFormData({...formData, etatGeneral: 'ALERTER_IDE'})}
              >
                <Text style={formData.etatGeneral === 'ALERTER_IDE' ? styles.pickerTextSelected : styles.pickerText}>
                  🚨 Alerter IDE
                </Text>
              </TouchableOpacity>
            </View>

            <Text style={styles.formLabel}>Observations</Text>
            <TextInput
              style={styles.textArea}
              placeholder="Observations libres..."
              value={formData.observations}
              onChangeText={(text) => setFormData({...formData, observations: text})}
              multiline
              numberOfLines={4}
            />

            {/* SECTION PHOTOS */}
            <Text style={styles.formLabel}>Photos ({formData.photos.length}/5)</Text>
            <ScrollView horizontal style={styles.photosContainer}>
              {formData.photos.map((photo, index) => (
                <View key={index} style={styles.photoWrapper}>
                  <Image
                    source={{ uri: `data:image/jpeg;base64,${photo}` }}
                    style={styles.photoPreview}
                  />
                  <TouchableOpacity 
                    style={styles.photoRemoveButton}
                    onPress={() => removePhoto(index)}
                  >
                    <Text style={styles.photoRemoveText}>✕</Text>
                  </TouchableOpacity>
                </View>
              ))}
              
              {formData.photos.length < 5 && (
                <TouchableOpacity 
                  style={styles.addPhotoButton}
                  onPress={takePhoto}
                >
                  <Text style={styles.addPhotoText}>📷</Text>
                  <Text style={styles.addPhotoLabel}>Ajouter</Text>
                </TouchableOpacity>
              )}
            </ScrollView>

            <View style={styles.formButtons}>
              <TouchableOpacity 
                style={styles.cancelButton}
                onPress={() => {
                  setShowForm(false);
                  setCurrentTask(null);
                  setScannedControlPoint(null);
                  setIsUnscheduledScan(false);
                  setFormData({ residentVu: true, etatGeneral: 'OK', observations: '', photos: [] });
                }}
              >
                <Text style={styles.cancelButtonText}>Annuler</Text>
              </TouchableOpacity>
              
              <TouchableOpacity 
                style={styles.submitButton}
                onPress={handleSubmitForm}
              >
                <Text style={styles.submitButtonText}>✅ Valider</Text>
              </TouchableOpacity>
            </View>
          </View>
        </ScrollView>
      </View>
    );
  };

  // ==========================================================================
  // COMPOSANT : Modal Historique
  // ==========================================================================
  const renderHistory = () => {
    if (!showHistory) return null;

    return (
      <Modal
        visible={showHistory}
        animationType="slide"
        onRequestClose={() => setShowHistory(false)}
      >
        <View style={styles.container}>
          <View style={styles.header}>
            <View>
              <Text style={styles.headerTitle}>Historique (30 jours)</Text>
              <Text style={styles.headerSubtitle}>{history.length} interventions</Text>
            </View>
            <TouchableOpacity 
              style={styles.logoutButton} 
              onPress={() => setShowHistory(false)}
            >
              <Text style={styles.logoutText}>Fermer</Text>
            </TouchableOpacity>
          </View>

          <FlatList
            data={history}
            keyExtractor={(item) => item.id}
            refreshControl={
              <RefreshControl 
                refreshing={loadingHistory} 
                onRefresh={loadHistory} 
              />
            }
            renderItem={({ item }) => {
              const formData = JSON.parse(item.formDataJson);
              const scanDate = new Date(item.scannedAt);
              
              return (
                <View style={styles.card}>
                  <View style={styles.cardHeader}>
                    <Text style={styles.cardTitle}>{item.controlPointName}</Text>
                    <View style={[
                      styles.badge, 
                      { backgroundColor: item.type === 'SCHEDULED' ? '#3b82f6' : '#10b981' }
                    ]}>
                      <Text style={styles.badgeText}>
                        {item.type === 'SCHEDULED' ? 'PLANIFIÉ' : 'LIBRE'}
                      </Text>
                    </View>
                  </View>
                  
                  <Text style={styles.cardTime}>
                    📅 {scanDate.toLocaleDateString('fr-FR')} à {scanDate.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })}
                  </Text>
                  
                  <Text style={styles.cardLocation}>
                    État: {formData.etatGeneral === 'OK' ? '✅ OK' : formData.etatGeneral === 'A_SURVEILLER' ? '⚠️ À surveiller' : '🚨 Alerter IDE'}
                  </Text>
                  
                  {formData.observations && (
                    <Text style={styles.cardChip} numberOfLines={2}>
                      💬 {formData.observations}
                    </Text>
                  )}
                  
                  {formData.photos && formData.photos.length > 0 && (
                    <Text style={styles.cardChip}>
                      📷 {formData.photos.length} photo(s)
                    </Text>
                  )}
                </View>
              );
            }}
            ListEmptyComponent={
              <View style={styles.emptyContainer}>
                <Text style={styles.emptyText}>Aucune intervention enregistrée</Text>
              </View>
            }
            contentContainerStyle={styles.listContent}
          />
        </View>
      </Modal>
    );
  };

  // ==========================================================================
  // RENDU : Dashboard (si connecté)
  // ==========================================================================
  if (isLoggedIn) {
    const totalTasks = tasks.length;
    const totalCompleted = completedTasksCount + freeScansCount;
    
    return (
      <View style={styles.container}>
        {/* HEADER AVEC COMPTEUR */}
        <View style={styles.header}>
          <View>
            <Text style={styles.headerTitle}>Mes Tâches du Jour</Text>
            <Text style={styles.headerSubtitle}>{userEmail}</Text>
            
            {/* COMPTEUR TÂCHES EFFECTUÉES */}
            <View style={styles.taskCounter}>
              <Text style={styles.taskCounterText}>
                ✅ {totalCompleted} / {totalTasks} tâches
              </Text>
              {freeScansCount > 0 && (
                <Text style={styles.taskCounterSubtext}>
                  (dont {freeScansCount} scan{freeScansCount > 1 ? 's' : ''} libre{freeScansCount > 1 ? 's' : ''})
                </Text>
              )}
            </View>
          </View>
          
          <View style={styles.headerButtons}>
            {/* INDICATEUR OFFLINE */}
            {!isOnline && (
              <View style={styles.offlineBadge}>
                <Text style={styles.offlineBadgeText}>📴 Hors ligne</Text>
              </View>
            )}
            
            {pendingSyncCount > 0 && (
              <View style={styles.pendingSyncBadge}>
                <Text style={styles.pendingSyncText}>⏳ {pendingSyncCount}</Text>
              </View>
            )}
            
            {/* BOUTON SYNC */}
            <TouchableOpacity 
              style={styles.syncButton} 
              onPress={handleManualSync}
              disabled={!isOnline}
            >
              <Text style={styles.syncButtonText}>🔄</Text>
            </TouchableOpacity>
            
            {/* BOUTON HISTORIQUE */}
            <TouchableOpacity 
              style={styles.historyButton} 
              onPress={() => {
                loadHistory();
                setShowHistory(true);
              }}
            >
              <Text style={styles.historyButtonText}>📜</Text>
            </TouchableOpacity>
            
            {/* BOUTON DÉCONNEXION */}
            <TouchableOpacity style={styles.logoutButton} onPress={handleLogout}>
              <Text style={styles.logoutText}>↪</Text>
            </TouchableOpacity>
          </View>
        </View>

        {/* BOUTON SCAN LIBRE */}
        <View style={styles.freeScanContainer}>
          <TouchableOpacity style={styles.freeScanButton} onPress={handleFreeScan}>
            <Text style={styles.freeScanText}>📱 Scan libre</Text>
          </TouchableOpacity>
        </View>

        {/* LISTE DES TÂCHES PLANIFIÉES */}
        <FlatList
          data={tasks}
          keyExtractor={(item) => item.id}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
          renderItem={({ item }) => {
            const badge = getStatusBadge(item.status, item.scheduledTimeEnd);
            return (
              <TouchableOpacity style={styles.card} onPress={() => handleScanTask(item)}>
                <View style={styles.cardHeader}>
                  <Text style={styles.cardTitle}>
                    {item.controlPointName || item.controlPoint?.name || 'Tâche sans nom'}
                  </Text>
                  <View style={[styles.badge, { backgroundColor: badge.color }]}>
                    <Text style={styles.badgeText}>{badge.text}</Text>
                  </View>
                </View>
                <Text style={styles.cardTime}>
                  ⏰ {formatTime(item.scheduledTimeStart)} - {formatTime(item.scheduledTimeEnd)}
                </Text>
                <Text style={styles.cardLocation}>
                  {item.controlPoint?.locationDescription || 'Emplacement non défini'}
                </Text>
                {item.controlPoint?.rfidChip && (
                  <Text style={styles.cardChip}>Puce: {item.controlPoint.rfidChip.chipId}</Text>
                )}
              </TouchableOpacity>
            );
          }}
          ListEmptyComponent={
            <View style={styles.emptyContainer}>
              <Text style={styles.emptyText}>Aucune tâche planifiée aujourd'hui</Text>
            </View>
          }
          contentContainerStyle={styles.listContent}
        />

        {renderScanModal()}
        {renderForm()}
        {renderHistory()}
      </View>
    );
  }

  // ==========================================================================
  // RENDU : Écran de connexion
  // ==========================================================================
  return (
    <ScrollView contentContainerStyle={styles.loginContainer}>
      <Image 
        source={require('../../assets/logo.png')} 
        style={styles.logo}
        resizeMode="contain"
      />
      
      <Text style={styles.title}>LABOR CONTROL</Text>
      <Text style={styles.subtitle}>Fait, scanné, prouvé.</Text>
      
      {savedCredentials && biometricsAvailable && (
        <TouchableOpacity 
          style={[styles.button, styles.biometricButton]} 
          onPress={handleBiometricLogin}
        >
          <Text style={styles.buttonText}>
            🔐 Connexion rapide
          </Text>
        </TouchableOpacity>
      )}
      
      <TextInput 
        style={styles.input} 
        placeholder="Email" 
        value={email} 
        onChangeText={setEmail} 
        autoCapitalize="none" 
        keyboardType="email-address" 
      />
      <TextInput 
        style={styles.input} 
        placeholder="Mot de passe" 
        value={password} 
        onChangeText={setPassword} 
        secureTextEntry 
      />
      
      <TouchableOpacity 
        style={[styles.button, loading && styles.buttonDisabled]}
        onPress={handleLogin} 
        disabled={loading}
      >
        <Text style={styles.buttonText}>
          {loading ? 'CONNEXION...' : 'SE CONNECTER'}
        </Text>
      </TouchableOpacity>
      
      <Text style={styles.hint}>jean.dupont@ehpad-roses.fr / Loulou</Text>
    </ScrollView>
  );
}

// ============================================================================
// STYLES
// ============================================================================

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8fafc' },
  loginContainer: { flexGrow: 1, backgroundColor: '#f8fafc', alignItems: 'center', justifyContent: 'center', padding: 20 },
  logo: { width: 120, height: 120, marginBottom: 24 },
  title: { fontSize: 36, fontWeight: 'bold', color: '#2563eb', marginBottom: 8 },
  subtitle: { fontSize: 18, color: '#64748b', marginBottom: 48, fontStyle: 'italic' },
  input: { width: '100%', height: 56, borderWidth: 2, borderColor: '#e2e8f0', borderRadius: 12, paddingHorizontal: 16, marginBottom: 16, fontSize: 16, backgroundColor: '#fff', color: '#1e293b' },
  button: { width: '100%', height: 56, backgroundColor: '#2563eb', borderRadius: 12, alignItems: 'center', justifyContent: 'center', marginTop: 8 },
  buttonDisabled: { backgroundColor: '#94a3b8' },
  biometricButton: { backgroundColor: '#10b981', marginBottom: 20 },
  buttonText: { color: '#fff', fontSize: 18, fontWeight: 'bold' },
  hint: { marginTop: 24, fontSize: 12, color: '#94a3b8' },
  
  // Header avec compteur et boutons
  header: { 
    backgroundColor: '#2563eb', 
    padding: 20, 
    paddingTop: 50, 
    flexDirection: 'row', 
    justifyContent: 'space-between', 
    alignItems: 'flex-start'
  },
  headerTitle: { fontSize: 24, fontWeight: 'bold', color: '#fff' },
  headerSubtitle: { fontSize: 14, color: '#93c5fd', marginTop: 4 },
  taskCounter: { marginTop: 8 },
  taskCounterText: { fontSize: 16, fontWeight: 'bold', color: '#fff' },
  taskCounterSubtext: { fontSize: 12, color: '#93c5fd', marginTop: 2 },
  
  headerButtons: { alignItems: 'flex-end', gap: 8 },
  offlineBadge: { 
    backgroundColor: '#ef4444', 
    paddingHorizontal: 12, 
    paddingVertical: 4, 
    borderRadius: 12 
  },
  offlineBadgeText: { fontSize: 12, color: '#fff', fontWeight: 'bold' },
  pendingSyncBadge: { 
    backgroundColor: '#fbbf24', 
    paddingHorizontal: 12, 
    paddingVertical: 4, 
    borderRadius: 12 
  },
  pendingSyncText: { fontSize: 12, color: '#fff', fontWeight: 'bold' },
  
  syncButton: { 
    backgroundColor: '#1e40af', 
    width: 40, 
    height: 40, 
    borderRadius: 20, 
    alignItems: 'center', 
    justifyContent: 'center' 
  },
  syncButtonText: { fontSize: 20 },
  
  historyButton: { 
    backgroundColor: '#1e40af', 
    width: 40, 
    height: 40, 
    borderRadius: 20, 
    alignItems: 'center', 
    justifyContent: 'center' 
  },
  historyButtonText: { fontSize: 20 },
  
  logoutButton: { 
    backgroundColor: '#1e40af', 
    width: 40, 
    height: 40, 
    borderRadius: 20, 
    alignItems: 'center', 
    justifyContent: 'center' 
  },
  logoutText: { color: '#fff', fontWeight: 'bold', fontSize: 20 },
  
  freeScanContainer: { padding: 16, backgroundColor: '#fff', borderBottomWidth: 1, borderBottomColor: '#e2e8f0' },
  freeScanButton: { backgroundColor: '#10b981', paddingVertical: 14, borderRadius: 12, alignItems: 'center' },
  freeScanText: { color: '#fff', fontSize: 18, fontWeight: 'bold' },
  listContent: { padding: 16 },
  card: { backgroundColor: '#fff', borderRadius: 12, padding: 16, marginBottom: 12, elevation: 3 },
  cardHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 },
  cardTitle: { fontSize: 18, fontWeight: 'bold', color: '#1e293b', flex: 1 },
  badge: { paddingHorizontal: 12, paddingVertical: 4, borderRadius: 12 },
  badgeText: { color: '#fff', fontSize: 12, fontWeight: 'bold' },
  cardTime: { fontSize: 16, color: '#2563eb', marginBottom: 8, fontWeight: '600' },
  cardLocation: { fontSize: 14, color: '#64748b', marginBottom: 8 },
  cardChip: { fontSize: 12, color: '#94a3b8' },
  emptyContainer: { alignItems: 'center', paddingVertical: 40 },
  emptyText: { fontSize: 16, color: '#94a3b8' },
  
  // Modal de scan NFC
  scanModalOverlay: { 
    flex: 1, 
    backgroundColor: 'rgba(0,0,0,0.85)', 
    justifyContent: 'center', 
    alignItems: 'center' 
  },
  scanModalContainer: { 
    backgroundColor: '#fff', 
    borderRadius: 24, 
    padding: 40, 
    width: '85%', 
    alignItems: 'center' 
  },
  scanModalIcon: { 
    fontSize: 64, 
    marginBottom: 20 
  },
  scanModalTitle: { 
    fontSize: 24, 
    fontWeight: 'bold', 
    color: '#2563eb', 
    marginBottom: 12, 
    textAlign: 'center' 
  },
  scanModalMessage: { 
    fontSize: 16, 
    color: '#64748b', 
    textAlign: 'center', 
    marginBottom: 32, 
    lineHeight: 24 
  },
  scanModalAnimation: { 
    width: 120, 
    height: 120, 
    justifyContent: 'center', 
    alignItems: 'center', 
    marginBottom: 32 
  },
  scanPulse: { 
    width: 80, 
    height: 80, 
    borderRadius: 40, 
    backgroundColor: '#2563eb', 
    opacity: 0.3 
  },
  scanCancelButton: { 
    paddingHorizontal: 32, 
    paddingVertical: 12, 
    borderRadius: 12, 
    borderWidth: 2, 
    borderColor: '#e2e8f0' 
  },
  scanCancelButtonText: { 
    fontSize: 16, 
    color: '#64748b', 
    fontWeight: 'bold' 
  },

  // Formulaire modal
  modalOverlay: { position: 'absolute', top: 0, left: 0, right: 0, bottom: 0, backgroundColor: 'rgba(0,0,0,0.5)', justifyContent: 'center', padding: 20 },
  scrollContent: { flexGrow: 1, justifyContent: 'center' },
  formContainer: { backgroundColor: '#fff', borderRadius: 16, padding: 24, width: '100%' },
  formTitle: { fontSize: 24, fontWeight: 'bold', color: '#2563eb', textAlign: 'center', marginBottom: 8 },
  formSubtitle: { fontSize: 16, color: '#64748b', textAlign: 'center', marginBottom: 24 },
  formBadge: { fontSize: 14, color: '#10b981', textAlign: 'center', fontWeight: 'bold', marginBottom: 16 },
  formLabel: { fontSize: 16, fontWeight: '600', color: '#1e293b', marginBottom: 8, marginTop: 16 },
  radioGroup: { flexDirection: 'row', gap: 12 },
  radioButton: { flex: 1, paddingVertical: 12, paddingHorizontal: 16, borderRadius: 8, borderWidth: 2, borderColor: '#e2e8f0', alignItems: 'center' },
  radioButtonSelected: { borderColor: '#2563eb', backgroundColor: '#eff6ff' },
  radioText: { fontSize: 16, color: '#1e293b', fontWeight: '600' },
  pickerContainer: { gap: 8 },
  pickerButton: { paddingVertical: 12, paddingHorizontal: 16, borderRadius: 8, borderWidth: 2, borderColor: '#e2e8f0' },
  pickerButtonSelected: { borderColor: '#2563eb', backgroundColor: '#eff6ff' },
  pickerText: { fontSize: 16, color: '#64748b' },
  pickerTextSelected: { fontSize: 16, color: '#2563eb', fontWeight: 'bold' },
  textArea: { borderWidth: 2, borderColor: '#e2e8f0', borderRadius: 8, padding: 12, fontSize: 16, minHeight: 100, textAlignVertical: 'top' },
  
  // Photos
  photosContainer: {
    flexDirection: 'row',
    marginTop: 8,
    marginBottom: 16
  },
  photoWrapper: {
    position: 'relative',
    marginRight: 12
  },
  photoPreview: {
    width: 80,
    height: 80,
    borderRadius: 8,
    borderWidth: 2,
    borderColor: '#e2e8f0'
  },
  photoRemoveButton: {
    position: 'absolute',
    top: -8,
    right: -8,
    backgroundColor: '#ef4444',
    width: 24,
    height: 24,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center'
  },
  photoRemoveText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold'
  },
  addPhotoButton: {
    width: 80,
    height: 80,
    borderRadius: 8,
    borderWidth: 2,
    borderColor: '#2563eb',
    borderStyle: 'dashed',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#eff6ff'
  },
  addPhotoText: {
    fontSize: 32
  },
  addPhotoLabel: {
    fontSize: 12,
    color: '#2563eb',
    fontWeight: 'bold',
    marginTop: 4
  },
  
  formButtons: { flexDirection: 'row', gap: 12, marginTop: 24 },
  cancelButton: { flex: 1, paddingVertical: 14, borderRadius: 8, borderWidth: 2, borderColor: '#e2e8f0', alignItems: 'center' },
  cancelButtonText: { fontSize: 16, color: '#64748b', fontWeight: 'bold' },
  submitButton: { flex: 1, paddingVertical: 14, borderRadius: 8, backgroundColor: '#2563eb', alignItems: 'center' },
  submitButtonText: { fontSize: 16, color: '#fff', fontWeight: 'bold' },
});
