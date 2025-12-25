/**
 * NFC Service - Centralized NFC operations
 *
 * Handles all NFC scanning and UID extraction
 */

import NfcManager, { NfcTech } from 'react-native-nfc-manager';

/**
 * Extract UID from NFC tag
 */
export function extractUid(tag: any): string | null {
  if (!tag || !tag.id) {
    return null;
  }

  let uid: string;

  if (Array.isArray(tag.id)) {
    uid = tag.id
      .map((byte: number) => byte.toString(16).padStart(2, '0'))
      .join('')
      .toUpperCase();
  } else if (typeof tag.id === 'string') {
    uid = tag.id.toUpperCase();
  } else {
    uid = String(tag.id).toUpperCase();
  }

  return uid;
}

/**
 * Scan a single NFC tag
 * Returns the UID or throws an error
 */
export async function scanNfcTag(): Promise<string> {
  try {
    await NfcManager.start();
    await NfcManager.requestTechnology(NfcTech.Ndef);

    const tag = await NfcManager.getTag();

    await NfcManager.cancelTechnologyRequest();

    if (!tag) {
      throw new Error('No tag detected');
    }

    const uid = extractUid(tag);

    if (!uid) {
      throw new Error('Failed to extract UID');
    }

    console.log('[NFC] Scanned UID:', uid);
    return uid;
  } catch (error: any) {
    await NfcManager.cancelTechnologyRequest().catch(() => {});

    // Don't throw for user-cancelled scans
    if (error.message?.includes('cancel') || error.message?.includes('Cancel')) {
      throw new Error('SCAN_CANCELLED');
    }

    throw error;
  }
}

/**
 * Initialize NFC Manager
 */
export async function initNfc(): Promise<boolean> {
  try {
    await NfcManager.start();
    return true;
  } catch (error) {
    console.error('[NFC] Failed to initialize:', error);
    return false;
  }
}

/**
 * Cancel ongoing NFC scan
 */
export async function cancelNfcScan(): Promise<void> {
  try {
    await NfcManager.cancelTechnologyRequest();
  } catch (error) {
    // Ignore errors when cancelling
  }
}
