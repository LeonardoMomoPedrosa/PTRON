import React, { useEffect, useMemo, useState } from 'react';
import { Alert, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import type { Insumo } from '../api/types';
import { FormField, Loading, PrimaryButton, Screen, SecondaryButton } from '../components/ui';
import { useSettings } from '../context/SettingsContext';
import { numberPt } from '../format';
import { showError } from '../hooks';
import { colors, spacing } from '../theme';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'EquipamentoForm'>;
type BomLine = { insumoId: number; qtd: string; nome: string };

export function EquipamentoFormScreen({ navigation, route }: Props) {
  const { config } = useSettings();
  const editing = route.params?.id != null;
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [nome, setNome] = useState('');
  const [fotoPath, setFotoPath] = useState<string | null>(null);
  const [insumos, setInsumos] = useState<Insumo[]>([]);
  const [bom, setBom] = useState<BomLine[]>([]);
  const [pickId, setPickId] = useState(0);
  const [pickQtd, setPickQtd] = useState('1');
  const [insumoSearch, setInsumoSearch] = useState('');

  useEffect(() => {
    (async () => {
      try {
        const list = await api.getInsumos(config);
        setInsumos(list);
        if (editing) {
          const eq = await api.getEquipamento(config, route.params!.id!);
          setNome(eq.nome);
          setFotoPath(eq.fotoPath ?? null);
          setBom(
            eq.insumos.map((i) => ({
              insumoId: i.insumoId,
              qtd: String(i.qtd),
              nome: i.insumoNome,
            })),
          );
        }
      } catch (e) {
        showError(e);
        navigation.goBack();
      } finally {
        setLoading(false);
      }
    })();
  }, [config, editing, navigation, route.params]);

  const filtered = useMemo(() => {
    const q = insumoSearch.trim().toLowerCase();
    if (!q) return insumos.slice(0, 30);
    return insumos.filter((i) => i.nome.toLowerCase().includes(q) || i.tipoNome.toLowerCase().includes(q)).slice(0, 30);
  }, [insumoSearch, insumos]);

  const addBom = () => {
    if (!pickId || !Number(pickQtd.replace(',', '.'))) {
      Alert.alert('Validação', 'Selecione um insumo e uma quantidade.');
      return;
    }
    const found = insumos.find((i) => i.id === pickId);
    if (!found) return;
    const qtd = pickQtd.replace(',', '.');
    setBom((prev) => {
      const existing = prev.find((p) => p.insumoId === pickId);
      if (existing) {
        return prev.map((p) =>
          p.insumoId === pickId
            ? { ...p, qtd: String(Number(p.qtd.replace(',', '.')) + Number(qtd)) }
            : p,
        );
      }
      return [...prev, { insumoId: pickId, qtd, nome: found.nome }];
    });
    setPickId(0);
    setPickQtd('1');
    setInsumoSearch('');
  };

  const onSave = async () => {
    if (!nome.trim()) {
      Alert.alert('Validação', 'Informe o nome.');
      return;
    }
    setSaving(true);
    try {
      const body = {
        nome,
        fotoPath,
        insumos: bom
          .map((b) => ({ insumoId: b.insumoId, qtd: Number(b.qtd.replace(',', '.')) }))
          .filter((b) => b.insumoId && b.qtd > 0),
      };
      if (editing) await api.updateEquipamento(config, route.params!.id!, body);
      else await api.createEquipamento(config, body);
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
        <FormField label="Nome" value={nome} onChangeText={setNome} />
        <Text style={styles.section}>BOM</Text>
        {bom.map((line) => (
          <View key={line.insumoId} style={styles.bomRow}>
            <View style={styles.flex}>
              <Text style={styles.bomName}>{line.nome}</Text>
              <FormField
                label="Qtd"
                value={line.qtd}
                onChangeText={(v) =>
                  setBom((prev) => prev.map((p) => (p.insumoId === line.insumoId ? { ...p, qtd: v } : p)))
                }
                keyboardType="decimal-pad"
              />
            </View>
            <Pressable onPress={() => setBom((prev) => prev.filter((p) => p.insumoId !== line.insumoId))}>
              <Text style={styles.danger}>Remover</Text>
            </Pressable>
          </View>
        ))}

        <Text style={styles.section}>Adicionar insumo</Text>
        <FormField
          label="Buscar"
          value={insumoSearch}
          onChangeText={setInsumoSearch}
          placeholder="Nome do insumo"
        />
        <View style={styles.chips}>
          {filtered.map((i) => (
            <Pressable
              key={i.id}
              onPress={() => setPickId(i.id)}
              style={[styles.chip, pickId === i.id && styles.chipActive]}
            >
              <Text style={[styles.chipText, pickId === i.id && styles.chipTextActive]}>
                {i.nome}
              </Text>
            </Pressable>
          ))}
        </View>
        <FormField label="Quantidade" value={pickQtd} onChangeText={setPickQtd} keyboardType="decimal-pad" />
        <SecondaryButton title="Adicionar à BOM" onPress={addBom} />
        <View style={styles.gap} />
        <PrimaryButton
          title={saving ? 'Salvando…' : editing ? 'Salvar' : 'Criar'}
          onPress={onSave}
          disabled={saving}
        />
        {bom.length > 0 ? (
          <Text style={styles.hint}>{bom.length} itens · qtds: {bom.map((b) => numberPt(Number(b.qtd.replace(',', '.')))).join(', ')}</Text>
        ) : null}
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: spacing.md, paddingBottom: spacing.xl },
  section: {
    marginTop: spacing.md,
    marginBottom: spacing.sm,
    fontWeight: '700',
    color: colors.textMuted,
    textTransform: 'uppercase',
    fontSize: 12,
  },
  bomRow: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 10,
    padding: spacing.md,
    marginBottom: spacing.sm,
    flexDirection: 'row',
    gap: spacing.md,
    alignItems: 'flex-start',
  },
  bomName: { fontWeight: '700', color: colors.text, marginBottom: spacing.sm },
  flex: { flex: 1 },
  danger: { color: colors.danger, fontWeight: '700', marginTop: 8 },
  chips: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: spacing.md },
  chip: {
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    borderRadius: 999,
    paddingHorizontal: 10,
    paddingVertical: 8,
  },
  chipActive: { backgroundColor: colors.primary, borderColor: colors.primary },
  chipText: { color: colors.text, fontSize: 12 },
  chipTextActive: { color: '#fff', fontWeight: '700' },
  gap: { height: spacing.md },
  hint: { marginTop: spacing.sm, color: colors.textMuted, fontSize: 12 },
});
