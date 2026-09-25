import React, { useState } from 'react';
import { Alert, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import { FormField, PrimaryButton, Screen, SecondaryButton } from '../components/ui';
import {
  DEFAULT_BASE_URL,
  DEFAULT_TOKEN,
  useSettings,
} from '../context/SettingsContext';
import { showError } from '../hooks';
import { colors, spacing } from '../theme';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'Settings'>;

export function SettingsScreen({ navigation }: Props) {
  const { baseUrl, token, save, config } = useSettings();
  const [url, setUrl] = useState(baseUrl);
  const [tok, setTok] = useState(token);
  const [saving, setSaving] = useState(false);

  const onSave = async () => {
    setSaving(true);
    try {
      await save({ baseUrl: url, token: tok });
      const info = await api.ping({
        baseUrl: url.trim().replace(/\/+$/, ''),
        token: tok.trim(),
      });
      Alert.alert('Salvo', `Conectado a ${info.name} v${info.version}.`);
      navigation.goBack();
    } catch (e) {
      showError(e);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <Text style={styles.help}>
          Emulador Android: {DEFAULT_BASE_URL}{'\n'}
          Celular na mesma rede: http://IP-DO-PC:5083{'\n'}
          Use o perfil PTRON-LAN no servidor.
        </Text>
        <FormField
          label="URL base"
          value={url}
          onChangeText={setUrl}
          autoCapitalize="none"
          autoCorrect={false}
          keyboardType="url"
          placeholder={DEFAULT_BASE_URL}
        />
        <FormField
          label="Token da API"
          value={tok}
          onChangeText={setTok}
          autoCapitalize="none"
          autoCorrect={false}
          placeholder={DEFAULT_TOKEN}
        />
        <Text style={styles.current}>Atual: {config.baseUrl}</Text>
        <PrimaryButton title={saving ? 'Salvando…' : 'Salvar e testar'} onPress={onSave} disabled={saving} />
        <View style={styles.gap} />
        <SecondaryButton
          title="Restaurar padrões"
          onPress={() => {
            setUrl(DEFAULT_BASE_URL);
            setTok(DEFAULT_TOKEN);
          }}
        />
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: spacing.md, paddingBottom: spacing.xl },
  help: {
    backgroundColor: colors.chip,
    padding: spacing.md,
    borderRadius: 10,
    color: colors.textMuted,
    marginBottom: spacing.lg,
    lineHeight: 20,
    fontSize: 13,
  },
  current: { color: colors.textMuted, marginBottom: spacing.md, fontSize: 12 },
  gap: { height: spacing.sm },
});
