export function money(value: number | null | undefined): string {
  const n = Number(value ?? 0);
  return n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
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
