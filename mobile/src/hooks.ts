import { useCallback, useState } from 'react';
import { Alert } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { ApiError } from './api/client';
import { useSettings } from './context/SettingsContext';

export function useApiLoader<T>(loader: (cfg: ReturnType<typeof useSettings>['config']) => Promise<T>) {
  const { config, ready } = useSettings();
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    if (!ready) return;
    setLoading(true);
    setError(null);
    try {
      setData(await loader(config));
    } catch (e) {
      setData(null);
      setError(e instanceof ApiError ? e.message : 'Erro inesperado.');
    } finally {
      setLoading(false);
    }
  }, [config, loader, ready]);

  useFocusEffect(
    useCallback(() => {
      void reload();
    }, [reload]),
  );

  return { data, loading, error, reload, config };
}

export function confirmDelete(message: string, onConfirm: () => void) {
  Alert.alert('Confirmar exclusão', message, [
    { text: 'Cancelar', style: 'cancel' },
    { text: 'Excluir', style: 'destructive', onPress: onConfirm },
  ]);
}

export function showError(e: unknown) {
  const message = e instanceof ApiError ? e.message : 'Erro inesperado.';
  Alert.alert('Erro', message);
}
