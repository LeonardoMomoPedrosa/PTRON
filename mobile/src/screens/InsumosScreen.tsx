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

const DEFAULT_PAGE_SIZE = 25;
const MAX_PAGE_SIZE = 500;

export function InsumosScreen({ navigation }: Props) {
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [page, setPage] = useState(0);
  const [pageSizeInput, setPageSizeInput] = useState(String(DEFAULT_PAGE_SIZE));
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);

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

  const items = data ?? [];
  const pageCount = Math.max(1, Math.ceil(items.length / pageSize));
  const safePage = Math.min(page, pageCount - 1);
  const pageItems = items.slice(safePage * pageSize, (safePage + 1) * pageSize);
  const rangeStart = items.length === 0 ? 0 : safePage * pageSize + 1;
  const rangeEnd = Math.min(items.length, (safePage + 1) * pageSize);

  const applySearch = () => {
    setPage(0);
    setQuery(search.trim());
  };

  const applyPageSize = () => {
    const parsed = Number.parseInt(pageSizeInput, 10);
    const next = Number.isFinite(parsed) ? Math.min(MAX_PAGE_SIZE, Math.max(1, parsed)) : DEFAULT_PAGE_SIZE;
    setPageSize(next);
    setPageSizeInput(String(next));
    setPage(0);
  };

  const header = useMemo(
    () => (
      <View>
        <View style={styles.searchWrap}>
          <TextInput
            value={search}
            onChangeText={setSearch}
            placeholder="Buscar por nome, tipo, valor…"
            placeholderTextColor={colors.textMuted}
            style={styles.search}
            returnKeyType="search"
            onSubmitEditing={applySearch}
          />
          <Pressable style={styles.searchBtn} onPress={applySearch}>
            <Text style={styles.searchBtnText}>Buscar</Text>
          </Pressable>
        </View>
        <View style={styles.pageSizeWrap}>
          <Text style={styles.pageSizeLabel}>Itens por página</Text>
          <TextInput
            value={pageSizeInput}
            onChangeText={setPageSizeInput}
            keyboardType="number-pad"
            style={styles.pageSizeInput}
            returnKeyType="done"
            onSubmitEditing={applyPageSize}
          />
          <Pressable style={styles.searchBtn} onPress={applyPageSize}>
            <Text style={styles.searchBtnText}>Aplicar</Text>
          </Pressable>
        </View>
      </View>
    ),
    [search, pageSizeInput],
  );

  if (loading && !data) return <Loading />;

  return (
    <Screen>
      {error ? <ErrorBanner message={error} /> : null}
      <FlatList
        data={pageItems}
        keyExtractor={(item) => String(item.id)}
        refreshing={loading}
        onRefresh={reload}
        ListHeaderComponent={header}
        ListFooterComponent={
          items.length === 0 ? null : (
            <View style={styles.pager}>
              <Pressable
                disabled={safePage === 0}
                onPress={() => setPage(safePage - 1)}
                style={[styles.pageBtn, safePage === 0 && styles.pageBtnDisabled]}
              >
                <Text style={styles.pageBtnText}>Anterior</Text>
              </Pressable>
              <Text style={styles.pageInfo}>
                {rangeStart}-{rangeEnd} de {items.length}
              </Text>
              <Pressable
                disabled={safePage >= pageCount - 1}
                onPress={() => setPage(safePage + 1)}
                style={[styles.pageBtn, safePage >= pageCount - 1 && styles.pageBtnDisabled]}
              >
                <Text style={styles.pageBtnText}>Próxima</Text>
              </Pressable>
            </View>
          )
        }
        contentContainerStyle={!items.length ? styles.flex : { paddingBottom: spacing.xl }}
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
  pageSizeWrap: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    paddingHorizontal: spacing.md,
    paddingBottom: spacing.sm,
  },
  pageSizeLabel: { color: colors.textMuted, fontSize: 13 },
  pageSizeInput: {
    width: 72,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 8,
    color: colors.text,
    textAlign: 'center',
  },
  pager: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.md,
    gap: spacing.sm,
  },
  pageBtn: {
    backgroundColor: colors.primary,
    borderRadius: 10,
    paddingHorizontal: 14,
    paddingVertical: 10,
  },
  pageBtnDisabled: { opacity: 0.4 },
  pageBtnText: { color: '#fff', fontWeight: '700' },
  pageInfo: { color: colors.text, fontWeight: '600' },
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
