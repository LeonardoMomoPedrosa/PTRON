import React, { useCallback } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import { Card, EmptyState, ErrorBanner, Loading, Screen } from '../components/ui';
import { dateTimePt, money, numberPt } from '../format';
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

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <Card>
          <Text style={styles.title}>Entrada #{data.id}</Text>
          <Text style={styles.meta}>{dateTimePt(data.data)}</Text>
          <Text style={styles.total}>{money(data.total)}</Text>
        </Card>
        {data.itens.map((item, idx) => (
          <Card key={`${item.insumoId}-${idx}`}>
            <Text style={styles.itemTitle}>{item.insumoNome}</Text>
            <Text style={styles.meta}>{item.tipoNome}</Text>
            <View style={styles.row}>
              <Text style={styles.meta}>
                {numberPt(item.qtd)} × {money(item.precoUnitario)}
              </Text>
              <Text style={styles.sub}>{money(item.subtotal)}</Text>
            </View>
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
