import React, { useCallback, useLayoutEffect, useMemo, useState } from 'react';
import { FlatList, Image, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import { photoUrl } from '../api/client';
import type { Insumo } from '../api/types';
import { Card, EmptyState, ErrorBanner, Loading, Screen } from '../components/ui';
import { money, numberPt } from '../format';
import { confirmDelete, showError, useApiLoader } from '../hooks';
import { colors, spacing } from '../theme';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'Insumos'>;

export function InsumosScreen({ navigation }: Props) {
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');

  const loader = useCallback(
    (cfg: Parameters<typeof api.getInsumos>[0]) => api.getInsumos(cfg, { search: query || undefined }),
    [query],
  );
  const { data, loading, error, reload, config } = useApiLoader(loader);

  useLayoutEffect(() => {
    navigation.setOptions({
      headerRight: () => (
        <Pressable onPress={() => navigation.navigate('InsumoForm', {})}>
          <Text style={styles.headerAction}>Novo</Text>
        </Pressable>
      ),
    });
  }, [navigation]);

  const onDelete = (item: Insumo) => {
    confirmDelete(`Excluir o insumo "${item.nome}"?`, async () => {
      try {
        await api.deleteInsumo(config, item.id);
        await reload();
      } catch (e) {
        showError(e);
      }
    });
  };

  const header = useMemo(
    () => (
      <View style={styles.searchWrap}>
        <TextInput
          value={search}
          onChangeText={setSearch}
          placeholder="Buscar por nome, tipo, valor…"
          placeholderTextColor={colors.textMuted}
          style={styles.search}
          returnKeyType="search"
          onSubmitEditing={() => setQuery(search.trim())}
        />
        <Pressable style={styles.searchBtn} onPress={() => setQuery(search.trim())}>
          <Text style={styles.searchBtnText}>Buscar</Text>
        </Pressable>
      </View>
    ),
    [search],
  );

  if (loading && !data) return <Loading />;

  return (
    <Screen>
      {error ? <ErrorBanner message={error} /> : null}
      <FlatList
        data={data ?? []}
        keyExtractor={(item) => String(item.id)}
        refreshing={loading}
        onRefresh={reload}
        ListHeaderComponent={header}
        contentContainerStyle={!data?.length ? styles.flex : { paddingBottom: spacing.xl }}
        ListEmptyComponent={<EmptyState title="Nenhum insumo encontrado" />}
        renderItem={({ item }) => {
          const uri = photoUrl(config, item.fotoPath);
          return (
            <Card>
              <Pressable
                style={styles.row}
                onPress={() => navigation.navigate('InsumoForm', { id: item.id })}
              >
                {uri ? (
                  <Image source={{ uri }} style={styles.thumb} />
                ) : (
                  <View style={[styles.thumb, styles.thumbEmpty]} />
                )}
                <View style={styles.body}>
                  <Text style={styles.title}>{item.nome}</Text>
                  <Text style={styles.meta}>{item.tipoNome}</Text>
                  <Text style={styles.meta}>
                    {[item.valor, item.potencia, item.voltagem].filter(Boolean).join(' · ') || '—'}
                  </Text>
                  <Text style={styles.stock}>
                    Saldo {numberPt(item.saldo)} · {money(item.custoUnitario)}
                  </Text>
                </View>
              </Pressable>
              <View style={styles.actions}>
                <Pressable onPress={() => navigation.navigate('InsumoForm', { id: item.id })}>
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
  searchWrap: {
    flexDirection: 'row',
    gap: spacing.sm,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
  },
  search: {
    flex: 1,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    color: colors.text,
  },
  searchBtn: {
    backgroundColor: colors.primary,
    borderRadius: 10,
    paddingHorizontal: 14,
    justifyContent: 'center',
  },
  searchBtnText: { color: '#fff', fontWeight: '700' },
  row: { flexDirection: 'row', gap: spacing.md },
  thumb: { width: 56, height: 56, borderRadius: 8, backgroundColor: colors.chip },
  thumbEmpty: { borderWidth: 1, borderColor: colors.border },
  body: { flex: 1 },
  title: { fontSize: 16, fontWeight: '700', color: colors.text },
  meta: { color: colors.textMuted, marginTop: 2, fontSize: 13 },
  stock: { marginTop: 6, fontWeight: '600', color: colors.primaryDark },
  actions: { flexDirection: 'row', gap: spacing.lg, marginTop: spacing.md },
  link: { color: colors.primary, fontWeight: '700' },
  danger: { color: colors.danger, fontWeight: '700' },
});
