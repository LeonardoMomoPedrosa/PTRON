import React, { useCallback, useLayoutEffect } from 'react';
import { FlatList, Image, Pressable, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import { photoUrl } from '../api/client';
import type { EquipamentoList } from '../api/types';
import { Card, EmptyState, ErrorBanner, Loading, Screen } from '../components/ui';
import { confirmDelete, showError, useApiLoader } from '../hooks';
import { colors, spacing } from '../theme';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'Equipamentos'>;

export function EquipamentosScreen({ navigation }: Props) {
  const loader = useCallback((cfg: Parameters<typeof api.getEquipamentos>[0]) => api.getEquipamentos(cfg), []);
  const { data, loading, error, reload, config } = useApiLoader(loader);

  useLayoutEffect(() => {
    navigation.setOptions({
      headerRight: () => (
        <Pressable onPress={() => navigation.navigate('EquipamentoForm', {})}>
          <Text style={styles.headerAction}>Novo</Text>
        </Pressable>
      ),
    });
  }, [navigation]);

  const onDelete = (item: EquipamentoList) => {
    confirmDelete(`Excluir o equipamento "${item.nome}"?`, async () => {
      try {
        await api.deleteEquipamento(config, item.id);
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
        ListEmptyComponent={<EmptyState title="Nenhum equipamento" />}
        renderItem={({ item }) => {
          const uri = photoUrl(config, item.fotoPath);
          return (
            <Card>
              <Pressable
                style={styles.row}
                onPress={() => navigation.navigate('EquipamentoForm', { id: item.id })}
              >
                {uri ? (
                  <Image source={{ uri }} style={styles.thumb} />
                ) : (
                  <View style={[styles.thumb, styles.thumbEmpty]} />
                )}
                <View style={styles.flexGrow}>
                  <Text style={styles.title}>{item.nome}</Text>
                  <Text style={styles.meta}>{item.insumosCount} itens na BOM</Text>
                </View>
              </Pressable>
              <View style={styles.actions}>
                <Pressable onPress={() => navigation.navigate('EquipamentoForm', { id: item.id })}>
                  <Text style={styles.link}>Editar</Text>
                </Pressable>
                <Pressable onPress={() => onDelete(item)}>
                  <Text style={styles.danger}>Excluir</Text>
                </Pressable>
              </View>
            </Card>
          );
        }}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  flex: { flexGrow: 1 },
  headerAction: { color: colors.primary, fontWeight: '700', fontSize: 16, paddingHorizontal: 8 },
  row: { flexDirection: 'row', gap: spacing.md, alignItems: 'center' },
  thumb: { width: 56, height: 56, borderRadius: 8, backgroundColor: colors.chip },
  thumbEmpty: { borderWidth: 1, borderColor: colors.border },
  flexGrow: { flex: 1 },
  title: { fontSize: 16, fontWeight: '700', color: colors.text },
  meta: { marginTop: 2, color: colors.textMuted },
  actions: { flexDirection: 'row', gap: spacing.lg, marginTop: spacing.md },
  link: { color: colors.primary, fontWeight: '700' },
  danger: { color: colors.danger, fontWeight: '700' },
});
