/**
 * User Layout
 *
 * Tab navigation for USER role
 * - Tasks (home)
 * - Anomaly
 * - History
 * - Profile
 */

import { Tabs } from 'expo-router';
import { View } from 'react-native';
import { IconSymbol } from '@/components/ui/icon-symbol';
import { Colors } from '@/constants/theme';
import { useColorScheme } from '@/hooks/use-color-scheme';
import OfflineBadge from '@/components/shared/OfflineBadge';

export default function UserLayout() {
  const colorScheme = useColorScheme();

  return (
    <>
      <OfflineBadge />
      <Tabs
        screenOptions={{
          tabBarActiveTintColor: Colors[colorScheme ?? 'light'].tint,
          headerShown: false
        }}
      >
      <Tabs.Screen
        name="tasks"
        options={{
          title: 'Tâches',
          tabBarIcon: ({ color }) => (
            <IconSymbol size={28} name="house.fill" color={color} />
          )
        }}
      />
      <Tabs.Screen
        name="anomaly"
        options={{
          title: 'Anomalie',
          tabBarIcon: ({ color }) => (
            <IconSymbol size={28} name="exclamationmark.triangle.fill" color={color} />
          )
        }}
      />
      <Tabs.Screen
        name="history"
        options={{
          title: 'Historique',
          tabBarIcon: ({ color }) => (
            <IconSymbol size={28} name="clock.fill" color={color} />
          )
        }}
      />
      <Tabs.Screen
        name="profile"
        options={{
          title: 'Profil',
          tabBarIcon: ({ color }) => (
            <IconSymbol size={28} name="person.fill" color={color} />
          )
        }}
      />
      </Tabs>
    </>
  );
}
