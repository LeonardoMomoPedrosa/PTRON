export type ApiInfo = { name: string; version: string };

export type Tipo = {
  id: number;
  nome: string;
  insumosCount: number;
};

export type TipoWrite = { nome: string };

export type Insumo = {
  id: number;
  tipoInsumoId: number;
  tipoNome: string;
  nome: string;
  valor?: string | null;
  potencia?: string | null;
  voltagem?: string | null;
  saldo: number;
  custoUnitario: number;
  fotoPath?: string | null;
};

export type InsumoWrite = {
  tipoInsumoId: number;
  nome: string;
  valor?: string | null;
  potencia?: string | null;
  voltagem?: string | null;
  fotoPath?: string | null;
};

export type EquipamentoList = {
  id: number;
  nome: string;
  fotoPath?: string | null;
  insumosCount: number;
};

export type BomItem = {
  insumoId: number;
  insumoNome: string;
  tipoNome?: string | null;
  qtd: number;
};

export type Equipamento = {
  id: number;
  nome: string;
  fotoPath?: string | null;
  insumos: BomItem[];
};

export type EquipamentoWrite = {
  nome: string;
  fotoPath?: string | null;
  insumos: { insumoId: number; qtd: number }[];
};

export type EntradaItem = {
  insumoId: number;
  insumoNome: string;
  tipoNome?: string | null;
  qtd: number;
  precoUnitario: number;
  subtotal: number;
};

export type EntradaEstoque = {
  id: number;
  data: string;
  total: number;
  itens: EntradaItem[];
};

export type EntradaWrite = {
  itens: { insumoId: number; qtd: number; precoUnitario: number }[];
};

export type BomLinhaPreview = {
  insumoId: number;
  insumoNome: string;
  qtdNecessaria: number;
  saldoDisponivel: number;
  custoUnitario: number;
  subtotal: number;
  faltante: number;
  disponivel: boolean;
};

export type ProducaoPreview = {
  equipamentoId: number;
  equipamentoNome: string;
  linhas: BomLinhaPreview[];
  custoEstimado: number;
  podeProduzir: boolean;
  faltantes: BomLinhaPreview[];
};

export type ProdutoList = {
  id: number;
  equipamentoId: number;
  equipamentoNome: string;
  descricaoAdicional?: string | null;
  data: string;
  custoTotal: number;
};

export type ProdutoInsumo = {
  insumoId: number;
  insumoNome: string;
  qtd: number;
  precoUnitario: number;
  subtotal: number;
};

export type Produto = {
  id: number;
  equipamentoId: number;
  equipamentoNome: string;
  descricaoAdicional?: string | null;
  data: string;
  custoTotal: number;
  insumos: ProdutoInsumo[];
};
