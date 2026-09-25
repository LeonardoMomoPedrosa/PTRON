import React, { useCallback, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import { ApiError } from '../api/client';
import { Card, EmptyState, ErrorBanner, Loading, PrimaryButton, Screen } from '../components/ui';
import { useSettings } from '../context/SettingsContext';
import { colors, spacing } from '../theme';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'Home'>;

const shortcuts: { title: string; subtitle: string; route: keyof RootStackParamList }[] = [
  { title: 'Tipos', subtitle: 'Tipos de componente', route: 'Tipos' },
  { title: 'Insumos', subtitle: 'Componentes e estoque', route: 'Insumos' },
  { title: 'Equipamentos', subtitle: 'Modelos e BOM', route: 'Equipamentos' },
  { title: 'Entradas', subtitle: 'Entrada de estoque', route: 'Entradas' },
  { title: 'Produção', subtitle: 'Produzir a partir do modelo', route: 'Producao' },
  { title: 'Produtos', subtitle: 'Itens produzidos', route: 'Produtos' },
];

export function HomeScreen({ navigation }: Props) {
  const { config, ready } = useSettings();
  const [status, setStatus] = useState<'idle' | 'ok' | 'fail'>('idle');
  const [message, setMessage] = useState<string | null>(null);
  const [testing, setTesting] = useState(false);

  const testConnection = useCallback(async () => {
    if (!ready) return;
    setTesting(true);
    setMessage(null);
    try {
      const info = await api.ping(config);
      setStatus('ok');
      setMessage(`${info.name} v${info.version} — conectado`);
    } catch (e) {
      setStatus('fail');
      setMessage(e instanceof ApiError ? e.message : 'Falha na conexão');
    } finally {
      setTesting(false);
    }
  }, [config, ready]);

  if (!ready) return <Loading />;

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        <Text style={styles.brand}>PTRON</Text>
        <Text style={styles.tagline}>Controle de produção no celular</Text>

        <Card style={styles.statusCard}>
          <Text style={styles.statusLabel}>Servidor</Text>
          <Text style={styles.statusUrl} numberOfLines={2}>
            {config.baseUrl}
          </Text>
          {message ? (
            <Text style={status === 'ok' ? styles.ok : styles.fail}>{message}</Text>
          ) : (
            <Text style={styles.muted}>Toque em Testar para verificar a API.</Text>
          )}
          <View style={styles.row}>
            <View style={styles.flex}>
              <PrimaryButton
                title={testing ? 'Testando…' : 'Testar conexão'}
                onPress={testConnection}
                disabled={testing}
              />
            </View>
            <View style={styles.flex}>
              <Pressable
                style={styles.linkBtn}
                onPress={() => navigation.navigate('Settings')}
              >
                <Text style={styles.linkText}>Configurar</Text>
              </Pressable>
            </View>
          </View>
        </Card>

        {status === 'fail' && message ? <ErrorBanner message={message} /> : null}

        <Text style={styles.section}>Áreas</Text>
        {shortcuts.map((item) => (
          <Pressable
            key={item.route}
            onPress={() => navigation.navigate(item.route as never)}
            style={({ pressed }) => [styles.tile, pressed && styles.tilePressed]}
          >
            <Text style={styles.tileTitle}>{item.title}</Text>
            <Text style={styles.tileSubtitle}>{item.subtitle}</Text>
          </Pressable>
        ))}
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { paddingBottom: spacing.xl },
  brand: {
    marginTop: spacing.lg,
    marginHorizontal: spacing.md,
    fontSize: 34,
    fontWeight: '800',
    color: colors.primaryDark,
    letterSpacing: 1,
  },
  tagline: {
    marginHorizontal: spacing.md,
    marginBottom: spacing.lg,
    color: colors.textMuted,
    fontSize: 15,
  },
  statusCard: { marginTop: 0 },
  statusLabel: { fontSize: 12, fontWeight: '700', color: colors.textMuted, textTransform: 'uppercase' },
  statusUrl: { marginTop: 4, fontSize: 15, color: colors.text, fontWeight: '600' },
  muted: { marginTop: spacing.sm, color: colors.textMuted, fontSize: 13 },
  ok: { marginTop: spacing.sm, color: colors.success, fontWeight: '600' },
  fail: { marginTop: spacing.sm, color: colors.danger, fontWeight: '600' },
  row: { flexDirection: 'row', gap: spacing.sm, marginTop: spacing.md },
  flex: { flex: 1 },
  linkBtn: {
    height: 48,
    borderRadius: 10,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.surface,
  },
  linkText: { color: colors.primary, fontWeight: '700' },
  section: {
    marginHorizontal: spacing.md,
    marginTop: spacing.md,
    marginBottom: spacing.sm,
    fontSize: 13,
    fontWeight: '700',
    color: colors.textMuted,
    textTransform: 'uppercase',
  },
  tile: {
    backgroundColor: colors.surface,
    marginHorizontal: spacing.md,
    marginBottom: spacing.sm,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
  },
  tilePressed: { opacity: 0.75 },
  tileTitle: { fontSize: 17, fontWeight: '700', color: colors.text },
  tileSubtitle: { marginTop: 2, color: colors.textMuted, fontSize: 13 },
});
