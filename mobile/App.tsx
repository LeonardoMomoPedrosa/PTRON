import React from 'react';
import { StatusBar } from 'expo-status-bar';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { SettingsProvider } from './src/context/SettingsContext';
import { RootNavigator } from './src/navigation/RootNavigator';

export default function App() {
  return (
    <SafeAreaProvider>
      <SettingsProvider>
        <StatusBar style="dark" />
        <RootNavigator />
      </SettingsProvider>
    </SafeAreaProvider>
  );
}
