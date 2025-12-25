/**
 * Service de lecture NFC bas niveau pour puces Mifare Classic 1K
 *
 * Architecture Labor Control:
 * - Bloc 1 (secteur 0, non protégé): ChipId en clair
 * - Bloc 4 (secteur 1, protégé): ChipId pour vérification
 * - Bloc 8 (secteur 2, protégé): Checksum anti-clonage
 */

import NfcManager, { NfcTech, Ndef } from 'react-native-nfc-manager';
import { getDefaultKey, hexToBytes, bytesToGuid, bytesToHex } from '../crypto/rfidCrypto';

/**
 * Données lues depuis une puce RFID
 */
export interface NfcChipData {
  uid: string;              // UID physique de la puce
  chipId: string;           // ChipId (depuis bloc 1)
  chipIdVerify?: string;    // ChipId de vérification (depuis bloc 4, optionnel)
  checksum?: string;        // Checksum anti-clonage (depuis bloc 8, optionnel)
  isAuthenticated: boolean; // Authentification secteur protégé réussie?
}

/**
 * Initialise le gestionnaire NFC
 * À appeler au démarrage de l'app
 */
export async function initNfc(): Promise<boolean> {
  try {
    const supported = await NfcManager.isSupported();
    if (!supported) {
      console.log('[NFC] ❌ NFC non supporté sur cet appareil');
      return false;
    }

    await NfcManager.start();
    console.log('[NFC] ✅ Gestionnaire NFC initialisé');
    return true;
  } catch (error) {
    console.error('[NFC] Erreur initialisation:', error);
    return false;
  }
}

/**
 * Lit l'UID d'une puce NFC
 *
 * @returns UID en hexadécimal
 */
export async function readUid(): Promise<string> {
  try {
    // Démarrer la session NFC
    await NfcManager.requestTechnology(NfcTech.NfcA);

    // Récupérer le tag
    const tag = await NfcManager.getTag();

    if (!tag || !tag.id) {
      throw new Error('Impossible de lire l\'UID de la puce');
    }

    // Convertir l'ID en hex
    const uid = bytesToHex(tag.id as unknown as number[]);

    console.log(`[NFC] UID lu: ${uid}`);
    return uid;
  } finally {
    // Toujours fermer la session
    await NfcManager.cancelTechnologyRequest();
  }
}

/**
 * Lit un bloc spécifique d'une puce Mifare Classic
 *
 * @param blockNumber - Numéro du bloc (0-63 pour Mifare 1K)
 * @param key - Clé d'authentification (6 octets en hex ou array)
 * @returns Données du bloc (16 octets)
 */
export async function readBlock(blockNumber: number, key?: string | number[]): Promise<number[]> {
  try {
    // Utiliser la clé par défaut si non fournie
    const authKey = key
      ? typeof key === 'string'
        ? hexToBytes(key)
        : key
      : getDefaultKey();

    // Authentifier le secteur
    const sectorNumber = Math.floor(blockNumber / 4);
    const keyType = 'A'; // Key A

    await NfcManager.mifareClassicAuthenticateA(sectorNumber, authKey);

    // Lire le bloc
    const blockData = await NfcManager.mifareClassicReadBlock(blockNumber);

    console.log(`[NFC] Bloc ${blockNumber} lu: ${bytesToHex(blockData)}`);
    return blockData;
  } catch (error) {
    console.error(`[NFC] Erreur lecture bloc ${blockNumber}:`, error);
    throw error;
  }
}

/**
 * Lit les données complètes d'une puce Labor Control
 * Mode offline: lit bloc 1 (non protégé) + tente bloc 4 et 8 (protégés)
 *
 * @param chipKey - Clé secrète de la puce (optionnel, pour lecture blocs protégés)
 * @returns Données de la puce
 */
export async function readLaborControlChip(chipKey?: string): Promise<NfcChipData> {
  try {
    // Démarrer la session NFC
    await NfcManager.requestTechnology(NfcTech.NfcA);

    // 1. Lire l'UID
    const tag = await NfcManager.getTag();
    if (!tag || !tag.id) {
      throw new Error('Impossible de lire l\'UID de la puce');
    }
    const uid = bytesToHex(tag.id as unknown as number[]);

    // 2. Lire bloc 1 (ChipId en clair, non protégé)
    const bloc1Data = await readBlock(1); // Utilise la clé par défaut
    const chipId = bytesToGuid(bloc1Data);

    console.log(`[NFC] ChipId lu (bloc 1): ${chipId}`);

    // 3. Tenter de lire les blocs protégés (4 et 8) si clé fournie
    let chipIdVerify: string | undefined;
    let checksum: string | undefined;
    let isAuthenticated = false;

    if (chipKey) {
      try {
        // Lire bloc 4 (ChipId protégé)
        const bloc4Data = await readBlock(4, chipKey);
        chipIdVerify = bytesToGuid(bloc4Data);
        console.log(`[NFC] ChipId vérifié (bloc 4): ${chipIdVerify}`);

        // Lire bloc 8 (Checksum protégé)
        const bloc8Data = await readBlock(8, chipKey);
        checksum = bytesToHex(bloc8Data);
        console.log(`[NFC] Checksum lu (bloc 8): ${checksum}`);

        isAuthenticated = true;
      } catch (error) {
        console.warn('[NFC] ⚠️ Impossible de lire les blocs protégés (clé incorrecte ou puce non encodée)');
        isAuthenticated = false;
      }
    }

    return {
      uid,
      chipId,
      chipIdVerify,
      checksum,
      isAuthenticated,
    };
  } finally {
    // Toujours fermer la session
    await NfcManager.cancelTechnologyRequest();
  }
}

/**
 * Scanne et valide une puce Labor Control en mode offline
 * Workflow complet:
 * 1. Lire bloc 1 → ChipId
 * 2. Générer clé unique: SHA256(ChipId + MasterKey)
 * 3. Lire bloc 4 avec clé générée → Vérifier ChipId correspond
 * 4. Vérifier whitelist locale
 *
 * @param generateKeyFn - Fonction de génération de clé (depuis rfidCrypto)
 * @param isWhitelistedFn - Fonction de vérification whitelist
 * @returns Résultat de validation
 */
export async function scanAndValidateChip(
  generateKeyFn: (chipId: string) => Promise<string>,
  isWhitelistedFn: (chipId: string) => Promise<any>
): Promise<{
  success: boolean;
  chipData?: NfcChipData;
  whitelistData?: any;
  message: string;
}> {
  try {
    // 1. Démarrer la session NFC
    await NfcManager.requestTechnology(NfcTech.NfcA);

    // 2. Lire l'UID
    const tag = await NfcManager.getTag();
    if (!tag || !tag.id) {
      throw new Error('Impossible de lire l\'UID de la puce');
    }
    const uid = bytesToHex(tag.id as unknown as number[]);

    // 3. Lire bloc 1 (ChipId en clair)
    const bloc1Data = await readBlock(1);
    const chipId = bytesToGuid(bloc1Data);

    console.log(`[NFC] 📱 Scan puce: ChipId=${chipId}`);

    // 4. Générer la clé secrète unique pour cette puce
    const chipKey = await generateKeyFn(chipId);

    // 5. Lire bloc 4 avec clé générée pour vérification anti-clonage
    let chipIdVerify: string | undefined;
    let isAuthenticated = false;

    try {
      const bloc4Data = await readBlock(4, chipKey);
      chipIdVerify = bytesToGuid(bloc4Data);
      isAuthenticated = true;

      // Vérifier que les ChipId correspondent
      if (chipId !== chipIdVerify) {
        return {
          success: false,
          message: `❌ Puce clonée détectée! ChipId bloc 1 (${chipId}) ≠ bloc 4 (${chipIdVerify})`,
        };
      }

      console.log('[NFC] ✅ Vérification anti-clonage OK');
    } catch (error) {
      return {
        success: false,
        message: '❌ Puce invalide ou non encodée (impossible de lire bloc protégé)',
      };
    }

    // 6. Vérifier la whitelist locale
    const whitelistData = await isWhitelistedFn(chipId);

    if (!whitelistData) {
      return {
        success: false,
        chipData: { uid, chipId, chipIdVerify, isAuthenticated },
        message: '❌ Puce non autorisée pour ce client (pas dans la whitelist)',
      };
    }

    // 7. Tout est OK!
    console.log('[NFC] ✅✅ Puce valide et autorisée!');

    return {
      success: true,
      chipData: { uid, chipId, chipIdVerify, isAuthenticated },
      whitelistData,
      message: '✅ Puce valide et autorisée',
    };
  } catch (error) {
    console.error('[NFC] Erreur scan:', error);
    return {
      success: false,
      message: `❌ Erreur: ${error instanceof Error ? error.message : 'Erreur inconnue'}`,
    };
  } finally {
    // Toujours fermer la session
    await NfcManager.cancelTechnologyRequest();
  }
}

/**
 * Écrit un bloc sur une puce Mifare Classic
 * ATTENTION: Utilisation réservée à l'interface Admin
 *
 * @param blockNumber - Numéro du bloc
 * @param data - Données à écrire (16 octets)
 * @param key - Clé d'authentification
 */
export async function writeBlock(blockNumber: number, data: number[], key?: string | number[]): Promise<void> {
  try {
    if (data.length !== 16) {
      throw new Error('Les données doivent faire exactement 16 octets');
    }

    // Utiliser la clé par défaut si non fournie
    const authKey = key
      ? typeof key === 'string'
        ? hexToBytes(key)
        : key
      : getDefaultKey();

    // Authentifier le secteur
    const sectorNumber = Math.floor(blockNumber / 4);
    await NfcManager.mifareClassicAuthenticateA(sectorNumber, authKey);

    // Écrire le bloc
    await NfcManager.mifareClassicWriteBlock(blockNumber, data);

    console.log(`[NFC] ✅ Bloc ${blockNumber} écrit: ${bytesToHex(data)}`);
  } catch (error) {
    console.error(`[NFC] Erreur écriture bloc ${blockNumber}:`, error);
    throw error;
  }
}

/**
 * Arrête proprement le gestionnaire NFC
 * À appeler lors de la fermeture de l'app
 */
export async function stopNfc(): Promise<void> {
  try {
    await NfcManager.cancelTechnologyRequest();
    console.log('[NFC] Gestionnaire NFC arrêté');
  } catch (error) {
    console.error('[NFC] Erreur arrêt NFC:', error);
  }
}
