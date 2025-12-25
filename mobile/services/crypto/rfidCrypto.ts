/**
 * Service de cryptographie RFID pour validation chip-based offline
 *
 * Architecture anti-clonage Labor Control:
 * - Chaque puce a une clé unique: SHA256(ChipId + MasterKey)
 * - Validation 100% offline via whitelist locale
 * - Pas de CustomerId sur les puces physiques
 */

import * as Crypto from 'expo-crypto';

/**
 * Configuration de sécurité RFID
 * IMPORTANT: MasterKey doit être la même que côté backend (RfidSecurity:MasterKey)
 */
const RFID_CONFIG = {
  // TODO: Récupérer depuis un stockage sécurisé ou serveur à la connexion
  MASTER_KEY: 'MASTER_KEY_CHANGE_IN_PRODUCTION',
  DEFAULT_KEY: [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF], // Clé par défaut Mifare Classic
};

/**
 * Génère la clé secrète unique pour une puce basée sur son ChipId
 * Algorithme: SHA256(ChipId + MasterKey) -> 6 premiers octets
 *
 * @param chipId - GUID de la puce (format: "550e8400-e29b-41d4-a716-446655440000")
 * @returns Clé Mifare Classic 6 octets en hexadécimal
 *
 * @example
 * const chipId = "550e8400-e29b-41d4-a716-446655440000";
 * const key = await generateChipSecretKey(chipId);
 * // key = "A1B2C3D4E5F6" (exemple)
 */
export async function generateChipSecretKey(chipId: string): Promise<string> {
  try {
    // 1. Construire la matière de la clé
    const keyMaterial = `${chipId}${RFID_CONFIG.MASTER_KEY}`;

    // 2. Calculer SHA256 avec expo-crypto
    const hash = await Crypto.digestStringAsync(
      Crypto.CryptoDigestAlgorithm.SHA256,
      keyMaterial
    );

    // 3. Prendre les 6 premiers octets (12 caractères hex)
    const key = hash.substring(0, 12).toUpperCase();

    return key;
  } catch (error) {
    console.error('[RFID Crypto] Erreur génération clé:', error);
    throw new Error('Impossible de générer la clé de la puce');
  }
}

/**
 * Convertit un GUID en bytes pour écriture NFC
 * Format Mifare: 16 octets
 *
 * @param guid - GUID au format standard
 * @returns Array de 16 octets
 */
export function guidToBytes(guid: string): number[] {
  // Retirer les tirets
  const hex = guid.replace(/-/g, '');

  // Vérifier longueur
  if (hex.length !== 32) {
    throw new Error('GUID invalide: doit contenir 32 caractères hexadécimaux');
  }

  // Convertir en bytes
  const bytes: number[] = [];
  for (let i = 0; i < hex.length; i += 2) {
    bytes.push(parseInt(hex.substring(i, i + 2), 16));
  }

  return bytes;
}

/**
 * Convertit des bytes en GUID
 *
 * @param bytes - Array de 16 octets
 * @returns GUID formaté
 */
export function bytesToGuid(bytes: number[]): string {
  if (bytes.length !== 16) {
    throw new Error('Les bytes doivent contenir exactement 16 octets pour un GUID');
  }

  // Convertir en hex
  const hex = bytes.map(b => b.toString(16).padStart(2, '0')).join('');

  // Formater en GUID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
  return `${hex.substring(0, 8)}-${hex.substring(8, 12)}-${hex.substring(12, 16)}-${hex.substring(16, 20)}-${hex.substring(20, 32)}`;
}

/**
 * Convertit une clé hex en bytes
 *
 * @param hexKey - Clé en hexadécimal (ex: "A1B2C3D4E5F6")
 * @returns Array de bytes
 */
export function hexToBytes(hexKey: string): number[] {
  const bytes: number[] = [];
  for (let i = 0; i < hexKey.length; i += 2) {
    bytes.push(parseInt(hexKey.substring(i, i + 2), 16));
  }
  return bytes;
}

/**
 * Convertit des bytes en hex
 *
 * @param bytes - Array de bytes
 * @returns String hexadécimal
 */
export function bytesToHex(bytes: number[]): string {
  return bytes.map(b => b.toString(16).padStart(2, '0')).join('').toUpperCase();
}

/**
 * Calcule le checksum HMAC-SHA256 pour validation anti-clonage
 * Formule: HMAC-SHA256(UID + Salt + ChipId)
 *
 * @param uid - UID physique de la puce
 * @param salt - Salt unique (UUID)
 * @param chipId - ChipId de la puce
 * @returns Checksum (16 premiers octets du HMAC)
 */
export async function calculateChecksum(uid: string, salt: string, chipId: string): Promise<string> {
  try {
    // TODO: Implémenter HMAC-SHA256
    // Pour l'instant, on fait un SHA256 simple (à améliorer)
    const input = `${uid}${salt}${chipId}`;
    const hash = await Crypto.digestStringAsync(
      Crypto.CryptoDigestAlgorithm.SHA256,
      input
    );

    // Prendre 16 premiers octets (32 caractères hex)
    return hash.substring(0, 32).toUpperCase();
  } catch (error) {
    console.error('[RFID Crypto] Erreur calcul checksum:', error);
    throw new Error('Impossible de calculer le checksum');
  }
}

/**
 * Met à jour la MasterKey (à appeler lors de la synchronisation avec le serveur)
 *
 * @param newMasterKey - Nouvelle clé maître depuis le backend
 */
export function updateMasterKey(newMasterKey: string): void {
  // TODO: Stocker dans SecureStore ou Keychain
  RFID_CONFIG.MASTER_KEY = newMasterKey;
  console.log('[RFID Crypto] MasterKey mise à jour');
}

/**
 * Récupère la MasterKey actuelle (pour debugging)
 * ATTENTION: Ne jamais logger cette valeur en production!
 */
export function getMasterKey(): string {
  return RFID_CONFIG.MASTER_KEY;
}

/**
 * Récupère la clé par défaut Mifare Classic
 */
export function getDefaultKey(): number[] {
  return [...RFID_CONFIG.DEFAULT_KEY];
}
