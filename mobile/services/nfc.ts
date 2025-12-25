/**
 * NFC Service for Labor Control DMTT
 *
 * Provides NFC reading and writing capabilities with 4-level access control:
 * 1. WELDER - Can read weld tags to start/end execution
 * 2. NDT_CONTROLLER - Can read weld tags for NDT control execution
 * 3. CCPU - Can read/write material and weld tags for validation
 * 4. ADMIN - Full read/write access to all tag types
 */

import { Platform } from 'react-native';

// NFC Tag Types
export type NfcTagType = 'WELD' | 'MATERIAL' | 'ASSET' | 'UNKNOWN';

// User roles with NFC access
export type NfcAccessRole = 'WELDER' | 'NDT_CONTROLLER' | 'CCPU' | 'ADMIN' | 'NONE';

// NFC Tag Data Structure
export interface NfcTagData {
  id: string;
  type: NfcTagType;
  reference?: string;
  additionalData?: Record<string, string>;
  rawPayload?: string;
}

// NFC Write Payload
export interface NfcWritePayload {
  type: NfcTagType;
  id: string;
  reference?: string;
  additionalData?: Record<string, string>;
}

// NFC Scan Result
export interface NfcScanResult {
  success: boolean;
  data?: NfcTagData;
  error?: string;
  tagId?: string;
}

// NFC Write Result
export interface NfcWriteResult {
  success: boolean;
  error?: string;
}

// Access Level Configuration
const ACCESS_LEVELS: Record<NfcAccessRole, {
  canRead: NfcTagType[];
  canWrite: NfcTagType[];
}> = {
  WELDER: {
    canRead: ['WELD'],
    canWrite: []
  },
  NDT_CONTROLLER: {
    canRead: ['WELD'],
    canWrite: []
  },
  CCPU: {
    canRead: ['WELD', 'MATERIAL', 'ASSET'],
    canWrite: ['WELD', 'MATERIAL']
  },
  ADMIN: {
    canRead: ['WELD', 'MATERIAL', 'ASSET', 'UNKNOWN'],
    canWrite: ['WELD', 'MATERIAL', 'ASSET']
  },
  NONE: {
    canRead: [],
    canWrite: []
  }
};

class NfcService {
  private isInitialized: boolean = false;
  private currentRole: NfcAccessRole = 'NONE';

  /**
   * Initialize NFC service
   */
  async initialize(): Promise<boolean> {
    if (this.isInitialized) return true;

    try {
      // Check platform support
      if (Platform.OS !== 'ios' && Platform.OS !== 'android') {
        console.warn('NFC not supported on this platform');
        return false;
      }

      // In production, initialize NFC manager:
      // await NfcManager.start();
      // const supported = await NfcManager.isSupported();
      // const enabled = await NfcManager.isEnabled();

      this.isInitialized = true;
      return true;
    } catch (error) {
      console.error('Failed to initialize NFC:', error);
      return false;
    }
  }

  /**
   * Set current user role for access control
   */
  setRole(role: NfcAccessRole): void {
    this.currentRole = role;
  }

  /**
   * Check if current role can read a tag type
   */
  canRead(tagType: NfcTagType): boolean {
    return ACCESS_LEVELS[this.currentRole].canRead.includes(tagType);
  }

  /**
   * Check if current role can write a tag type
   */
  canWrite(tagType: NfcTagType): boolean {
    return ACCESS_LEVELS[this.currentRole].canWrite.includes(tagType);
  }

  /**
   * Scan an NFC tag
   */
  async scanTag(): Promise<NfcScanResult> {
    if (!this.isInitialized) {
      const initialized = await this.initialize();
      if (!initialized) {
        return { success: false, error: 'NFC non disponible' };
      }
    }

    try {
      // In production:
      // await NfcManager.requestTechnology(NfcTech.Ndef);
      // const tag = await NfcManager.getTag();
      // const ndefRecords = tag?.ndefMessage;
      // const payload = this.parseNdefRecords(ndefRecords);

      // For development, return mock data
      return this.simulateScan();
    } catch (error: any) {
      if (error.message?.includes('cancelled')) {
        return { success: false, error: 'Scan annulé' };
      }
      console.error('NFC scan error:', error);
      return { success: false, error: 'Erreur de lecture NFC' };
    } finally {
      // await NfcManager.cancelTechnologyRequest();
    }
  }

  /**
   * Write data to an NFC tag
   */
  async writeTag(payload: NfcWritePayload): Promise<NfcWriteResult> {
    if (!this.canWrite(payload.type)) {
      return {
        success: false,
        error: `Écriture ${payload.type} non autorisée pour ce rôle`
      };
    }

    if (!this.isInitialized) {
      const initialized = await this.initialize();
      if (!initialized) {
        return { success: false, error: 'NFC non disponible' };
      }
    }

    try {
      const ndefPayload = this.createNdefPayload(payload);

      // In production:
      // await NfcManager.requestTechnology(NfcTech.Ndef);
      // await NfcManager.ndefHandler.writeNdefMessage([
      //   Ndef.textRecord(ndefPayload)
      // ]);

      console.log('NFC Write payload:', ndefPayload);
      return { success: true };
    } catch (error: any) {
      console.error('NFC write error:', error);
      return { success: false, error: 'Erreur d\'écriture NFC' };
    } finally {
      // await NfcManager.cancelTechnologyRequest();
    }
  }

  /**
   * Parse raw NFC payload into structured data
   */
  parsePayload(rawPayload: string): NfcTagData {
    const result: NfcTagData = {
      id: '',
      type: 'UNKNOWN',
      rawPayload
    };

    // Expected format: DMTT:<TYPE>:<ID>:<REFERENCE>:<ADDITIONAL_JSON>
    if (rawPayload.startsWith('DMTT:')) {
      const parts = rawPayload.split(':');

      if (parts.length >= 3) {
        const type = parts[1] as NfcTagType;
        result.type = ['WELD', 'MATERIAL', 'ASSET'].includes(type) ? type : 'UNKNOWN';
        result.id = parts[2];
      }

      if (parts.length >= 4) {
        result.reference = parts[3];
      }

      if (parts.length >= 5) {
        try {
          result.additionalData = JSON.parse(parts[4]);
        } catch {
          // Invalid JSON, ignore additional data
        }
      }
    } else {
      // Legacy format - just an ID
      result.id = rawPayload;
    }

    return result;
  }

  /**
   * Create NDEF payload from write request
   */
  private createNdefPayload(payload: NfcWritePayload): string {
    let ndefString = `DMTT:${payload.type}:${payload.id}`;

    if (payload.reference) {
      ndefString += `:${payload.reference}`;
    }

    if (payload.additionalData) {
      ndefString += `:${JSON.stringify(payload.additionalData)}`;
    }

    return ndefString;
  }

  /**
   * Simulate NFC scan for development
   */
  private async simulateScan(): Promise<NfcScanResult> {
    // Simulate scan delay
    await new Promise(resolve => setTimeout(resolve, 1500));

    // Generate mock data based on current role
    const mockData: NfcTagData = {
      id: `mock-${Date.now()}`,
      type: 'WELD',
      reference: `REF-${Math.random().toString(36).substring(7).toUpperCase()}`,
      rawPayload: ''
    };

    // Check access
    if (!this.canRead(mockData.type)) {
      return {
        success: false,
        error: `Lecture ${mockData.type} non autorisée pour ce rôle`
      };
    }

    mockData.rawPayload = this.createNdefPayload({
      type: mockData.type,
      id: mockData.id,
      reference: mockData.reference
    });

    return {
      success: true,
      data: mockData,
      tagId: `TAG-${Date.now()}`
    };
  }

  /**
   * Get access level description for current role
   */
  getAccessDescription(): string {
    const access = ACCESS_LEVELS[this.currentRole];
    const readTypes = access.canRead.join(', ') || 'Aucun';
    const writeTypes = access.canWrite.join(', ') || 'Aucun';

    return `Lecture: ${readTypes}\nÉcriture: ${writeTypes}`;
  }

  /**
   * Cleanup NFC resources
   */
  async cleanup(): Promise<void> {
    try {
      // In production: await NfcManager.cancelTechnologyRequest();
      this.isInitialized = false;
    } catch (error) {
      console.error('NFC cleanup error:', error);
    }
  }
}

// Export singleton instance
export const nfcService = new NfcService();

// Role mapping utility
export function mapUserRoleToNfcAccess(userRole: string): NfcAccessRole {
  const roleMap: Record<string, NfcAccessRole> = {
    'WELDER': 'WELDER',
    'SOUDEUR': 'WELDER',
    'NDT_CONTROLLER': 'NDT_CONTROLLER',
    'CONTROLEUR_CND': 'NDT_CONTROLLER',
    'CCPU': 'CCPU',
    'ADMIN': 'ADMIN',
    'ADMINISTRATOR': 'ADMIN',
    'WELDING_COORDINATOR': 'ADMIN',
    'QUALITY_MANAGER': 'ADMIN',
    'RQ': 'ADMIN'
  };

  return roleMap[userRole.toUpperCase()] || 'NONE';
}
