export function money(value: number | null | undefined): string {
  return moneyCurrency(value, 'BRL');
}

export function moneyCurrency(value: number | null | undefined, moeda?: string | null): string {
  const n = Number(value ?? 0);
  if (moeda === 'USD') {
    return `US$ ${n.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }
  return n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
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
