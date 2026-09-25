import React, { useCallback, useLayoutEffect } from 'react';
import { FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import type { Tipo } from '../api/types';
import {
  Card,
  EmptyState,
  ErrorBanner,
  Loading,
  Screen,
} from '../components/ui';
import { confirmDelete, showError, useApiLoader } from '../hooks';
import { colors, spacing } from '../theme';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'Tipos'>;

export function TiposScreen({ navigation }: Props) {
  const loader = useCallback((cfg: Parameters<typeof api.getTipos>[0]) => api.getTipos(cfg), []);
  const { data, loading, error, reload, config } = useApiLoader(loader);

  useLayoutEffect(() => {
    navigation.setOptions({
      headerRight: () => (
        <Pressable onPress={() => navigation.navigate('TipoForm', {})}>
          <Text style={styles.headerAction}>Novo</Text>
        </Pressable>
      ),
    });
  }, [navigation]);

  const onDelete = (item: Tipo) => {
    confirmDelete(`Excluir o tipo "${item.nome}"?`, async () => {
      try {
        await api.deleteTipo(config, item.id);
        await reload();
      } catch (e) {
        showError(e);
      }
    });
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
        ListEmptyComponent={<EmptyState title="Nenhum tipo cadastrado" />}
        renderItem={({ item }) => (
          <Card>
            <Pressable onPress={() => navigation.navigate('TipoForm', { id: item.id, nome: item.nome })}>
              <Text style={styles.title}>{item.nome}</Text>
              <Text style={styles.meta}>{item.insumosCount} insumos</Text>
            </Pressable>
            <View style={styles.actions}>
              <Pressable onPress={() => navigation.navigate('TipoForm', { id: item.id, nome: item.nome })}>
                <Text style={styles.link}>Editar</Text>
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
  headerAction: { color: colors.primary, fontWeight: '700', fontSize: 16, paddingHorizontal: 8 },
  title: { fontSize: 17, fontWeight: '700', color: colors.text },
  meta: { marginTop: 2, color: colors.textMuted },
  actions: { flexDirection: 'row', gap: spacing.lg, marginTop: spacing.md },
  link: { color: colors.primary, fontWeight: '700' },
  danger: { color: colors.danger, fontWeight: '700' },
});
