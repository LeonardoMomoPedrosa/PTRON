export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
    this.name = 'ApiError';
  }
}

export type ApiConfig = {
  baseUrl: string;
  token: string;
};

function joinUrl(baseUrl: string, path: string): string {
  const base = baseUrl.replace(/\/+$/, '');
  const p = path.startsWith('/') ? path : `/${path}`;
  return `${base}${p}`;
}

export async function apiRequest<T>(
  config: ApiConfig,
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const url = joinUrl(config.baseUrl, path);
  const headers = new Headers(options.headers);
  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json');
  }
  if (config.token) {
    headers.set('Authorization', `Bearer ${config.token}`);
  }

  const hasBody = options.body != null;
  if (hasBody && !(options.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  let response: Response;
  try {
    response = await fetch(url, { ...options, headers });
  } catch {
    throw new ApiError(0, 'Não foi possível conectar ao servidor. Verifique a URL e a rede.');
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  let data: unknown = null;
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = text;
    }
  }

  if (!response.ok) {
    const message =
      typeof data === 'object' && data && 'error' in data
        ? String((data as { error: string }).error)
        : typeof data === 'string' && data
          ? data
          : `Erro HTTP ${response.status}`;
    throw new ApiError(response.status, message);
  }

  return data as T;
}

export function photoUrl(config: ApiConfig, fotoPath?: string | null): string | null {
  if (!fotoPath) return null;
  if (fotoPath.startsWith('http://') || fotoPath.startsWith('https://')) return fotoPath;
  return joinUrl(config.baseUrl, fotoPath);
}
