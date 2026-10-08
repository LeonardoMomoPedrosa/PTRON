import React, { useCallback } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import { Card, EmptyState, ErrorBanner, Loading, Screen } from '../components/ui';
import { dateTimePt, money, moneyCurrency, moedaSymbol, numberPt, percentPt } from '../format';
import { useApiLoader } from '../hooks';
import { colors, spacing } from '../theme';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'EntradaDetalhe'>;

export function EntradaDetalheScreen({ route }: Props) {
  const loader = useCallback(
    (cfg: Parameters<typeof api.getEntrada>[0]) => api.getEntrada(cfg, route.params.id),
    [route.params.id],
  );
  const { data, loading, error } = useApiLoader(loader);

  if (loading && !data) return <Loading />;
  if (error) {
    return (
      <Screen>
        <ErrorBanner message={error} />
      </Screen>
    );
  }
  if (!data) return <EmptyState title="Entrada não encontrada" />;

  const moedaProdutos = data.moedaProdutos || data.moeda;
  const moedaImpostos = data.moedaImpostos || moedaProdutos;
  const moedaFrete = data.moedaFrete || moedaProdutos;
  const cambioProdutos = data.cambioProdutos ?? data.cambio;
  const cambioImpostos = data.cambioImpostos ?? cambioProdutos;

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <Card>
          <Text style={styles.title}>Entrada #{data.id}</Text>
          <Text style={styles.meta}>{dateTimePt(data.data)}</Text>
          <Text style={styles.meta}>Produtos: {moneyCurrency(data.totalProdutos, moedaProdutos)}</Text>
          {moedaProdutos !== 'BRL' ? (
            <Text style={styles.meta}>
              Câmbio produtos: {money(cambioProdutos)} / {moedaSymbol(moedaProdutos)} 1
            </Text>
          ) : null}
          <Text style={styles.meta}>Frete: {moneyCurrency(data.frete, moedaFrete)}</Text>
          <Text style={styles.meta}>Impostos: {moneyCurrency(data.impostos, moedaImpostos)}</Text>
          {moedaImpostos !== 'BRL' && moedaImpostos !== moedaProdutos ? (
            <Text style={styles.meta}>
              Câmbio impostos: {money(cambioImpostos)} / {moedaSymbol(moedaImpostos)} 1
            </Text>
          ) : null}
          <Text style={styles.total}>{money(data.total)}</Text>
        </Card>
        {data.itens.map((item, idx) => (
          <Card key={`${item.insumoId}-${idx}`}>
            <Text style={styles.itemTitle}>{item.insumoNome}</Text>
            <Text style={styles.meta}>{item.tipoNome}</Text>
            <View style={styles.row}>
              <Text style={styles.meta}>
                {numberPt(item.qtd)} × {moneyCurrency(item.precoUnitario, moedaProdutos)}
              </Text>
              <Text style={styles.sub}>{moneyCurrency(item.subtotal, moedaProdutos)}</Text>
            </View>
            <Text style={styles.meta}>Proporção: {percentPt(item.proporcao)}</Text>
            <Text style={styles.meta}>
              Frete {moneyCurrency(item.freteRateado, moedaFrete)} · Impostos{' '}
              {moneyCurrency(item.impostoRateado, moedaImpostos)}
            </Text>
            <Text style={styles.sub}>Custo unit. {money(item.custoUnitario)}</Text>
          </Card>
        ))}
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { paddingBottom: spacing.xl, paddingTop: spacing.sm },
  title: { fontSize: 18, fontWeight: '700', color: colors.text },
  meta: { marginTop: 2, color: colors.textMuted },
  total: { marginTop: spacing.sm, fontSize: 20, fontWeight: '800', color: colors.primaryDark },
  itemTitle: { fontWeight: '700', color: colors.text },
  row: { flexDirection: 'row', justifyContent: 'space-between', marginTop: spacing.sm },
  sub: { fontWeight: '700', color: colors.text },
});
