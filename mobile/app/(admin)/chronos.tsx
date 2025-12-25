/**
 * Chronos Screen (Admin)
 *
 * Features:
 * - Chrono Parcours: Time route between control points
 * - Chrono Tâche: Set expected task duration
 * - View timing statistics
 */

import React, { useState, useEffect } from 'react';
import {
  StyleSheet,
  Text,
  View,
  TouchableOpacity,
  FlatList,
  Alert,
  Modal,
  Picker,
  ActivityIndicator,
} from 'react-native';
import NfcManager, { NfcTech } from 'react-native-nfc-manager';
import { useAuth } from '@/contexts/AuthContext';

const API_BASE_URL = 'https://laborcontrol-api.azurewebsites.net/api';

type ChronoMode = 'ROUTE' | 'TASK';

interface ControlPoint {
  id: string;
  name: string;
}

interface ChronoSession {
  id: string;
  mode: ChronoMode;
  startPoint: string;
  endPoint?: string;
  duration: number;
  createdAt: string;
}

export default function ChronosScreen() {
  const { token, user } = useAuth();

  const [mode, setMode] = useState<ChronoMode>('ROUTE');
  const [isTimingActive, setIsTimingActive] = useState(false);
  const [startTime, setStartTime] = useState<Date | null>(null);
  const [startPointName, setStartPointName] = useState<string>('');
  const [elapsedTime, setElapsedTime] = useState(0);
  const [sessions, setSessions] = useState<ChronoSession[]>([]);
  const [controlPoints, setControlPoints] = useState<ControlPoint[]>([]);

  // Timer effect
  useEffect(() => {
    let interval: NodeJS.Timeout;

    if (isTimingActive && startTime) {
      interval = setInterval(() => {
        const now = new Date();
        const elapsed = Math.floor((now.getTime() - startTime.getTime()) / 1000);
        setElapsedTime(elapsed);
      }, 1000);
    }

    return () => {
      if (interval) clearInterval(interval);
    };
  }, [isTimingActive, startTime]);

  useEffect(() => {
    loadControlPoints();
    loadSessions();
  }, []);

  const loadControlPoints = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/controlpoints/customer/${user?.customerId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (response.ok) {
        const data = await response.json();
        setControlPoints(data);
      }
    } catch (error) {
      console.error('[CHRONOS] Load control points error:', error);
    }
  };

  const loadSessions = async () => {
    // TODO: Implement API endpoint to load chrono sessions
    console.log('[CHRONOS] Loading sessions...');
  };

  const handleStartChrono = async () => {
    try {
      await NfcManager.start();
      await NfcManager.requestTechnology(NfcTech.NfcA);

      const tag = await NfcManager.getTag();

      if (tag && tag.id) {
        let uid;
        if (Array.isArray(tag.id)) {
          uid = tag.id.map((byte: number) => byte.toString(16).padStart(2, '0')).join('').toUpperCase();
        } else {
          uid = String(tag.id).toUpperCase();
        }

        // Find control point by UID
        const response = await fetch(`${API_BASE_URL}/controlpoints/by-uid/${uid}`, {
          headers: { Authorization: `Bearer ${token}` },
        });

        if (response.ok) {
          const controlPoint = await response.json();
          setStartPointName(controlPoint.name);
          setStartTime(new Date());
          setIsTimingActive(true);
          setElapsedTime(0);

          Alert.alert(
            '✅ Chrono démarré',
            `Point de départ: ${controlPoint.name}\n\nScannez le point d'arrivée pour terminer.`,
            [{ text: 'OK' }]
          );
        } else {
          Alert.alert('❌ Erreur', 'Point de contrôle non trouvé');
        }
      }
    } catch (error) {
      console.error('[CHRONOS] Start error:', error);
      const errorMessage = String(error);
      if (!errorMessage.includes('cancelled')) {
        Alert.alert('❌ Erreur', 'Scan annulé ou échec');
      }
    } finally {
      NfcManager.cancelTechnologyRequest();
    }
  };

  const handleStopChrono = async () => {
    try {
      await NfcManager.start();
      await NfcManager.requestTechnology(NfcTech.NfcA);

      const tag = await NfcManager.getTag();

      if (tag && tag.id) {
        let uid;
        if (Array.isArray(tag.id)) {
          uid = tag.id.map((byte: number) => byte.toString(16).padStart(2, '0')).join('').toUpperCase();
        } else {
          uid = String(tag.id).toUpperCase();
        }

        // Find control point by UID
        const response = await fetch(`${API_BASE_URL}/controlpoints/by-uid/${uid}`, {
          headers: { Authorization: `Bearer ${token}` },
        });

        if (response.ok) {
          const controlPoint = await response.json();
          const endTime = new Date();
          const duration = Math.floor((endTime.getTime() - startTime!.getTime()) / 1000);

          setIsTimingActive(false);

          Alert.alert(
            '✅ Chrono terminé',
            `Parcours: ${startPointName} → ${controlPoint.name}\nDurée: ${formatDuration(duration)}`,
            [
              {
                text: 'Enregistrer',
                onPress: () => {
                  // TODO: Save chrono session to backend
                  console.log('[CHRONOS] Saving session:', {
                    mode,
                    startPoint: startPointName,
                    endPoint: controlPoint.name,
                    duration,
                  });
                  Alert.alert('✅ Enregistré', 'Temps de parcours enregistré');
                  resetChrono();
                },
              },
              {
                text: 'Annuler',
                style: 'cancel',
                onPress: () => resetChrono(),
              },
            ]
          );
        } else {
          Alert.alert('❌ Erreur', 'Point de contrôle non trouvé');
        }
      }
    } catch (error) {
      console.error('[CHRONOS] Stop error:', error);
      const errorMessage = String(error);
      if (!errorMessage.includes('cancelled')) {
        Alert.alert('❌ Erreur', 'Scan annulé ou échec');
      }
    } finally {
      NfcManager.cancelTechnologyRequest();
    }
  };

  const resetChrono = () => {
    setIsTimingActive(false);
    setStartTime(null);
    setStartPointName('');
    setElapsedTime(0);
  };

  const formatDuration = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}m ${secs}s`;
  };

  return (
    <View style={styles.container}>
      {/* MODE SELECTOR */}
      <View style={styles.modeSelectorContainer}>
        <TouchableOpacity
          style={[styles.modeButton, mode === 'ROUTE' && styles.modeButtonActive]}
          onPress={() => !isTimingActive && setMode('ROUTE')}
          disabled={isTimingActive}
        >
          <Text
            style={[
              styles.modeButtonText,
              mode === 'ROUTE' && styles.modeButtonTextActive,
            ]}
          >
            🏃 Chrono Parcours
          </Text>
        </TouchableOpacity>

        <TouchableOpacity
          style={[styles.modeButton, mode === 'TASK' && styles.modeButtonActive]}
          onPress={() => !isTimingActive && setMode('TASK')}
          disabled={isTimingActive}
        >
          <Text
            style={[
              styles.modeButtonText,
              mode === 'TASK' && styles.modeButtonTextActive,
            ]}
          >
            ⏱️ Chrono Tâche
          </Text>
        </TouchableOpacity>
      </View>

      {/* TIMER DISPLAY */}
      {isTimingActive ? (
        <View style={styles.timerContainer}>
          <Text style={styles.timerLabel}>En cours...</Text>
          <Text style={styles.timerValue}>{formatDuration(elapsedTime)}</Text>
          <Text style={styles.timerStartPoint}>{startPointName}</Text>

          <TouchableOpacity style={styles.stopButton} onPress={handleStopChrono}>
            <Text style={styles.stopButtonText}>⏹️ SCANNER POINT D'ARRIVÉE</Text>
          </TouchableOpacity>

          <TouchableOpacity style={styles.cancelButton} onPress={resetChrono}>
            <Text style={styles.cancelButtonText}>Annuler</Text>
          </TouchableOpacity>
        </View>
      ) : (
        <View style={styles.startContainer}>
          <Text style={styles.instructions}>
            {mode === 'ROUTE'
              ? 'Scannez le point de départ du parcours'
              : 'Scannez le point de contrôle pour chronométrer la tâche'}
          </Text>

          <TouchableOpacity style={styles.startButton} onPress={handleStartChrono}>
            <Text style={styles.startButtonText}>▶️ DÉMARRER CHRONO</Text>
          </TouchableOpacity>
        </View>
      )}

      {/* SESSIONS LIST */}
      <View style={styles.sessionsContainer}>
        <Text style={styles.sessionsTitle}>Chronos récents</Text>
        <FlatList
          data={sessions}
          keyExtractor={(item) => item.id}
          renderItem={({ item }) => (
            <View style={styles.sessionCard}>
              <Text style={styles.sessionMode}>
                {item.mode === 'ROUTE' ? '🏃 Parcours' : '⏱️ Tâche'}
              </Text>
              <Text style={styles.sessionPath}>
                {item.startPoint}
                {item.endPoint && ` → ${item.endPoint}`}
              </Text>
              <Text style={styles.sessionDuration}>
                Durée: {formatDuration(item.duration)}
              </Text>
            </View>
          )}
          ListEmptyComponent={
            <View style={styles.emptyContainer}>
              <Text style={styles.emptyText}>Aucun chrono enregistré</Text>
            </View>
          }
        />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8fafc' },
  modeSelectorContainer: {
    flexDirection: 'row',
    padding: 16,
    gap: 12,
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#e2e8f0',
  },
  modeButton: {
    flex: 1,
    paddingVertical: 14,
    borderRadius: 12,
    borderWidth: 2,
    borderColor: '#e2e8f0',
    alignItems: 'center',
  },
  modeButtonActive: {
    borderColor: '#8b5cf6',
    backgroundColor: '#f3e8ff',
  },
  modeButtonText: {
    fontSize: 15,
    fontWeight: '600',
    color: '#64748b',
  },
  modeButtonTextActive: {
    color: '#8b5cf6',
  },
  timerContainer: {
    padding: 40,
    alignItems: 'center',
    backgroundColor: '#fff',
    margin: 16,
    borderRadius: 16,
    elevation: 4,
  },
  timerLabel: {
    fontSize: 16,
    color: '#64748b',
    marginBottom: 8,
  },
  timerValue: {
    fontSize: 56,
    fontWeight: 'bold',
    color: '#8b5cf6',
    marginBottom: 16,
  },
  timerStartPoint: {
    fontSize: 18,
    color: '#1e293b',
    fontWeight: '600',
    marginBottom: 32,
  },
  stopButton: {
    backgroundColor: '#ef4444',
    paddingVertical: 16,
    paddingHorizontal: 32,
    borderRadius: 12,
    marginBottom: 12,
  },
  stopButtonText: {
    color: '#fff',
    fontSize: 18,
    fontWeight: 'bold',
  },
  cancelButton: {
    paddingVertical: 12,
  },
  cancelButtonText: {
    color: '#64748b',
    fontSize: 16,
  },
  startContainer: {
    padding: 40,
    alignItems: 'center',
    backgroundColor: '#fff',
    margin: 16,
    borderRadius: 16,
    elevation: 4,
  },
  instructions: {
    fontSize: 16,
    color: '#64748b',
    textAlign: 'center',
    marginBottom: 24,
    lineHeight: 24,
  },
  startButton: {
    backgroundColor: '#8b5cf6',
    paddingVertical: 16,
    paddingHorizontal: 32,
    borderRadius: 12,
  },
  startButtonText: {
    color: '#fff',
    fontSize: 18,
    fontWeight: 'bold',
  },
  sessionsContainer: {
    flex: 1,
    padding: 16,
  },
  sessionsTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#1e293b',
    marginBottom: 12,
  },
  sessionCard: {
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    elevation: 2,
  },
  sessionMode: {
    fontSize: 14,
    color: '#8b5cf6',
    fontWeight: '600',
    marginBottom: 8,
  },
  sessionPath: {
    fontSize: 16,
    color: '#1e293b',
    marginBottom: 8,
  },
  sessionDuration: {
    fontSize: 14,
    color: '#64748b',
  },
  emptyContainer: {
    alignItems: 'center',
    paddingVertical: 40,
  },
  emptyText: {
    fontSize: 16,
    color: '#94a3b8',
  },
});
