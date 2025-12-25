/**
 * Supervisor Interface Layout
 *
 * Tab-based navigation for supervisor features:
 * - Intercept delayed tasks
 * - Declare anomalies/alerts
 * - View recent tasks
 */

import { Tabs } from 'expo-router';
import React from 'react';
import { TouchableOpacity, Text, StyleSheet } from 'react-native';
import { router } from 'expo-router';
import { IconSymbol } from '@/components/ui/icon-symbol';
import { useColorScheme } from '@/hooks/use-color-scheme';

export default function SupervisorLayout() {
  const colorScheme = useColorScheme();

  return (
    <Tabs
      screenOptions={{
        tabBarActiveTintColor: '#f59e0b',
        headerShown: true,
        headerStyle: {
          backgroundColor: '#f59e0b',
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
        name="team"
        options={{
          title: 'Équipe',
          tabBarIcon: ({ color }) => <IconSymbol size={28} name="person.3.fill" color={color} />,
        }}
      />
      <Tabs.Screen
        name="intercept"
        options={{
          title: 'Intervention',
          tabBarIcon: ({ color }) => <IconSymbol size={28} name="bolt.fill" color={color} />,
        }}
      />
      <Tabs.Screen
        name="anomalies"
        options={{
          title: 'Anomalies',
          tabBarIcon: ({ color }) => <IconSymbol size={28} name="exclamationmark.triangle.fill" color={color} />,
        }}
      />
      <Tabs.Screen
        name="recent-tasks"
        options={{
          title: 'Activité',
          tabBarIcon: ({ color }) => <IconSymbol size={28} name="clock.fill" color={color} />,
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
    backgroundColor: '#d97706',
    alignItems: 'center',
    justifyContent: 'center',
  },
  headerButtonText: {
    color: '#fff',
    fontSize: 20,
    fontWeight: 'bold',
  },
});
