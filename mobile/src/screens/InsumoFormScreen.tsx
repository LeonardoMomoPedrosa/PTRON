import React, { useEffect, useState } from 'react';
import { Alert, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import type { Tipo } from '../api/types';
import { FormField, Loading, PrimaryButton, Screen } from '../components/ui';
import { useSettings } from '../context/SettingsContext';
import { showError } from '../hooks';
import { colors, spacing } from '../theme';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'InsumoForm'>;

export function InsumoFormScreen({ navigation, route }: Props) {
  const { config } = useSettings();
  const editing = route.params?.id != null;
  const [loading, setLoading] = useState(editing);
  const [saving, setSaving] = useState(false);
  const [tipos, setTipos] = useState<Tipo[]>([]);
  const [tipoId, setTipoId] = useState(0);
  const [nome, setNome] = useState('');
  const [valor, setValor] = useState('');
  const [potencia, setPotencia] = useState('');
  const [voltagem, setVoltagem] = useState('');
  const [fotoPath, setFotoPath] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      try {
        const t = await api.getTipos(config);
        setTipos(t);
        if (editing) {
          const item = await api.getInsumo(config, route.params!.id!);
          setTipoId(item.tipoInsumoId);
          setNome(item.nome);
          setValor(item.valor ?? '');
          setPotencia(item.potencia ?? '');
          setVoltagem(item.voltagem ?? '');
          setFotoPath(item.fotoPath ?? null);
        } else if (t[0]) {
          setTipoId(t[0].id);
        }
      } catch (e) {
        showError(e);
        navigation.goBack();
      } finally {
        setLoading(false);
      }
    })();
  }, [config, editing, navigation, route.params]);

  const onSave = async () => {
    if (!nome.trim() || tipoId === 0) {
      Alert.alert('Validação', 'Informe nome e tipo.');
      return;
    }
    setSaving(true);
    try {
      const body = {
        tipoInsumoId: tipoId,
        nome,
        valor: valor || null,
        potencia: potencia || null,
        voltagem: voltagem || null,
        fotoPath,
      };
      if (editing) await api.updateInsumo(config, route.params!.id!, body);
      else await api.createInsumo(config, body);
      navigation.goBack();
    } catch (e) {
      showError(e);
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <Loading />;

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <Text style={styles.label}>Tipo</Text>
        <View style={styles.chips}>
          {tipos.map((t) => (
            <Pressable
              key={t.id}
              onPress={() => setTipoId(t.id)}
              style={[styles.chip, tipoId === t.id && styles.chipActive]}
            >
              <Text style={[styles.chipText, tipoId === t.id && styles.chipTextActive]}>{t.nome}</Text>
            </Pressable>
          ))}
        </View>
        <FormField label="Nome" value={nome} onChangeText={setNome} />
        <FormField label="Valor" value={valor} onChangeText={setValor} placeholder="ex: 10Ω" />
        <FormField label="Potência" value={potencia} onChangeText={setPotencia} placeholder="ex: 2W" />
        <FormField label="Voltagem" value={voltagem} onChangeText={setVoltagem} placeholder="ex: 35V" />
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
  content: { padding: spacing.md, paddingBottom: spacing.xl },
  label: { fontSize: 13, fontWeight: '600', color: colors.textMuted, marginBottom: spacing.xs },
  chips: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: spacing.md },
  chip: {
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    borderRadius: 999,
    paddingHorizontal: 12,
    paddingVertical: 8,
  },
  chipActive: { backgroundColor: colors.primary, borderColor: colors.primary },
  chipText: { color: colors.text, fontSize: 13 },
  chipTextActive: { color: '#fff', fontWeight: '700' },
});
