import React, { useEffect, useMemo, useState } from 'react';
import { Alert, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import type { EntradaEstoque, EntradaWrite, Insumo } from '../api/types';
import { FormField, Loading, PrimaryButton, Screen, SecondaryButton } from '../components/ui';
import { useSettings } from '../context/SettingsContext';
import { MOEDAS, money, moneyCurrency, moedaSymbol, numberPt, percentPt, type MoedaCodigo } from '../format';
import { showError } from '../hooks';
import { colors, spacing } from '../theme';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'EntradaNova'>;
type CartLine = { insumoId: number; nome: string; qtd: string; preco: string };

function parseDecimal(value: string): number {
  return Number(value.replace(',', '.')) || 0;
}

function freteOptions(produtos: MoedaCodigo, impostos: MoedaCodigo): MoedaCodigo[] {
  const list: MoedaCodigo[] = [];
  for (const code of [produtos, impostos, 'BRL' as MoedaCodigo]) {
    if (!list.includes(code)) list.push(code);
  }
  return list;
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
  const [moedaProdutos, setMoedaProdutos] = useState<MoedaCodigo>('BRL');
  const [cambioProdutos, setCambioProdutos] = useState('');
  const [moedaImpostos, setMoedaImpostos] = useState<MoedaCodigo>('BRL');
  const [cambioImpostos, setCambioImpostos] = useState('');
  const [moedaFrete, setMoedaFrete] = useState<MoedaCodigo>('BRL');
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

  const opcoesFrete = useMemo(
    () => freteOptions(moedaProdutos, moedaImpostos),
    [moedaProdutos, moedaImpostos],
  );
  useEffect(() => {
    if (!opcoesFrete.includes(moedaFrete)) setMoedaFrete(moedaProdutos);
  }, [moedaFrete, moedaProdutos, opcoesFrete]);

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
  const cambioProdutosNum = parseDecimal(cambioProdutos);
  const cambioImpostosNum = parseDecimal(cambioImpostos);
  const mesmaMoeda = moedaImpostos === moedaProdutos;
  const cambioProdutosEfetivo = moedaProdutos === 'BRL' ? 1 : cambioProdutosNum;
  const cambioImpostosEfetivo = moedaImpostos === 'BRL' ? 1 : mesmaMoeda ? cambioProdutosEfetivo : cambioImpostosNum;
  const cambioFrete =
    moedaFrete === 'BRL' ? 1 : moedaFrete === moedaProdutos ? cambioProdutosEfetivo : cambioImpostosEfetivo;
  const moedasIguais = mesmaMoeda && moedaFrete === moedaProdutos;
  const totalMoeda = totalProdutos + freteNum + impostosNum;
  const totalBrl = totalProdutos * cambioProdutosEfetivo + freteNum * cambioFrete + impostosNum * cambioImpostosEfetivo;
  const temEstrangeira = moedaProdutos !== 'BRL' || moedaImpostos !== 'BRL' || moedaFrete !== 'BRL';
  const cambioTotalValido =
    (moedaProdutos === 'BRL' || cambioProdutosNum > 0) &&
    (moedaImpostos === 'BRL' || mesmaMoeda || cambioImpostosNum > 0);

  const clearPreview = () => setPreview(null);

  const suggestPrice = (insumo: Insumo): string => {
    const custo =
      moedaProdutos === 'BRL'
        ? insumo.custoUnitario
        : cambioProdutosNum > 0
          ? insumo.custoUnitario / cambioProdutosNum
          : 0;
    return custo > 0 ? String(Math.round(custo * 10000) / 10000) : '0';
  };

  const pickInsumo = (insumo: Insumo) => {
    setPickId(insumo.id);
    setPreco(suggestPrice(insumo));
  };

  const body = (): EntradaWrite | null => {
    if (!cart.length) {
      Alert.alert('Validação', 'Adicione ao menos um item.');
      return null;
    }
    if (moedaProdutos !== 'BRL' && !(cambioProdutosNum > 0)) {
      Alert.alert('Validação', `Informe o câmbio dos produtos (quantos R$ valem 1 ${moedaSymbol(moedaProdutos)}).`);
      return null;
    }
    if (moedaImpostos !== 'BRL' && !mesmaMoeda && !(cambioImpostosNum > 0)) {
      Alert.alert('Validação', `Informe o câmbio dos impostos (quantos R$ valem 1 ${moedaSymbol(moedaImpostos)}).`);
      return null;
    }
    if (freteNum < 0 || impostosNum < 0) {
      Alert.alert('Validação', 'Frete e impostos não podem ser negativos.');
      return null;
    }
    return {
      moedaProdutos,
      cambioProdutos: moedaProdutos === 'BRL' ? 0 : cambioProdutosNum,
      moedaImpostos,
      cambioImpostos: mesmaMoeda || moedaImpostos === 'BRL' ? 0 : cambioImpostosNum,
      moedaFrete,
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

  const previewProdutos = preview?.moedaProdutos || preview?.moeda;
  const previewImpostos = preview?.moedaImpostos || previewProdutos;
  const previewFrete = preview?.moedaFrete || previewProdutos;

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <Text style={styles.section}>Moeda dos produtos</Text>
        <CurrencyChips
          value={moedaProdutos}
          onChange={(option) => {
            setMoedaProdutos(option);
            clearPreview();
          }}
        />
        {moedaProdutos !== 'BRL' ? (
          <FormField
            label={`Câmbio (R$ por ${moedaSymbol(moedaProdutos)})`}
            value={cambioProdutos}
            onChangeText={(value) => {
              setCambioProdutos(value);
              clearPreview();
            }}
            keyboardType="decimal-pad"
            placeholder="Ex.: 5,45"
          />
        ) : null}

        <Text style={styles.section}>Moeda dos impostos</Text>
        <CurrencyChips
          value={moedaImpostos}
          onChange={(option) => {
            setMoedaImpostos(option);
            clearPreview();
          }}
        />
        {moedaImpostos !== 'BRL' && !mesmaMoeda ? (
          <FormField
            label={`Câmbio (R$ por ${moedaSymbol(moedaImpostos)})`}
            value={cambioImpostos}
            onChangeText={(value) => {
              setCambioImpostos(value);
              clearPreview();
            }}
            keyboardType="decimal-pad"
            placeholder="Ex.: 5,45"
          />
        ) : null}
        {mesmaMoeda && moedaProdutos !== 'BRL' ? (
          <Text style={styles.hint}>Produtos e impostos usam o mesmo câmbio.</Text>
        ) : null}

        <Text style={styles.section}>Moeda do frete</Text>
        <CurrencyChips
          value={moedaFrete}
          options={opcoesFrete}
          onChange={(option) => {
            setMoedaFrete(option);
            clearPreview();
          }}
        />
        <FormField
          label={`Frete (${moedaSymbol(moedaFrete)})`}
          value={frete}
          onChangeText={(value) => {
            setFrete(value);
            clearPreview();
          }}
          keyboardType="decimal-pad"
        />
        <FormField
          label={`Impostos (${moedaSymbol(moedaImpostos)})`}
          value={impostos}
          onChangeText={(value) => {
            setImpostos(value);
            clearPreview();
          }}
          keyboardType="decimal-pad"
        />
        {temEstrangeira ? (
          <Text style={styles.hint}>O custo do estoque é gravado em reais.</Text>
        ) : null}

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
          label={`Preço unitário (${moedaSymbol(moedaProdutos)})`}
          value={preco}
          onChangeText={setPreco}
          keyboardType="decimal-pad"
        />
        <SecondaryButton title="Adicionar ao carrinho" onPress={add} />

        <Text style={styles.section}>
          Carrinho · produtos {moneyCurrency(totalProdutos, moedaProdutos)}
        </Text>
        {cart.map((line) => (
          <View key={line.insumoId} style={styles.card}>
            <Text style={styles.title}>{line.nome}</Text>
            <Text style={styles.meta}>
              {numberPt(parseDecimal(line.qtd))} × {moneyCurrency(parseDecimal(line.preco), moedaProdutos)}
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
          <Text style={styles.meta}>Produtos: {moneyCurrency(totalProdutos, moedaProdutos)}</Text>
          <Text style={styles.meta}>Frete: {moneyCurrency(freteNum, moedaFrete)}</Text>
          <Text style={styles.meta}>Impostos: {moneyCurrency(impostosNum, moedaImpostos)}</Text>
          {moedasIguais ? (
            <Text style={styles.title}>
              Total {moedaSymbol(moedaProdutos)}: {moneyCurrency(totalMoeda, moedaProdutos)}
            </Text>
          ) : null}
          {temEstrangeira && cambioTotalValido ? (
            <Text style={styles.title}>Total em R$: {money(totalBrl)}</Text>
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
              A proporção usa só o valor dos produtos. Frete e impostos entram nessa mesma proporção, cada um na sua moeda.
            </Text>
            {preview.itens.map((item) => (
              <View key={item.insumoId} style={styles.card}>
                <Text style={styles.title}>{item.insumoNome}</Text>
                <Text style={styles.meta}>
                  {numberPt(item.qtd)} × {moneyCurrency(item.precoUnitario, previewProdutos)} ={' '}
                  {moneyCurrency(item.subtotal, previewProdutos)}
                </Text>
                <Text style={styles.meta}>Proporção: {percentPt(item.proporcao)}</Text>
                <Text style={styles.meta}>
                  Frete: {moneyCurrency(item.freteRateado, previewFrete)} · Impostos:{' '}
                  {moneyCurrency(item.impostoRateado, previewImpostos)}
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

function CurrencyChips({
  value,
  onChange,
  options = MOEDAS,
}: {
  value: MoedaCodigo;
  onChange: (value: MoedaCodigo) => void;
  options?: readonly MoedaCodigo[];
}) {
  return (
    <View style={styles.chips}>
      {options.map((option) => (
        <Pressable
          key={option}
          onPress={() => onChange(option)}
          style={[styles.chip, value === option && styles.chipActive]}
        >
          <Text style={[styles.chipText, value === option && styles.chipTextActive]}>{moedaSymbol(option)}</Text>
        </Pressable>
      ))}
    </View>
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
