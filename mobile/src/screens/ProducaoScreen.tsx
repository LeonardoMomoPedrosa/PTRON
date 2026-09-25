import React, { useCallback, useEffect, useState } from 'react';
import { Alert, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { api } from '../api';
import type { EquipamentoList, ProducaoPreview } from '../api/types';
import {
  Card,
  EmptyState,
  ErrorBanner,
  FormField,
  Loading,
  PrimaryButton,
  Screen,
} from '../components/ui';
import { useSettings } from '../context/SettingsContext';
import { money, numberPt } from '../format';
import { showError } from '../hooks';
import { colors, spacing } from '../theme';
import type { RootStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<RootStackParamList, 'Producao'>;

export function ProducaoScreen({ navigation }: Props) {
  const { config } = useSettings();
  const [loading, setLoading] = useState(true);
  const [equipamentos, setEquipamentos] = useState<EquipamentoList[]>([]);
  const [selectedId, setSelectedId] = useState(0);
  const [preview, setPreview] = useState<ProducaoPreview | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [descricao, setDescricao] = useState('');
  const [producing, setProducing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadEquipamentos = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setEquipamentos(await api.getEquipamentos(config));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar equipamentos');
    } finally {
      setLoading(false);
    }
  }, [config]);

  useEffect(() => {
    void loadEquipamentos();
  }, [loadEquipamentos]);

  const loadPreview = async (id: number) => {
    setSelectedId(id);
    setPreview(null);
    if (!id) return;
    setPreviewLoading(true);
    try {
      setPreview(await api.getProducaoPreview(config, id));
    } catch (e) {
      showError(e);
    } finally {
      setPreviewLoading(false);
    }
  };

  const produzir = async () => {
    if (!preview?.podeProduzir) {
      Alert.alert('Indisponível', 'Há insumos faltantes para produzir.');
      return;
    }
    setProducing(true);
    try {
      const produto = await api.produzir(config, preview.equipamentoId, descricao || undefined);
      Alert.alert('Produzido', `Produto #${produto.id} criado.`);
      navigation.navigate('ProdutoDetalhe', { id: produto.id });
    } catch (e) {
      showError(e);
    } finally {
      setProducing(false);
    }
  };

  if (loading) return <Loading />;

  return (
    <Screen>
      <ScrollView contentContainerStyle={styles.content}>
        {error ? <ErrorBanner message={error} /> : null}
        <Text style={styles.section}>Equipamento</Text>
        <View style={styles.chips}>
          {equipamentos.map((e) => (
            <Pressable
              key={e.id}
              onPress={() => void loadPreview(e.id)}
              style={[styles.chip, selectedId === e.id && styles.chipActive]}
            >
              <Text style={[styles.chipText, selectedId === e.id && styles.chipTextActive]}>
                {e.nome}
              </Text>
            </Pressable>
          ))}
        </View>
        {!equipamentos.length ? <EmptyState title="Cadastre um equipamento primeiro" /> : null}

        {previewLoading ? <Loading /> : null}

        {preview ? (
          <>
            <Card>
              <Text style={styles.title}>{preview.equipamentoNome}</Text>
              <Text style={styles.meta}>Custo estimado: {money(preview.custoEstimado)}</Text>
              <Text style={preview.podeProduzir ? styles.ok : styles.fail}>
                {preview.podeProduzir ? 'Pronto para produzir' : 'Insumos insuficientes'}
              </Text>
            </Card>

            {preview.faltantes.length > 0 ? (
              <Card style={styles.warnCard}>
                <Text style={styles.warnTitle}>Faltantes</Text>
                {preview.faltantes.map((f) => (
                  <Text key={f.insumoId} style={styles.warnItem}>
                    {f.insumoNome}: falta {numberPt(f.faltante)}
                  </Text>
                ))}
              </Card>
            ) : null}

            {preview.linhas.map((l) => (
              <Card key={l.insumoId}>
                <Text style={styles.itemTitle}>{l.insumoNome}</Text>
                <Text style={styles.meta}>
                  Necessário {numberPt(l.qtdNecessaria)} · Disponível {numberPt(l.saldoDisponivel)}
                </Text>
                <Text style={styles.meta}>
                  {money(l.custoUnitario)} · Subtotal {money(l.subtotal)}
                </Text>
                <Text style={l.disponivel ? styles.ok : styles.fail}>
                  {l.disponivel ? 'OK' : `Falta ${numberPt(l.faltante)}`}
                </Text>
              </Card>
            ))}

            <FormField
              label="Descrição adicional"
              value={descricao}
              onChangeText={setDescricao}
              placeholder="Opcional"
              multiline
            />
            <PrimaryButton
              title={producing ? 'Produzindo…' : 'Produzir'}
              onPress={produzir}
              disabled={producing || !preview.podeProduzir}
            />
          </>
        ) : null}
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { paddingBottom: spacing.xl, paddingTop: spacing.sm },
  section: {
    marginHorizontal: spacing.md,
    marginBottom: spacing.sm,
    fontWeight: '700',
    color: colors.textMuted,
    textTransform: 'uppercase',
    fontSize: 12,
  },
  chips: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
    marginHorizontal: spacing.md,
    marginBottom: spacing.md,
  },
  chip: {
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    borderRadius: 999,
    paddingHorizontal: 12,
    paddingVertical: 8,
  },
  chipActive: { backgroundColor: colors.primary, borderColor: colors.primary },
  chipText: { color: colors.text, fontSize: 13 },
  chipTextActive: { color: '#fff', fontWeight: '700' },
  title: { fontSize: 18, fontWeight: '700', color: colors.text },
  meta: { marginTop: 4, color: colors.textMuted },
  ok: { marginTop: 8, color: colors.success, fontWeight: '700' },
  fail: { marginTop: 8, color: colors.danger, fontWeight: '700' },
  warnCard: { backgroundColor: colors.warningBg, borderColor: '#FEC84B' },
  warnTitle: { fontWeight: '800', color: colors.warning, marginBottom: 6 },
  warnItem: { color: colors.warning, marginBottom: 2 },
  itemTitle: { fontWeight: '700', color: colors.text },
});
