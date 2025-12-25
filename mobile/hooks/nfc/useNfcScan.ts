/**
 * Hook: useNfcScan
 *
 * Generic NFC scanning hook
 * Reusable across all screens that need NFC functionality
 */

import { useState } from 'react';
import { Alert } from 'react-native';
import { scanNfcTag, initNfc, cancelNfcScan } from '@/services/nfc/nfcService';

interface UseNfcScanReturn {
  // State
  scanning: boolean;
  lastScannedUid: string | null;

  // Actions
  scan: () => Promise<{ success: boolean; uid?: string }>;
  cancel: () => Promise<void>;
  reset: () => void;
}

export function useNfcScan(): UseNfcScanReturn {
  const [scanning, setScanning] = useState(false);
  const [lastScannedUid, setLastScannedUid] = useState<string | null>(null);

  // ==========================================================================
  // Start NFC scan
  // ==========================================================================
  const scan = async (): Promise<{ success: boolean; uid?: string }> => {
    if (scanning) {
      console.warn('[useNfcScan] Scan already in progress');
      return { success: false };
    }

    setScanning(true);
    console.log('[useNfcScan] Starting NFC scan...');

    try {
      // Initialize NFC
      const initialized = await initNfc();
      if (!initialized) {
        throw new Error('NFC non disponible sur cet appareil');
      }

      // Scan tag
      const uid = await scanNfcTag();

      console.log('[useNfcScan] ✅ Tag scanned:', uid);
      setLastScannedUid(uid);

      return { success: true, uid };
    } catch (error: any) {
      console.error('[useNfcScan] ❌ Scan failed:', error);

      // Don't show alert for user cancellation
      const errorMessage = String(error.message || error);
      if (!errorMessage.toLowerCase().includes('cancel')) {
        Alert.alert('Erreur NFC', errorMessage);
      }

      return { success: false };
    } finally {
      setScanning(false);
    }
  };

  // ==========================================================================
  // Cancel ongoing scan
  // ==========================================================================
  const cancel = async () => {
    console.log('[useNfcScan] Cancelling scan...');
    await cancelNfcScan();
    setScanning(false);
  };

  // ==========================================================================
  // Reset state
  // ==========================================================================
  const reset = () => {
    console.log('[useNfcScan] Resetting...');
    setScanning(false);
    setLastScannedUid(null);
  };

  return {
    scanning,
    lastScannedUid,
    scan,
    cancel,
    reset
  };
}
