import React, { useCallback } from 'react';
import { FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import type { ProdutoList } from '../api/types';
import { Card, EmptyState, ErrorBanner, Loading, Screen } from '../components/ui';
import { dateTimePt, money } from '../format';
import { confirmDelete, showError, useApiLoader } from '../hooks';
import { colors, spacing } from '../theme';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'Produtos'>;

export function ProdutosScreen({ navigation }: Props) {
  const loader = useCallback((cfg: Parameters<typeof api.getProdutos>[0]) => api.getProdutos(cfg), []);
  const { data, loading, error, reload, config } = useApiLoader(loader);

  const onDelete = (item: ProdutoList) => {
    confirmDelete(
      `Excluir o produto #${item.id}? Os insumos voltam ao estoque.`,
      async () => {
        try {
          await api.deleteProduto(config, item.id);
          await reload();
        } catch (e) {
          showError(e);
        }
      },
    );
  };

  if (loading && !data) return <Loading />;

  return (
    <Screen>
      {error ? <ErrorBanner message={error} /> : null}
      <FlatList
        data={data ?? []}
        keyExtractor={(item) => String(item.id)}
        refreshing={loading}
        onRefresh={reload}
        contentContainerStyle={!data?.length ? styles.flex : undefined}
        ListEmptyComponent={<EmptyState title="Nenhum produto produzido" />}
        renderItem={({ item }) => (
          <Card>
            <Pressable onPress={() => navigation.navigate('ProdutoDetalhe', { id: item.id })}>
              <Text style={styles.title}>
                #{item.id} · {item.equipamentoNome}
              </Text>
              {item.descricaoAdicional ? (
                <Text style={styles.meta}>{item.descricaoAdicional}</Text>
              ) : null}
              <View style={styles.row}>
                <Text style={styles.meta}>{dateTimePt(item.data)}</Text>
                <Text style={styles.total}>{money(item.custoTotal)}</Text>
              </View>
            </Pressable>
            <View style={styles.actions}>
              <Pressable onPress={() => navigation.navigate('ProdutoDetalhe', { id: item.id })}>
                <Text style={styles.link}>Detalhe</Text>
              </Pressable>
              <Pressable onPress={() => onDelete(item)}>
                <Text style={styles.danger}>Excluir</Text>
              </Pressable>
            </View>
          </Card>
        )}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  flex: { flexGrow: 1 },
  title: { fontSize: 16, fontWeight: '700', color: colors.text },
  meta: { marginTop: 2, color: colors.textMuted },
  row: { flexDirection: 'row', justifyContent: 'space-between', marginTop: spacing.sm },
  total: { fontWeight: '700', color: colors.primaryDark },
  actions: { flexDirection: 'row', gap: spacing.lg, marginTop: spacing.md },
  link: { color: colors.primary, fontWeight: '700' },
  danger: { color: colors.danger, fontWeight: '700' },
});
