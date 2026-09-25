import React, { useCallback, useLayoutEffect } from 'react';
import { FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import { Card, EmptyState, ErrorBanner, Loading, Screen } from '../components/ui';
import { dateTimePt, money } from '../format';
import { useApiLoader } from '../hooks';
import { colors, spacing } from '../theme';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'Entradas'>;

export function EntradasScreen({ navigation }: Props) {
  const loader = useCallback((cfg: Parameters<typeof api.getEntradas>[0]) => api.getEntradas(cfg), []);
  const { data, loading, error, reload } = useApiLoader(loader);

  useLayoutEffect(() => {
    navigation.setOptions({
      headerRight: () => (
        <Pressable onPress={() => navigation.navigate('EntradaNova')}>
          <Text style={styles.headerAction}>Nova</Text>
        </Pressable>
      ),
    });
  }, [navigation]);

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
        ListEmptyComponent={<EmptyState title="Nenhuma entrada" subtitle="Registre uma nova entrada de estoque." />}
        renderItem={({ item }) => (
          <Card>
            <Pressable onPress={() => navigation.navigate('EntradaDetalhe', { id: item.id })}>
              <Text style={styles.title}>Entrada #{item.id}</Text>
              <Text style={styles.meta}>{dateTimePt(item.data)}</Text>
              <View style={styles.row}>
                <Text style={styles.meta}>{item.itens?.length ?? 0} itens</Text>
                <Text style={styles.total}>{money(item.total)}</Text>
              </View>
            </Pressable>
          </Card>
        )}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  flex: { flexGrow: 1 },
  headerAction: { color: colors.primary, fontWeight: '700', fontSize: 16, paddingHorizontal: 8 },
  title: { fontSize: 16, fontWeight: '700', color: colors.text },
  meta: { marginTop: 2, color: colors.textMuted },
  row: { flexDirection: 'row', justifyContent: 'space-between', marginTop: spacing.sm },
  total: { fontWeight: '700', color: colors.primaryDark },
});
