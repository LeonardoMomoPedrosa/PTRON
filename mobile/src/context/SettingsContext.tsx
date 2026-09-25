import AsyncStorage from '@react-native-async-storage/async-storage';
import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ApiConfig } from '../api/client';

const STORAGE_KEY = 'ptron.settings.v1';

export const DEFAULT_BASE_URL = 'http://10.0.2.2:5083';
export const DEFAULT_TOKEN = 'ptron-dev-token-8f4c2a91';

type SettingsState = {
  baseUrl: string;
  token: string;
  ready: boolean;
  setBaseUrl: (value: string) => void;
  setToken: (value: string) => void;
  save: (next: { baseUrl: string; token: string }) => Promise<void>;
  config: ApiConfig;
};

const SettingsContext = createContext<SettingsState | null>(null);

export function SettingsProvider({ children }: { children: React.ReactNode }) {
  const [baseUrl, setBaseUrl] = useState(DEFAULT_BASE_URL);
  const [token, setToken] = useState(DEFAULT_TOKEN);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const raw = await AsyncStorage.getItem(STORAGE_KEY);
        if (raw) {
          const parsed = JSON.parse(raw) as { baseUrl?: string; token?: string };
          if (parsed.baseUrl) setBaseUrl(parsed.baseUrl);
          if (parsed.token) setToken(parsed.token);
        }
      } finally {
        setReady(true);
      }
    })();
  }, []);

  const save = useCallback(async (next: { baseUrl: string; token: string }) => {
    const clean = {
      baseUrl: next.baseUrl.trim().replace(/\/+$/, ''),
      token: next.token.trim(),
    };
    setBaseUrl(clean.baseUrl);
    setToken(clean.token);
    await AsyncStorage.setItem(STORAGE_KEY, JSON.stringify(clean));
  }, []);

  const value = useMemo(
    () => ({
      baseUrl,
      token,
      ready,
      setBaseUrl,
      setToken,
      save,
      config: { baseUrl, token },
    }),
    [baseUrl, token, ready, save],
  );

  return <SettingsContext.Provider value={value}>{children}</SettingsContext.Provider>;
}

export function useSettings(): SettingsState {
  const ctx = useContext(SettingsContext);
  if (!ctx) throw new Error('useSettings must be used within SettingsProvider');
  return ctx;
}
