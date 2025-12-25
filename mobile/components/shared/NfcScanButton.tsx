/**
 * NFC Scan Button Component
 *
 * Reusable NFC scanning component with visual feedback
 * Handles NFC tag reading and returns parsed data
 * Supports both callback patterns: onPress and onScan
 */

import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Alert,
  Animated,
  Platform,
  ActivityIndicator
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';

interface NfcTagData {
  weldId?: string;
  materialId?: string;
  assetId?: string;
  tagType?: string;
  rawData?: string;
}

interface NfcScanButtonProps {
  // New callback pattern for parsed data
  onScan?: (tagData: NfcTagData) => void;
  // Legacy callback pattern
  onPress?: () => void;
  buttonText?: string;
  buttonColor?: string;
  disabled?: boolean;
  scanning?: boolean;
  label?: string;
  variant?: 'primary' | 'secondary' | 'large';
}

export default function NfcScanButton({
  onScan,
  onPress,
  buttonText,
  buttonColor = '#3b82f6',
  disabled = false,
  scanning: externalScanning = false,
  label = 'Scanner NFC',
  variant = 'large'
}: NfcScanButtonProps) {
  const [isScanning, setIsScanning] = useState(false);
  const [nfcSupported, setNfcSupported] = useState<boolean | null>(null);
  const pulseAnim = useState(new Animated.Value(1))[0];

  const displayText = buttonText || label;
  const activeScanning = isScanning || externalScanning;

  useEffect(() => {
    checkNfcSupport();
    return () => {
      stopScan();
    };
  }, []);

  useEffect(() => {
    if (activeScanning && variant === 'large') {
      startPulseAnimation();
    } else {
      pulseAnim.setValue(1);
    }
  }, [activeScanning, variant]);

  const checkNfcSupport = async () => {
    try {
      const supported = Platform.OS === 'ios' || Platform.OS === 'android';
      setNfcSupported(supported);
    } catch (error) {
      console.error('Error checking NFC support:', error);
      setNfcSupported(false);
    }
  };

  const startPulseAnimation = () => {
    Animated.loop(
      Animated.sequence([
        Animated.timing(pulseAnim, {
          toValue: 1.1,
          duration: 800,
          useNativeDriver: true
        }),
        Animated.timing(pulseAnim, {
          toValue: 1,
          duration: 800,
          useNativeDriver: true
        })
      ])
    ).start();
  };

  const startScan = async () => {
    if (disabled || activeScanning) return;

    // If using legacy onPress pattern
    if (onPress && !onScan) {
      onPress();
      return;
    }

    setIsScanning(true);

    try {
      await simulateNfcScan();
    } catch (error: any) {
      if (!error.message?.includes('cancelled')) {
        Alert.alert(
          'Erreur NFC',
          'Impossible de lire le tag NFC. Veuillez réessayer.'
        );
      }
    } finally {
      setIsScanning(false);
    }
  };

  const stopScan = () => {
    setIsScanning(false);
  };

  const simulateNfcScan = async (): Promise<void> => {
    await new Promise(resolve => setTimeout(resolve, 2000));

    return new Promise((resolve, reject) => {
      Alert.alert(
        'Simulation NFC',
        'Sélectionnez le type de tag à simuler:',
        [
          {
            text: 'Soudure',
            onPress: () => {
              const mockWeldId = `weld-${Date.now()}`;
              onScan?.({
                weldId: mockWeldId,
                tagType: 'WELD',
                rawData: `DMTT:WELD:${mockWeldId}`
              });
              resolve();
            }
          },
          {
            text: 'Matériau',
            onPress: () => {
              const mockMaterialId = `mat-${Date.now()}`;
              onScan?.({
                materialId: mockMaterialId,
                tagType: 'MATERIAL',
                rawData: `DMTT:MATERIAL:${mockMaterialId}`
              });
              resolve();
            }
          },
          {
            text: 'Équipement',
            onPress: () => {
              const mockAssetId = `asset-${Date.now()}`;
              onScan?.({
                assetId: mockAssetId,
                tagType: 'ASSET',
                rawData: `DMTT:ASSET:${mockAssetId}`
              });
              resolve();
            }
          },
          {
            text: 'Annuler',
            style: 'cancel',
            onPress: () => reject(new Error('cancelled'))
          }
        ]
      );
    });
  };

  if (nfcSupported === false) {
    return (
      <View style={styles.unsupportedContainer}>
        <Ionicons name="warning" size={32} color="#f59e0b" />
        <Text style={styles.unsupportedText}>
          NFC non disponible sur cet appareil
        </Text>
      </View>
    );
  }

  // Large circular button variant (default for new screens)
  if (variant === 'large') {
    return (
      <View style={styles.container}>
        <Animated.View style={[
          styles.scanButtonContainer,
          { transform: [{ scale: pulseAnim }] }
        ]}>
          <TouchableOpacity
            style={[
              styles.scanButton,
              { backgroundColor: buttonColor },
              activeScanning && styles.scanningButton,
              disabled && styles.disabledButton
            ]}
            onPress={activeScanning ? stopScan : startScan}
            disabled={disabled}
            activeOpacity={0.8}
          >
            {activeScanning ? (
              <>
                <View style={styles.scanningIndicator}>
                  <Ionicons name="radio" size={48} color="#fff" />
                </View>
                <Text style={styles.scanningText}>Approchez le tag...</Text>
              </>
            ) : (
              <>
                <Ionicons name="scan" size={48} color="#fff" />
                <Text style={styles.buttonText}>{displayText}</Text>
              </>
            )}
          </TouchableOpacity>
        </Animated.View>

        {activeScanning && (
          <TouchableOpacity style={styles.cancelButton} onPress={stopScan}>
            <Text style={styles.cancelText}>Annuler</Text>
          </TouchableOpacity>
        )}

        <View style={styles.instructions}>
          <Ionicons name="information-circle" size={16} color="#64748b" />
          <Text style={styles.instructionText}>
            Approchez votre téléphone du tag NFC
          </Text>
        </View>
      </View>
    );
  }

  // Primary/Secondary button variants (legacy compatibility)
  const isPrimary = variant === 'primary';

  return (
    <TouchableOpacity
      style={[
        styles.legacyButton,
        isPrimary ? styles.primary : styles.secondary,
        (disabled || activeScanning) && styles.disabled
      ]}
      onPress={startScan}
      disabled={disabled || activeScanning}
      activeOpacity={0.7}
    >
      {activeScanning ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator
            color={isPrimary ? '#fff' : buttonColor}
            size="small"
          />
          <Text
            style={[
              styles.legacyText,
              isPrimary ? styles.primaryText : styles.secondaryText
            ]}
          >
            Scan en cours...
          </Text>
        </View>
      ) : (
        <Text
          style={[
            styles.legacyText,
            isPrimary ? styles.primaryText : styles.secondaryText
          ]}
        >
          📱 {displayText}
        </Text>
      )}
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  // Large variant styles
  container: {
    alignItems: 'center',
    gap: 16
  },
  scanButtonContainer: {},
  scanButton: {
    width: 180,
    height: 180,
    borderRadius: 90,
    alignItems: 'center',
    justifyContent: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
    elevation: 8
  },
  scanningButton: {
    opacity: 0.9
  },
  disabledButton: {
    opacity: 0.5
  },
  buttonText: {
    fontSize: 18,
    fontWeight: '600',
    color: '#fff',
    marginTop: 12
  },
  scanningIndicator: {},
  scanningText: {
    fontSize: 14,
    color: '#fff',
    marginTop: 12,
    opacity: 0.9
  },
  cancelButton: {
    paddingVertical: 10,
    paddingHorizontal: 24
  },
  cancelText: {
    fontSize: 16,
    color: '#ef4444',
    fontWeight: '500'
  },
  instructions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6
  },
  instructionText: {
    fontSize: 13,
    color: '#64748b'
  },
  unsupportedContainer: {
    alignItems: 'center',
    padding: 24,
    backgroundColor: '#1e293b',
    borderRadius: 12
  },
  unsupportedText: {
    fontSize: 14,
    color: '#f59e0b',
    marginTop: 12,
    textAlign: 'center'
  },
  // Legacy variant styles
  legacyButton: {
    paddingVertical: 14,
    paddingHorizontal: 24,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: 48
  },
  primary: {
    backgroundColor: '#2563eb'
  },
  secondary: {
    backgroundColor: '#fff',
    borderWidth: 2,
    borderColor: '#2563eb'
  },
  disabled: {
    opacity: 0.5
  },
  loadingContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8
  },
  legacyText: {
    fontSize: 16,
    fontWeight: '600'
  },
  primaryText: {
    color: '#fff'
  },
  secondaryText: {
    color: '#2563eb'
  }
});
