import React, { useCallback } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import { Card, EmptyState, ErrorBanner, Loading, PrimaryButton, Screen } from '../components/ui';
import { dateTimePt, money, numberPt } from '../format';
import { confirmDelete, showError, useApiLoader } from '../hooks';
import { colors, spacing } from '../theme';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'ProdutoDetalhe'>;

export function ProdutoDetalheScreen({ navigation, route }: Props) {
  const loader = useCallback(
    (cfg: Parameters<typeof api.getProduto>[0]) => api.getProduto(cfg, route.params.id),
    [route.params.id],
  );
  const { data, loading, error, config } = useApiLoader(loader);

  const onDelete = () => {
    if (!data) return;
    confirmDelete(`Excluir o produto #${data.id}? Os insumos voltam ao estoque.`, async () => {
      try {
        await api.deleteProduto(config, data.id);
        navigation.goBack();
      } catch (e) {
        showError(e);
      }
    });
  };

  if (loading && !data) return <Loading />;
  if (error) {
    return (
      <Screen>
        <ErrorBanner message={error} />
      </Screen>
    );
  }
  if (!data) return <EmptyState title="Produto não encontrado" />;

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <Card>
          <Text style={styles.title}>
            #{data.id} · {data.equipamentoNome}
          </Text>
          {data.descricaoAdicional ? <Text style={styles.meta}>{data.descricaoAdicional}</Text> : null}
          <Text style={styles.meta}>{dateTimePt(data.data)}</Text>
          <Text style={styles.total}>{money(data.custoTotal)}</Text>
        </Card>

        <Text style={styles.section}>Insumos (snapshot)</Text>
        {data.insumos.map((item, idx) => (
          <Card key={`${item.insumoId}-${idx}`}>
            <Text style={styles.itemTitle}>{item.insumoNome}</Text>
            <View style={styles.row}>
              <Text style={styles.meta}>
                {numberPt(item.qtd)} × {money(item.precoUnitario)}
              </Text>
              <Text style={styles.sub}>{money(item.subtotal)}</Text>
            </View>
          </Card>
        ))}

        <View style={styles.pad}>
          <PrimaryButton title="Excluir produto" onPress={onDelete} danger />
        </View>
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { paddingBottom: spacing.xl, paddingTop: spacing.sm },
  title: { fontSize: 18, fontWeight: '700', color: colors.text },
  meta: { marginTop: 2, color: colors.textMuted },
  total: { marginTop: spacing.sm, fontSize: 20, fontWeight: '800', color: colors.primaryDark },
  section: {
    marginHorizontal: spacing.md,
    marginTop: spacing.sm,
    marginBottom: spacing.sm,
    fontWeight: '700',
    color: colors.textMuted,
    textTransform: 'uppercase',
    fontSize: 12,
  },
  itemTitle: { fontWeight: '700', color: colors.text },
  row: { flexDirection: 'row', justifyContent: 'space-between', marginTop: spacing.sm },
  sub: { fontWeight: '700', color: colors.text },
  pad: { marginHorizontal: spacing.md, marginTop: spacing.md },
});
