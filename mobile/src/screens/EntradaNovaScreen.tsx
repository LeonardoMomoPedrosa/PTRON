import React, { useEffect, useMemo, useState } from 'react';
import { Alert, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import type { EntradaEstoque, EntradaWrite, Insumo } from '../api/types';
import { FormField, Loading, PrimaryButton, Screen, SecondaryButton } from '../components/ui';
import { useSettings } from '../context/SettingsContext';
import { money, moneyCurrency, numberPt, percentPt } from '../format';
import { showError } from '../hooks';
import { colors, spacing } from '../theme';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'EntradaNova'>;
type CartLine = { insumoId: number; nome: string; qtd: string; preco: string };
type Moeda = 'BRL' | 'USD';

function parseDecimal(value: string): number {
  return Number(value.replace(',', '.')) || 0;
}

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
  const [moeda, setMoeda] = useState<Moeda>('BRL');
  const [cambio, setCambio] = useState('');
  const [frete, setFrete] = useState('0');
  const [impostos, setImpostos] = useState('0');
  const [preview, setPreview] = useState<EntradaEstoque | null>(null);

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

  const totalProdutos = cart.reduce((sum, line) => sum + parseDecimal(line.qtd) * parseDecimal(line.preco), 0);
  const freteNum = parseDecimal(frete);
  const impostosNum = parseDecimal(impostos);
  const totalMoeda = totalProdutos + freteNum + impostosNum;
  const cambioNum = parseDecimal(cambio);
  const symbol = moeda === 'USD' ? 'US$' : 'R$';

  const clearPreview = () => setPreview(null);

  const suggestPrice = (insumo: Insumo, currency: Moeda, rate: number): string => {
    const custo = currency === 'USD' ? (rate > 0 ? insumo.custoUnitario / rate : 0) : insumo.custoUnitario;
    return custo > 0 ? String(Math.round(custo * 10000) / 10000) : '0';
  };

  const pickInsumo = (insumo: Insumo) => {
    setPickId(insumo.id);
    setPreco(suggestPrice(insumo, moeda, cambioNum));
  };

  const body = (): EntradaWrite | null => {
    if (!cart.length) {
      Alert.alert('Validação', 'Adicione ao menos um item.');
      return null;
    }
    if (moeda === 'USD' && !(cambioNum > 0)) {
      Alert.alert('Validação', 'Informe o câmbio (quantos R$ valem 1 US$).');
      return null;
    }
    if (freteNum < 0 || impostosNum < 0) {
      Alert.alert('Validação', 'Frete e impostos não podem ser negativos.');
      return null;
    }
    return {
      moeda,
      cambio: moeda === 'USD' ? cambioNum : 0,
      frete: freteNum,
      impostos: impostosNum,
      itens: cart.map((c) => ({
        insumoId: c.insumoId,
        qtd: parseDecimal(c.qtd),
        precoUnitario: parseDecimal(c.preco),
      })),
    };
  };

  const add = () => {
    const found = insumos.find((i) => i.id === pickId);
    const q = parseDecimal(qtd);
    const p = Number(preco.replace(',', '.'));
    if (!found || !(q > 0) || !(p >= 0) || Number.isNaN(p)) {
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
                qtd: String(parseDecimal(c.qtd) + q),
                preco: String(
                  (parseDecimal(c.qtd) * parseDecimal(c.preco) + q * p) / (parseDecimal(c.qtd) + q),
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
    clearPreview();
  };

  const showPreview = async () => {
    const payload = body();
    if (!payload) return;
    setSaving(true);
    try {
      setPreview(await api.previewEntrada(config, payload));
    } catch (e) {
      showError(e);
    } finally {
      setSaving(false);
    }
  };

  const finalize = async () => {
    const payload = body();
    if (!payload || !preview) return;
    setSaving(true);
    try {
      const created = await api.finalizarEntrada(config, payload);
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
        <Text style={styles.section}>Moeda</Text>
        <View style={styles.chips}>
          {(['BRL', 'USD'] as Moeda[]).map((option) => (
            <Pressable
              key={option}
              onPress={() => {
                setMoeda(option);
                clearPreview();
              }}
              style={[styles.chip, moeda === option && styles.chipActive]}
            >
              <Text style={[styles.chipText, moeda === option && styles.chipTextActive]}>
                {option === 'USD' ? 'US$' : 'R$'}
              </Text>
            </Pressable>
          ))}
        </View>
        {moeda === 'USD' ? (
          <>
            <FormField
              label="Câmbio (R$ por US$)"
              value={cambio}
              onChangeText={(value) => {
                setCambio(value);
                clearPreview();
              }}
              keyboardType="decimal-pad"
              placeholder="Ex.: 5,45"
            />
            <Text style={styles.hint}>Os produtos, o frete e os impostos desta entrada são em dólar.</Text>
          </>
        ) : null}
        <FormField
          label={`Frete (${symbol})`}
          value={frete}
          onChangeText={(value) => {
            setFrete(value);
            clearPreview();
          }}
          keyboardType="decimal-pad"
        />
        <FormField
          label={`Impostos (${symbol})`}
          value={impostos}
          onChangeText={(value) => {
            setImpostos(value);
            clearPreview();
          }}
          keyboardType="decimal-pad"
        />

        <FormField label="Buscar insumo" value={search} onChangeText={setSearch} />
        <View style={styles.chips}>
          {filtered.map((i) => (
            <Pressable
              key={i.id}
              onPress={() => pickInsumo(i)}
              style={[styles.chip, pickId === i.id && styles.chipActive]}
            >
              <Text style={[styles.chipText, pickId === i.id && styles.chipTextActive]}>{i.nome}</Text>
            </Pressable>
          ))}
        </View>
        <FormField label="Quantidade" value={qtd} onChangeText={setQtd} keyboardType="decimal-pad" />
        <FormField
          label={`Preço unitário (${symbol})`}
          value={preco}
          onChangeText={setPreco}
          keyboardType="decimal-pad"
        />
        <SecondaryButton title="Adicionar ao carrinho" onPress={add} />

        <Text style={styles.section}>
          Carrinho · produtos {moneyCurrency(totalProdutos, moeda)}
        </Text>
        {cart.map((line) => (
          <View key={line.insumoId} style={styles.card}>
            <Text style={styles.title}>{line.nome}</Text>
            <Text style={styles.meta}>
              {numberPt(parseDecimal(line.qtd))} × {moneyCurrency(parseDecimal(line.preco), moeda)}
            </Text>
            <Pressable
              onPress={() => {
                setCart((prev) => prev.filter((c) => c.insumoId !== line.insumoId));
                clearPreview();
              }}
            >
              <Text style={styles.danger}>Remover</Text>
            </Pressable>
          </View>
        ))}

        <View style={styles.card}>
          <Text style={styles.meta}>Produtos: {moneyCurrency(totalProdutos, moeda)}</Text>
          <Text style={styles.meta}>Frete: {moneyCurrency(freteNum, moeda)}</Text>
          <Text style={styles.meta}>Impostos: {moneyCurrency(impostosNum, moeda)}</Text>
          <Text style={styles.title}>Total {symbol}: {moneyCurrency(totalMoeda, moeda)}</Text>
          {moeda === 'USD' && cambioNum > 0 ? (
            <Text style={styles.title}>Total em R$: {money(totalMoeda * cambioNum)}</Text>
          ) : null}
        </View>

        <PrimaryButton
          title={saving && !preview ? 'Calculando…' : 'Pré-visualizar'}
          onPress={showPreview}
          disabled={saving || !cart.length}
        />

        {preview ? (
          <View>
            <Text style={styles.section}>Pré-visualização</Text>
            <Text style={styles.hint}>
              A proporção usa só o valor dos produtos. Frete e impostos entram nessa mesma proporção.
            </Text>
            {preview.itens.map((item) => (
              <View key={item.insumoId} style={styles.card}>
                <Text style={styles.title}>{item.insumoNome}</Text>
                <Text style={styles.meta}>
                  {numberPt(item.qtd)} × {moneyCurrency(item.precoUnitario, preview.moeda)} ={' '}
                  {moneyCurrency(item.subtotal, preview.moeda)}
                </Text>
                <Text style={styles.meta}>Proporção: {percentPt(item.proporcao)}</Text>
                <Text style={styles.meta}>
                  Frete: {moneyCurrency(item.freteRateado, preview.moeda)} · Impostos:{' '}
                  {moneyCurrency(item.impostoRateado, preview.moeda)}
                </Text>
                <Text style={styles.title}>
                  Custo unit. {money(item.custoUnitario)} · Total {money(item.subtotalBrl)}
                </Text>
              </View>
            ))}
            <Text style={styles.section}>Total em R$ {money(preview.total)}</Text>
            <PrimaryButton
              title={saving ? 'Finalizando…' : 'Confirmar entrada'}
              onPress={finalize}
              disabled={saving}
            />
          </View>
        ) : null}
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
  hint: { color: colors.textMuted, marginBottom: spacing.sm },
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
