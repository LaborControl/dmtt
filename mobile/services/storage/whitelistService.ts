/**
 * Service de gestion de la whitelist locale des puces RFID
 *
 * Fonctionnement:
 * - Liste des ChipId autorisés pour le customer connecté
 * - Stockage local pour validation 100% offline
 * - Synchronisation avec le backend lors de connexion
 */

import { storage as AsyncStorage } from '@/utils/storage';

const STORAGE_KEYS = {
  WHITELIST: '@laborcontrol:rfid_whitelist',
  WHITELIST_UPDATED: '@laborcontrol:rfid_whitelist_updated',
  CUSTOMER_ID: '@laborcontrol:customer_id',
};

/**
 * Structure d'une puce dans la whitelist
 */
export interface WhitelistedChip {
  chipId: string;              // GUID de la puce
  controlPointId?: string;     // Point de contrôle assigné (optionnel)
  controlPointName?: string;   // Nom du point de contrôle
  activatedAt: string;         // Date d'activation ISO 8601
  status: 'ACTIVE' | 'INACTIVE' | 'SAV';
}

/**
 * Whitelist complète
 */
export interface ChipWhitelist {
  customerId: string;
  chips: WhitelistedChip[];
  lastUpdated: string;         // ISO 8601 timestamp
}

/**
 * Ajoute une puce à la whitelist locale
 *
 * @param chip - Données de la puce à ajouter
 */
export async function addToWhitelist(chip: WhitelistedChip): Promise<void> {
  try {
    const whitelist = await getWhitelist();

    // Vérifier si la puce existe déjà
    const existingIndex = whitelist.chips.findIndex(c => c.chipId === chip.chipId);

    if (existingIndex >= 0) {
      // Mettre à jour la puce existante
      whitelist.chips[existingIndex] = chip;
      console.log(`[Whitelist] Puce mise à jour: ${chip.chipId}`);
    } else {
      // Ajouter la nouvelle puce
      whitelist.chips.push(chip);
      console.log(`[Whitelist] Puce ajoutée: ${chip.chipId}`);
    }

    // Mettre à jour le timestamp
    whitelist.lastUpdated = new Date().toISOString();

    // Sauvegarder
    await saveWhitelist(whitelist);
  } catch (error) {
    console.error('[Whitelist] Erreur ajout puce:', error);
    throw new Error('Impossible d\'ajouter la puce à la whitelist');
  }
}

/**
 * Vérifie si une puce est dans la whitelist locale
 *
 * @param chipId - GUID de la puce à vérifier
 * @returns Puce si trouvée, null sinon
 */
export async function isChipWhitelisted(chipId: string): Promise<WhitelistedChip | null> {
  try {
    const whitelist = await getWhitelist();

    const chip = whitelist.chips.find(c => c.chipId === chipId && c.status === 'ACTIVE');

    if (chip) {
      console.log(`[Whitelist] ✅ Puce autorisée: ${chipId}`);
      return chip;
    } else {
      console.log(`[Whitelist] ❌ Puce NON autorisée: ${chipId}`);
      return null;
    }
  } catch (error) {
    console.error('[Whitelist] Erreur vérification puce:', error);
    return null;
  }
}

/**
 * Récupère la whitelist complète
 *
 * @returns Whitelist actuelle
 */
export async function getWhitelist(): Promise<ChipWhitelist> {
  try {
    const data = await AsyncStorage.getItem(STORAGE_KEYS.WHITELIST);

    if (!data) {
      // Créer une whitelist vide
      const customerId = await AsyncStorage.getItem(STORAGE_KEYS.CUSTOMER_ID) || '';
      return {
        customerId,
        chips: [],
        lastUpdated: new Date().toISOString(),
      };
    }

    return JSON.parse(data);
  } catch (error) {
    console.error('[Whitelist] Erreur lecture whitelist:', error);
    throw new Error('Impossible de lire la whitelist locale');
  }
}

/**
 * Sauvegarde la whitelist complète
 *
 * @param whitelist - Whitelist à sauvegarder
 */
export async function saveWhitelist(whitelist: ChipWhitelist): Promise<void> {
  try {
    await AsyncStorage.setItem(STORAGE_KEYS.WHITELIST, JSON.stringify(whitelist));
    await AsyncStorage.setItem(STORAGE_KEYS.WHITELIST_UPDATED, whitelist.lastUpdated);
    console.log(`[Whitelist] Sauvegardée: ${whitelist.chips.length} puces`);
  } catch (error) {
    console.error('[Whitelist] Erreur sauvegarde:', error);
    throw new Error('Impossible de sauvegarder la whitelist');
  }
}

/**
 * Synchronise la whitelist avec le backend
 *
 * @param apiUrl - URL de l'API
 * @param token - Token JWT
 * @param customerId - ID du customer
 * @returns Nombre de puces synchronisées
 */
export async function syncWhitelist(apiUrl: string, token: string, customerId: string): Promise<number> {
  try {
    console.log('[Whitelist] Début synchronisation avec backend...');

    // Appeler l'API pour récupérer la whitelist du customer
    const response = await fetch(`${apiUrl}/rfidchips/whitelist/${customerId}`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Erreur API: ${response.status}`);
    }

    const data = await response.json();

    // Construire la whitelist
    const whitelist: ChipWhitelist = {
      customerId,
      chips: data.chips || [],
      lastUpdated: new Date().toISOString(),
    };

    // Sauvegarder localement
    await saveWhitelist(whitelist);

    // Sauvegarder le customerId
    await AsyncStorage.setItem(STORAGE_KEYS.CUSTOMER_ID, customerId);

    console.log(`[Whitelist] ✅ Synchronisation réussie: ${whitelist.chips.length} puces`);
    return whitelist.chips.length;
  } catch (error) {
    console.error('[Whitelist] ❌ Erreur synchronisation:', error);
    throw new Error('Impossible de synchroniser la whitelist avec le serveur');
  }
}

/**
 * Supprime une puce de la whitelist locale
 *
 * @param chipId - GUID de la puce à supprimer
 */
export async function removeFromWhitelist(chipId: string): Promise<void> {
  try {
    const whitelist = await getWhitelist();

    const initialLength = whitelist.chips.length;
    whitelist.chips = whitelist.chips.filter(c => c.chipId !== chipId);

    if (whitelist.chips.length < initialLength) {
      whitelist.lastUpdated = new Date().toISOString();
      await saveWhitelist(whitelist);
      console.log(`[Whitelist] Puce supprimée: ${chipId}`);
    } else {
      console.log(`[Whitelist] Puce non trouvée: ${chipId}`);
    }
  } catch (error) {
    console.error('[Whitelist] Erreur suppression puce:', error);
    throw new Error('Impossible de supprimer la puce de la whitelist');
  }
}

/**
 * Vide complètement la whitelist locale
 * ATTENTION: Utiliser uniquement lors de déconnexion ou changement de customer
 */
export async function clearWhitelist(): Promise<void> {
  try {
    await AsyncStorage.removeItem(STORAGE_KEYS.WHITELIST);
    await AsyncStorage.removeItem(STORAGE_KEYS.WHITELIST_UPDATED);
    await AsyncStorage.removeItem(STORAGE_KEYS.CUSTOMER_ID);
    console.log('[Whitelist] ✅ Whitelist vidée');
  } catch (error) {
    console.error('[Whitelist] Erreur vidage whitelist:', error);
    throw new Error('Impossible de vider la whitelist');
  }
}

/**
 * Obtient les statistiques de la whitelist
 *
 * @returns Stats de la whitelist
 */
export async function getWhitelistStats(): Promise<{
  total: number;
  active: number;
  inactive: number;
  sav: number;
  lastUpdated: string;
}> {
  try {
    const whitelist = await getWhitelist();

    return {
      total: whitelist.chips.length,
      active: whitelist.chips.filter(c => c.status === 'ACTIVE').length,
      inactive: whitelist.chips.filter(c => c.status === 'INACTIVE').length,
      sav: whitelist.chips.filter(c => c.status === 'SAV').length,
      lastUpdated: whitelist.lastUpdated,
    };
  } catch (error) {
    console.error('[Whitelist] Erreur stats:', error);
    return {
      total: 0,
      active: 0,
      inactive: 0,
      sav: 0,
      lastUpdated: new Date().toISOString(),
    };
  }
}

/**
 * Obtient la liste des points de contrôle depuis la whitelist
 * (utile pour afficher les points de contrôle disponibles hors ligne)
 *
 * @returns Liste des points de contrôle uniques
 */
export async function getControlPointsFromWhitelist(): Promise<{ id: string; name: string }[]> {
  try {
    const whitelist = await getWhitelist();

    const controlPoints = new Map<string, string>();

    whitelist.chips.forEach(chip => {
      if (chip.controlPointId && chip.controlPointName) {
        controlPoints.set(chip.controlPointId, chip.controlPointName);
      }
    });

    return Array.from(controlPoints.entries()).map(([id, name]) => ({ id, name }));
  } catch (error) {
    console.error('[Whitelist] Erreur récupération points de contrôle:', error);
    return [];
  }
}
