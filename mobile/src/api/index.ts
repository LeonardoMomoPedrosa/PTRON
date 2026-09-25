import { apiRequest, type ApiConfig } from './client';
import type {
  ApiInfo,
  EntradaEstoque,
  EntradaWrite,
  Equipamento,
  EquipamentoList,
  EquipamentoWrite,
  Insumo,
  InsumoWrite,
  ProducaoPreview,
  Produto,
  ProdutoList,
  Tipo,
  TipoWrite,
} from './types';

export const api = {
  ping: (cfg: ApiConfig) => apiRequest<ApiInfo>(cfg, '/api'),

  getTipos: (cfg: ApiConfig) => apiRequest<Tipo[]>(cfg, '/api/tipos'),
  getTipo: (cfg: ApiConfig, id: number) => apiRequest<Tipo>(cfg, `/api/tipos/${id}`),
  createTipo: (cfg: ApiConfig, body: TipoWrite) =>
    apiRequest<Tipo>(cfg, '/api/tipos', { method: 'POST', body: JSON.stringify(body) }),
  updateTipo: (cfg: ApiConfig, id: number, body: TipoWrite) =>
    apiRequest<Tipo>(cfg, `/api/tipos/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteTipo: (cfg: ApiConfig, id: number) =>
    apiRequest<void>(cfg, `/api/tipos/${id}`, { method: 'DELETE' }),

  getInsumos: (cfg: ApiConfig, params?: { search?: string; tipoId?: number }) => {
    const q = new URLSearchParams();
    if (params?.search) q.set('search', params.search);
    if (params?.tipoId) q.set('tipoId', String(params.tipoId));
    const qs = q.toString();
    return apiRequest<Insumo[]>(cfg, `/api/insumos${qs ? `?${qs}` : ''}`);
  },
  getInsumo: (cfg: ApiConfig, id: number) => apiRequest<Insumo>(cfg, `/api/insumos/${id}`),
  createInsumo: (cfg: ApiConfig, body: InsumoWrite) =>
    apiRequest<Insumo>(cfg, '/api/insumos', { method: 'POST', body: JSON.stringify(body) }),
  updateInsumo: (cfg: ApiConfig, id: number, body: InsumoWrite) =>
    apiRequest<Insumo>(cfg, `/api/insumos/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  deleteInsumo: (cfg: ApiConfig, id: number) =>
    apiRequest<void>(cfg, `/api/insumos/${id}`, { method: 'DELETE' }),

  getEquipamentos: (cfg: ApiConfig) => apiRequest<EquipamentoList[]>(cfg, '/api/equipamentos'),
  getEquipamento: (cfg: ApiConfig, id: number) =>
    apiRequest<Equipamento>(cfg, `/api/equipamentos/${id}`),
  createEquipamento: (cfg: ApiConfig, body: EquipamentoWrite) =>
    apiRequest<Equipamento>(cfg, '/api/equipamentos', {
      method: 'POST',
      body: JSON.stringify(body),
    }),
  updateEquipamento: (cfg: ApiConfig, id: number, body: EquipamentoWrite) =>
    apiRequest<Equipamento>(cfg, `/api/equipamentos/${id}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    }),
  deleteEquipamento: (cfg: ApiConfig, id: number) =>
    apiRequest<void>(cfg, `/api/equipamentos/${id}`, { method: 'DELETE' }),

  getEntradas: (cfg: ApiConfig) => apiRequest<EntradaEstoque[]>(cfg, '/api/entradas'),
  getEntrada: (cfg: ApiConfig, id: number) =>
    apiRequest<EntradaEstoque>(cfg, `/api/entradas/${id}`),
  finalizarEntrada: (cfg: ApiConfig, body: EntradaWrite) =>
    apiRequest<EntradaEstoque>(cfg, '/api/entradas', {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  getProducaoPreview: (cfg: ApiConfig, equipamentoId: number) =>
    apiRequest<ProducaoPreview>(cfg, `/api/producao/preview/${equipamentoId}`),
  produzir: (cfg: ApiConfig, equipamentoId: number, descricaoAdicional?: string) =>
    apiRequest<Produto>(cfg, '/api/producao', {
      method: 'POST',
      body: JSON.stringify({ equipamentoId, descricaoAdicional }),
    }),

  getProdutos: (cfg: ApiConfig) => apiRequest<ProdutoList[]>(cfg, '/api/produtos'),
  getProduto: (cfg: ApiConfig, id: number) => apiRequest<Produto>(cfg, `/api/produtos/${id}`),
  deleteProduto: (cfg: ApiConfig, id: number) =>
    apiRequest<void>(cfg, `/api/produtos/${id}`, { method: 'DELETE' }),
};
