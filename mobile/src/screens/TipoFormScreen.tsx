import React, { useState } from 'react';
import { Alert, ScrollView, StyleSheet } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import { FormField, PrimaryButton, Screen } from '../components/ui';
import { useSettings } from '../context/SettingsContext';
import { showError } from '../hooks';
import { spacing } from '../theme';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'TipoForm'>;

export function TipoFormScreen({ navigation, route }: Props) {
  const { config } = useSettings();
  const editing = route.params?.id != null;
  const [nome, setNome] = useState(route.params?.nome ?? '');
  const [saving, setSaving] = useState(false);

  const onSave = async () => {
    if (!nome.trim()) {
      Alert.alert('Validação', 'Informe o nome.');
      return;
    }
    setSaving(true);
    try {
      if (editing) {
        await api.updateTipo(config, route.params!.id!, { nome });
      } else {
        await api.createTipo(config, { nome });
      }
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
        <FormField label="Nome" value={nome} onChangeText={setNome} autoFocus />
        <PrimaryButton
          title={saving ? 'Salvando…' : editing ? 'Salvar' : 'Criar'}
          onPress={onSave}
          disabled={saving}
        />
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: spacing.md },
});
