/**
 * Hook de validation NFC version 2 - Architecture chip-based offline
 *
 * Changements majeurs vs V1:
 * - Validation 100% offline via whitelist locale
 * - Clé unique par puce: SHA256(ChipId + MasterKey)
 * - Vérification anti-clonage: lecture bloc 4 avec clé générée
 * - Pas de dépendance au backend pour validation (sauf sync initiale)
 */

import { useState, useCallback } from 'react';
import { Alert } from 'react-native';
import { initNfc, scanAndValidateChip, readLaborControlChip } from '../services/nfc/nfcReader';
import { generateChipSecretKey } from '../services/crypto/rfidCrypto';
import { isChipWhitelisted, WhitelistedChip } from '../services/storage/whitelistService';

/**
 * Résultat de validation d'une puce
 */
export interface ValidationResult {
  isValid: boolean;
  chipId?: string;
  controlPointId?: string;
  controlPointName?: string;
  message: string;
  uid?: string;
  isAuthenticated?: boolean;
}

/**
 * État du scan NFC
 */
export interface NfcScanState {
  isScanning: boolean;
  isNfcSupported: boolean;
  lastResult: ValidationResult | null;
  error: string | null;
}

/**
 * Hook principal de validation NFC v2 (chip-based offline)
 *
 * @returns État et méthodes pour gérer le NFC
 */
export function useNfcValidationV2() {
  const [state, setState] = useState<NfcScanState>({
    isScanning: false,
    isNfcSupported: false,
    lastResult: null,
    error: null,
  });

  /**
   * Initialise le NFC au montage du composant
   */
  const initialize = useCallback(async () => {
    try {
      const supported = await initNfc();
      setState(prev => ({ ...prev, isNfcSupported: supported }));

      if (!supported) {
        Alert.alert(
          'NFC non supporté',
          'Votre appareil ne supporte pas la technologie NFC ou elle est désactivée.',
          [{ text: 'OK' }]
        );
      }
    } catch (error) {
      console.error('[Hook NFC] Erreur initialisation:', error);
      setState(prev => ({
        ...prev,
        isNfcSupported: false,
        error: 'Impossible d\'initialiser le NFC',
      }));
    }
  }, []);

  /**
   * Scanne et valide une puce en mode offline
   *
   * Workflow:
   * 1. Lire bloc 1 → ChipId
   * 2. Générer clé: SHA256(ChipId + MasterKey)
   * 3. Lire bloc 4 avec clé → Vérifier anti-clonage
   * 4. Vérifier whitelist locale
   *
   * @param onSuccess - Callback si validation réussie
   * @param onError - Callback si validation échouée
   */
  const scanChip = useCallback(
    async (
      onSuccess?: (result: ValidationResult) => void,
      onError?: (error: string) => void
    ) => {
      if (!state.isNfcSupported) {
        const errorMsg = 'NFC non supporté sur cet appareil';
        setState(prev => ({ ...prev, error: errorMsg }));
        onError?.(errorMsg);
        return;
      }

      setState(prev => ({ ...prev, isScanning: true, error: null }));

      try {
        console.log('[Hook NFC] 📱 Début du scan...');

        // Scanne et valide la puce en mode offline
        const result = await scanAndValidateChip(
          generateChipSecretKey,
          isChipWhitelisted
        );

        if (result.success && result.chipData && result.whitelistData) {
          // Puce valide et autorisée
          const whitelistChip = result.whitelistData as WhitelistedChip;

          const validationResult: ValidationResult = {
            isValid: true,
            chipId: result.chipData.chipId,
            controlPointId: whitelistChip.controlPointId,
            controlPointName: whitelistChip.controlPointName,
            message: result.message,
            uid: result.chipData.uid,
            isAuthenticated: result.chipData.isAuthenticated,
          };

          setState(prev => ({
            ...prev,
            isScanning: false,
            lastResult: validationResult,
            error: null,
          }));

          console.log('[Hook NFC] ✅ Validation réussie:', validationResult);
          onSuccess?.(validationResult);
        } else {
          // Puce invalide ou non autorisée
          const validationResult: ValidationResult = {
            isValid: false,
            chipId: result.chipData?.chipId,
            message: result.message,
            uid: result.chipData?.uid,
            isAuthenticated: result.chipData?.isAuthenticated,
          };

          setState(prev => ({
            ...prev,
            isScanning: false,
            lastResult: validationResult,
            error: result.message,
          }));

          console.log('[Hook NFC] ❌ Validation échouée:', result.message);
          onError?.(result.message);
        }
      } catch (error) {
        const errorMsg = error instanceof Error ? error.message : 'Erreur inconnue lors du scan';

        setState(prev => ({
          ...prev,
          isScanning: false,
          error: errorMsg,
        }));

        console.error('[Hook NFC] Erreur scan:', error);
        onError?.(errorMsg);
      }
    },
    [state.isNfcSupported]
  );

  /**
   * Lit simplement une puce sans validation whitelist
   * Utile pour l'activation initiale ou le diagnostic
   *
   * @returns Données brutes de la puce
   */
  const readChipRaw = useCallback(async () => {
    if (!state.isNfcSupported) {
      throw new Error('NFC non supporté sur cet appareil');
    }

    setState(prev => ({ ...prev, isScanning: true, error: null }));

    try {
      console.log('[Hook NFC] 📱 Lecture brute de la puce...');

      // Lire bloc 1 uniquement (non protégé)
      const chipData = await readLaborControlChip();

      setState(prev => ({ ...prev, isScanning: false }));

      console.log('[Hook NFC] ✅ Lecture réussie:', chipData);
      return chipData;
    } catch (error) {
      const errorMsg = error instanceof Error ? error.message : 'Erreur lecture puce';

      setState(prev => ({
        ...prev,
        isScanning: false,
        error: errorMsg,
      }));

      console.error('[Hook NFC] Erreur lecture:', error);
      throw error;
    }
  }, [state.isNfcSupported]);

  /**
   * Réinitialise l'état du hook
   */
  const reset = useCallback(() => {
    setState({
      isScanning: false,
      isNfcSupported: state.isNfcSupported,
      lastResult: null,
      error: null,
    });
  }, [state.isNfcSupported]);

  return {
    // État
    isScanning: state.isScanning,
    isNfcSupported: state.isNfcSupported,
    lastResult: state.lastResult,
    error: state.error,

    // Méthodes
    initialize,
    scanChip,
    readChipRaw,
    reset,
  };
}

/**
 * Hook simplifié pour un scan rapide (version raccourcie)
 *
 * @param onSuccess - Callback succès
 * @param onError - Callback erreur
 * @returns Fonction de scan
 */
export function useNfcQuickScan(
  onSuccess?: (result: ValidationResult) => void,
  onError?: (error: string) => void
) {
  const { isNfcSupported, isScanning, scanChip, initialize } = useNfcValidationV2();

  // Auto-init au montage
  useState(() => {
    initialize();
  });

  const quickScan = useCallback(() => {
    scanChip(onSuccess, onError);
  }, [scanChip, onSuccess, onError]);

  return {
    isNfcSupported,
    isScanning,
    quickScan,
  };
}

/**
 * Hook pour scanner avec affichage automatique d'Alert
 *
 * @returns Fonction de scan avec Alert intégré
 */
export function useNfcScanWithAlert() {
  const { initialize, scanChip, isNfcSupported, isScanning } = useNfcValidationV2();

  // Auto-init
  useState(() => {
    initialize();
  });

  const scanWithAlert = useCallback(() => {
    scanChip(
      (result) => {
        // Succès
        Alert.alert(
          '✅ Puce valide',
          `ChipId: ${result.chipId}\n${result.controlPointName ? `Point: ${result.controlPointName}` : ''}`,
          [{ text: 'OK' }]
        );
      },
      (error) => {
        // Erreur
        Alert.alert('❌ Erreur', error, [{ text: 'OK' }]);
      }
    );
  }, [scanChip]);

  return {
    isNfcSupported,
    isScanning,
    scanWithAlert,
  };
}
