/**
 * NDT Controller - Tab Layout
 *
 * Navigation for NDT Controller role with blue theme
 * Tabs: Controls, Scan, History, Profile
 */

import React from 'react';
import { Tabs } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';
import { Platform } from 'react-native';

export default function NDTControllerLayout() {
  return (
    <Tabs
      screenOptions={{
        tabBarActiveTintColor: '#3b82f6',
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
        name="controls"
        options={{
          title: 'Contrôles',
          headerTitle: 'Mes Contrôles CND',
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="clipboard-outline" size={size} color={color} />
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
        name="history"
        options={{
          title: 'Historique',
          headerTitle: 'Historique Contrôles',
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="time-outline" size={size} color={color} />
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
