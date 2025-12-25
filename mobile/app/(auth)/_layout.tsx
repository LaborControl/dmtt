/**
 * Auth Layout
 *
 * Stack navigation for authentication screens
 * - Login
 * - Role Selection
 */

import { Stack } from 'expo-router';

export default function AuthLayout() {
  return (
    <Stack
      screenOptions={{
        headerShown: false,
        animation: 'fade'
      }}
    >
      <Stack.Screen name="login" />
      <Stack.Screen name="role-selection" />
    </Stack>
  );
}
