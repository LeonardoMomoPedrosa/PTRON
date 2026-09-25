import React, { useEffect, useMemo, useState } from 'react';
import { Alert, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import type { Insumo } from '../api/types';
import { FormField, Loading, PrimaryButton, Screen, SecondaryButton } from '../components/ui';
import { useSettings } from '../context/SettingsContext';
import { money, numberPt } from '../format';
import { showError } from '../hooks';
import { colors, spacing } from '../theme';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'EntradaNova'>;
type CartLine = { insumoId: number; nome: string; qtd: string; preco: string };

export function EntradaNovaScreen({ navigation }: Props) {
  const { config } = useSettings();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [insumos, setInsumos] = useState<Insumo[]>([]);
  const [search, setSearch] = useState('');
  const [pickId, setPickId] = useState(0);
  const [qtd, setQtd] = useState('1');
  const [preco, setPreco] = useState('0');
  const [cart, setCart] = useState<CartLine[]>([]);

  useEffect(() => {
    (async () => {
      try {
        setInsumos(await api.getInsumos(config));
      } catch (e) {
        showError(e);
        navigation.goBack();
      } finally {
        setLoading(false);
      }
    })();
  }, [config, navigation]);

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return insumos.slice(0, 25);
    return insumos
      .filter((i) => i.nome.toLowerCase().includes(q) || i.tipoNome.toLowerCase().includes(q))
      .slice(0, 25);
  }, [insumos, search]);

  const total = cart.reduce((sum, line) => {
    const q = Number(line.qtd.replace(',', '.')) || 0;
    const p = Number(line.preco.replace(',', '.')) || 0;
    return sum + q * p;
  }, 0);

  const add = () => {
    const found = insumos.find((i) => i.id === pickId);
    const q = Number(qtd.replace(',', '.'));
    const p = Number(preco.replace(',', '.'));
    if (!found || !(q > 0) || p < 0) {
      Alert.alert('Validação', 'Selecione insumo, quantidade > 0 e preço ≥ 0.');
      return;
    }
    setCart((prev) => {
      const existing = prev.find((c) => c.insumoId === pickId);
      if (existing) {
        return prev.map((c) =>
          c.insumoId === pickId
            ? {
                ...c,
                qtd: String(Number(c.qtd.replace(',', '.')) + q),
                preco: String(
                  ((Number(c.qtd.replace(',', '.')) * Number(c.preco.replace(',', '.')) + q * p) /
                    (Number(c.qtd.replace(',', '.')) + q)),
                ),
              }
            : c,
        );
      }
      return [...prev, { insumoId: pickId, nome: found.nome, qtd: String(q), preco: String(p) }];
    });
    setPickId(0);
    setQtd('1');
    setPreco('0');
    setSearch('');
  };

  const finalize = async () => {
    if (!cart.length) {
      Alert.alert('Validação', 'Adicione ao menos um item.');
      return;
    }
    setSaving(true);
    try {
      const created = await api.finalizarEntrada(config, {
        itens: cart.map((c) => ({
          insumoId: c.insumoId,
          qtd: Number(c.qtd.replace(',', '.')),
          precoUnitario: Number(c.preco.replace(',', '.')),
        })),
      });
      navigation.replace('EntradaDetalhe', { id: created.id });
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
        <FormField label="Buscar insumo" value={search} onChangeText={setSearch} />
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
        <FormField label="Quantidade" value={qtd} onChangeText={setQtd} keyboardType="decimal-pad" />
        <FormField label="Preço unitário" value={preco} onChangeText={setPreco} keyboardType="decimal-pad" />
        <SecondaryButton title="Adicionar ao carrinho" onPress={add} />

        <Text style={styles.section}>Carrinho · {money(total)}</Text>
        {cart.map((line) => (
          <View key={line.insumoId} style={styles.card}>
            <Text style={styles.title}>{line.nome}</Text>
            <Text style={styles.meta}>
              {numberPt(Number(line.qtd.replace(',', '.')))} × {money(Number(line.preco.replace(',', '.')))}
            </Text>
            <Pressable onPress={() => setCart((prev) => prev.filter((c) => c.insumoId !== line.insumoId))}>
              <Text style={styles.danger}>Remover</Text>
            </Pressable>
          </View>
        ))}

        <PrimaryButton
          title={saving ? 'Finalizando…' : 'Finalizar entrada'}
          onPress={finalize}
          disabled={saving || !cart.length}
        />
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { padding: spacing.md, paddingBottom: spacing.xl },
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
  section: {
    marginTop: spacing.lg,
    marginBottom: spacing.sm,
    fontWeight: '700',
    color: colors.textMuted,
  },
  card: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 10,
    padding: spacing.md,
    marginBottom: spacing.sm,
  },
  title: { fontWeight: '700', color: colors.text },
  meta: { marginTop: 4, color: colors.textMuted },
  danger: { marginTop: 8, color: colors.danger, fontWeight: '700' },
});
