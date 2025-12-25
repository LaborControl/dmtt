/**
 * Admin Interface Layout
 *
 * Tab-based navigation for admin features:
 * - Equipment creation
 * - Control points management
 * - Chip assignment
 * - RFID chip registration
 * - Chronos (route & task timing)
 */

import { Tabs } from 'expo-router';
import React from 'react';
import { TouchableOpacity, Text, StyleSheet } from 'react-native';
import { router } from 'expo-router';
import { useAuth } from '@/contexts/AuthContext';
import { IconSymbol } from '@/components/ui/icon-symbol';
import { Colors } from '@/constants/theme';
import { useColorScheme } from '@/hooks/use-color-scheme';

export default function AdminLayout() {
  const colorScheme = useColorScheme();
  const { logout } = useAuth();

  return (
    <Tabs
      screenOptions={{
        tabBarActiveTintColor: '#8b5cf6',
        headerShown: true,
        headerStyle: {
          backgroundColor: '#8b5cf6',
        },
        headerTintColor: '#fff',
        headerTitleStyle: {
          fontWeight: 'bold',
        },
        headerRight: () => (
          <TouchableOpacity
            style={styles.headerButton}
            onPress={() => {
              router.replace('/role-selection');
            }}
          >
            <Text style={styles.headerButtonText}>↩</Text>
          </TouchableOpacity>
        ),
      }}
    >
      <Tabs.Screen
        name="equipment"
        options={{
          title: 'Équipements',
          tabBarIcon: ({ color }) => <IconSymbol size={28} name="wrench.fill" color={color} />,
        }}
      />
      <Tabs.Screen
        name="control-points"
        options={{
          title: 'Points de Contrôle',
          tabBarIcon: ({ color }) => <IconSymbol size={28} name="mappin.circle.fill" color={color} />,
        }}
      />
      <Tabs.Screen
        name="chip-assignment"
        options={{
          title: 'Affectation',
          tabBarIcon: ({ color }) => <IconSymbol size={28} name="tag.fill" color={color} />,
        }}
      />
      <Tabs.Screen
        name="register-chips"
        options={{
          title: 'Enregistrer',
          tabBarIcon: ({ color }) => <IconSymbol size={28} name="plus.circle.fill" color={color} />,
        }}
      />
      <Tabs.Screen
        name="chronos"
        options={{
          title: 'Chronos',
          tabBarIcon: ({ color }) => <IconSymbol size={28} name="timer.fill" color={color} />,
        }}
      />
    </Tabs>
  );
}

const styles = StyleSheet.create({
  headerButton: {
    marginRight: 16,
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: '#7c3aed',
    alignItems: 'center',
    justifyContent: 'center',
  },
  headerButtonText: {
    color: '#fff',
    fontSize: 20,
    fontWeight: 'bold',
  },
});
