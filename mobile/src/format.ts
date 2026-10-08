export const MOEDAS = ['BRL', 'USD', 'EUR', 'GBP', 'CNY'] as const;
export type MoedaCodigo = (typeof MOEDAS)[number];

export function money(value: number | null | undefined): string {
  return moneyCurrency(value, 'BRL');
}

export function moedaSymbol(moeda?: string | null): string {
  switch ((moeda ?? 'BRL').toUpperCase()) {
    case 'USD':
      return 'US$';
    case 'EUR':
      return '€';
    case 'GBP':
      return '£';
    case 'CNY':
      return 'CN¥';
    default:
      return 'R$';
  }
}

export function moneyCurrency(value: number | null | undefined, moeda?: string | null): string {
  const n = Number(value ?? 0);
  const code = (moeda ?? 'BRL').toUpperCase();
  if (code === 'BRL') {
    return n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }
  const formatted = n.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  return `${moedaSymbol(code)} ${formatted}`;
}

export function percentPt(ratio: number | null | undefined): string {
  const n = Number(ratio ?? 0);
  return n.toLocaleString('pt-BR', { style: 'percent', minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export function numberPt(value: number | null | undefined, digits = 4): string {
  const n = Number(value ?? 0);
  return n.toLocaleString('pt-BR', {
    minimumFractionDigits: 0,
    maximumFractionDigits: digits,
  });
}

export function dateTimePt(iso: string | null | undefined): string {
  if (!iso) return '—';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString('pt-BR');
}
