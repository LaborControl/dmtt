import { useState, useCallback } from 'react';
import NfcManager, { NfcTech } from 'react-native-nfc-manager';
import axios from 'axios';

/**
 * Hook pour la validation de puces RFID NTAG 213
 * Lit l'UID et le checksum, puis valide via l'API
 */

export interface NfcChipData {
  uid: string;
  checksum: string;
  systemId?: string;
}

export interface ValidationResult {
  isValid: boolean;
  chipId?: string;
  message: string;
  controlPointId?: string;
}

export interface UseNfcValidationReturn {
  isReading: boolean;
  isValidating: boolean;
  error: string | null;
  readChip: () => Promise<NfcChipData | null>;
  validateChip: (uid: string) => Promise<ValidationResult | null>;
  clearError: () => void;
}

export const useNfcValidation = (apiUrl: string, token: string): UseNfcValidationReturn => {
  const [isReading, setIsReading] = useState(false);
  const [isValidating, setIsValidating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  /**
   * Lit une puce NFC NTAG 213
   * Récupère l'UID (pages 0-2) et le checksum (pages 6-7)
   */
  const readChip = useCallback(async (): Promise<NfcChipData | null> => {
    setIsReading(true);
    setError(null);

    try {
      // Initialiser NFC Manager
      await NfcManager.requestTechnology(NfcTech.NfcA);

      // Lire la puce
      const tag = await NfcManager.getTag();

      if (!tag) {
        throw new Error('Impossible de lire la puce');
      }

      // Parser l'UID (pages 0-2)
      // Adapter selon la structure réelle de react-native-nfc-manager
      let uid = '';

      // Essayer différentes propriétés possibles
      if ((tag as any).id) {
        uid = (tag as any).id;
      } else if ((tag as any).nfcId1) {
        uid = ((tag as any).nfcId1 as number[])
          .map((byte: number) => byte.toString(16).padStart(2, '0'))
          .join('')
          .toUpperCase();
      } else if ((tag as any).uid) {
        uid = (tag as any).uid;
      }

      if (!uid) {
        throw new Error('UID non trouvé sur la puce');
      }

      // Parser le checksum (pages 6-7)
      // Note: Adapter selon la structure réelle de la puce
      let checksum = '';
      let systemId = '';

      // Essayer de lire les données NDEF ou brutes
      if ((tag as any).ndefMessage && (tag as any).ndefMessage.length > 0) {
        // Si NDEF disponible, parser les données
        const ndefRecord = (tag as any).ndefMessage[0];
        if (ndefRecord.payload) {
          const payload = String.fromCharCode(...ndefRecord.payload);
          // Extraire checksum et systemId du payload
          const parts = payload.split(':');
          if (parts.length >= 2) {
            systemId = parts[0]; // LC:YYYY-MM-DD
            checksum = parts[1]; // Checksum HMAC
          }
        }
      } else if ((tag as any).pages) {
        // Sinon, essayer de lire les pages brutes
        const pages = (tag as any).pages as number[][];
        if (pages.length > 6) {
          // Pages 6-7 contiennent le checksum
          const checksumBytes = [...(pages[6] || []), ...(pages[7] || [])];
          checksum = String.fromCharCode(...checksumBytes).trim();
        }
      }

      console.log('✅ Puce lue:', { uid, checksum, systemId });

      return {
        uid,
        checksum,
        systemId,
      };
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Erreur lecture puce';
      setError(errorMessage);
      console.error('❌ Erreur lecture NFC:', errorMessage);
      return null;
    } finally {
      try {
        await NfcManager.cancelTechnologyRequest();
      } catch (e) {
        // Ignorer les erreurs de fermeture
      }
      setIsReading(false);
    }
  }, []);

  /**
   * Valide une puce via l'API Backend
   * Appelle POST /api/rfidchips/validate-scan
   */
  const validateChip = useCallback(
    async (uid: string): Promise<ValidationResult | null> => {
      setIsValidating(true);
      setError(null);

      try {
        if (!uid) {
          throw new Error('UID requis pour la validation');
        }

        const response = await axios.post(
          `${apiUrl}/api/rfidchips/validate-scan`,
          { uid },
          {
            headers: {
              Authorization: `Bearer ${token}`,
              'Content-Type': 'application/json',
            },
          }
        );

        const result: ValidationResult = {
          isValid: response.data.isValid,
          chipId: response.data.chipId,
          message: response.data.message,
          controlPointId: response.data.controlPointId,
        };

        if (result.isValid) {
          console.log('✅ Puce valide:', result.chipId);
        } else {
          console.warn('⚠️ Puce invalide:', result.message);
        }

        return result;
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Erreur validation puce';
        setError(errorMessage);
        console.error('❌ Erreur validation:', errorMessage);
        return null;
      } finally {
        setIsValidating(false);
      }
    },
    [apiUrl, token]
  );

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  return {
    isReading,
    isValidating,
    error,
    readChip,
    validateChip,
    clearError,
  };
};

/**
 * Hook composé pour lire et valider en une seule opération
 */
export const useNfcScan = (apiUrl: string, token: string) => {
  const { isReading, isValidating, error, readChip, validateChip, clearError } = useNfcValidation(
    apiUrl,
    token
  );

  const scanAndValidate = useCallback(async (): Promise<ValidationResult | null> => {
    try {
      clearError();

      // 1. Lire la puce
      const chipData = await readChip();
      if (!chipData) {
        return null;
      }

      // 2. Valider via l'API
      const result = await validateChip(chipData.uid);
      return result;
    } catch (err) {
      console.error('❌ Erreur scan complet:', err);
      return null;
    }
  }, [readChip, validateChip, clearError]);

  return {
    isScanning: isReading || isValidating,
    error,
    scanAndValidate,
    clearError,
  };
};
