/**
 * Loading Spinner Component
 *
 * Reusable loading indicator with optional message
 * Supports dark theme for DMTT nuclear application
 */

import React from 'react';
import {
  View,
  Text,
  StyleSheet,
  ActivityIndicator
} from 'react-native';

interface LoadingSpinnerProps {
  message?: string;
  color?: string;
  size?: 'small' | 'large';
  fullScreen?: boolean;
  darkMode?: boolean;
}

export default function LoadingSpinner({
  message = 'Chargement...',
  color,
  size = 'large',
  fullScreen = true,
  darkMode = true
}: LoadingSpinnerProps) {
  const spinnerColor = color || (darkMode ? '#3b82f6' : '#2563eb');
  const backgroundColor = darkMode ? '#0f172a' : '#f8fafc';
  const textColor = darkMode ? '#94a3b8' : '#64748b';

  const content = (
    <View style={styles.content}>
      <ActivityIndicator size={size} color={spinnerColor} />
      {message && (
        <Text style={[styles.message, { color: textColor }]}>{message}</Text>
      )}
    </View>
  );

  if (fullScreen) {
    return (
      <View style={[styles.fullScreenContainer, { backgroundColor }]}>
        {content}
      </View>
    );
  }

  return content;
}

const styles = StyleSheet.create({
  fullScreenContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 20
  },
  content: {
    alignItems: 'center',
    gap: 16
  },
  message: {
    fontSize: 14,
    textAlign: 'center',
    marginTop: 8
  }
});
