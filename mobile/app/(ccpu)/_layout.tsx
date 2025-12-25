/**
 * CCPU (Chargé de Contrôle Préparation Usinage) - Tab Layout
 *
 * Navigation for CCPU role with purple theme
 * Tabs: Materials, Welds, Scan, Profile
 */

import React from 'react';
import { Tabs } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';
import { Platform } from 'react-native';

export default function CCPULayout() {
  return (
    <Tabs
      screenOptions={{
        tabBarActiveTintColor: '#8b5cf6',
        tabBarInactiveTintColor: '#64748b',
        tabBarStyle: {
          backgroundColor: '#1e293b',
          borderTopColor: '#334155',
          borderTopWidth: 1,
          paddingTop: 8,
          paddingBottom: Platform.OS === 'ios' ? 24 : 8,
          height: Platform.OS === 'ios' ? 88 : 64
        },
        tabBarLabelStyle: {
          fontSize: 12,
          fontWeight: '600'
        },
        headerStyle: {
          backgroundColor: '#0f172a'
        },
        headerTintColor: '#f1f5f9',
        headerTitleStyle: {
          fontWeight: '600'
        }
      }}
    >
      <Tabs.Screen
        name="materials"
        options={{
          title: 'Matériaux',
          headerTitle: 'Validation Matériaux',
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="cube-outline" size={size} color={color} />
          )
        }}
      />
      <Tabs.Screen
        name="welds"
        options={{
          title: 'Soudures',
          headerTitle: 'Validation Soudures',
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="flame-outline" size={size} color={color} />
          )
        }}
      />
      <Tabs.Screen
        name="scan"
        options={{
          title: 'Scanner',
          headerTitle: 'Scanner NFC',
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="scan" size={size} color={color} />
          )
        }}
      />
      <Tabs.Screen
        name="profile"
        options={{
          title: 'Profil',
          headerTitle: 'Mon Profil',
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="person-outline" size={size} color={color} />
          )
        }}
      />
    </Tabs>
  );
}
