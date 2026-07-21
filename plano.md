# PTRON — Plano de Implementação

Aplicativo de controle de produção de equipamentos eletrônicos (uso pessoal).

## Stack

- **Frontend + Backend:** Blazor Server (.NET 8) — C# full-stack, sem JavaScript.
- **UI:** MudBlazor (tabelas, autocomplete, diálogos, upload).
- **Banco de dados:** SQLite via Entity Framework Core (arquivo `ptron.db`).
- **Fotos:** salvas em `wwwroot/uploads/`, caminho gravado no banco.
- **Idioma da UI:** Português. **Moeda:** `decimal`, formato R$ (pt-BR).

## Progresso

| Épico | Descrição | Status |
|-------|-----------|--------|
| 0 | Fundação do projeto | ✅ Concluído |
| 1 | Cadastro de Tipos | ⬜ Pendente |
| 2 | Cadastro de Insumos | ⬜ Pendente |
| 3 | Equipamentos / Modelos | ⬜ Pendente |
| 4 | Entrada de Estoque | ⬜ Pendente |
| 5 | Produção | ⬜ Pendente |
| 6 | Produtos | ⬜ Pendente |
| 7 | Acabamento | ⬜ Pendente |

> Última atualização: Épico 0 finalizado — projeto compila (0 warnings), executa, aplica migrations e cria `ptron.db`. Próximo: Épico 1.

## Modelo de dados

```
TipoInsumo        (Id, Nome)
Insumo            (Id, TipoInsumoId, Nome, Valor?, Potencia?, Voltagem?,
                   Saldo=0, CustoUnitario=0, FotoPath?)
Equipamento       (Id, Nome, FotoPath?)
EquipamentoInsumo (Id, EquipamentoId, InsumoId, Qtd)            -- BOM (item 3a)
EntradaEstoque    (Id, Data)
EntradaEstoqueItem(Id, EntradaEstoqueId, InsumoId, Qtd, PrecoUnitario)
Produto           (Id, EquipamentoId, DescricaoAdicional, Data, CustoTotal)
ProdutoInsumo     (Id, ProdutoId, InsumoId, PrecoUnitario, Qtd)  -- snapshot (item 6a)
```

---

## Épico 0 — Fundação do projeto ✅

Preparar o esqueleto técnico antes das funcionalidades.

- [x] **E0-S1** — Scaffold do projeto Blazor Server (.NET 8) em `src/PTRON` + `PTRON.sln`. SDK fixado em .NET 8 via `global.json`; arquivos de exemplo do template removidos.
- [x] **E0-S2** — Pacotes adicionados (EF Core Sqlite 8.0.29, Design, MudBlazor 9.7.0); MudBlazor integrado (providers, CSS/JS/fontes); layout base com `MudAppBar` + `MudDrawer`, alternância claro/escuro, `NavMenu` com as 6 áreas, home com atalhos e páginas placeholder.
- [x] **E0-S3** — 8 entidades em `Models/`; `AppDbContext` com relacionamentos, delete behaviors e precisão decimal (18,4), registrado via `AddDbContextFactory`; migration `InitialCreate`; migração automática no startup criando `ptron.db`.
- [x] **E0-S4** — `ImageUploadService`: valida tipo/tamanho (5 MB), salva em `wwwroot/uploads/`, retorna caminho web, com helper `Delete`.
- [x] **E0-S5** — Cultura pt-BR global (`Program.cs` + `UseRequestLocalization`) e helper `Format` (`Money` → `R$ 1.234,56`, `Number`, `Date`).

**Extras:** `.gitignore` (bin/obj, `*.db`, `wwwroot/uploads/`); README atualizado (comando de execução e porta corretos).

---

## Épico 1 — Cadastro de Tipos (item 1)

- **E1-S1** — Listar tipos em tabela.
- **E1-S2** — Criar/editar tipo (campo Nome) via diálogo.
- **E1-S3** — Excluir tipo, bloqueando exclusão se houver insumos vinculados.

---

## Épico 2 — Cadastro de Insumos (item 2)

- **E2-S1** — Listar insumos (Nome, Tipo, Saldo, Custo unitário, miniatura da foto).
- **E2-S2** — Criar/editar insumo: seleção de Tipo (dropdown), campos opcionais (valor, potência, voltagem), upload de foto opcional.
- **E2-S3** — Saldo e Custo unitário exibidos como somente-leitura (sempre 0 no cadastro).
- **E2-S4** — Excluir insumo, bloqueando se estiver em BOM ou tiver saldo/movimentos.
- **E2-S5** — Busca/filtro por nome e por tipo.

---

## Épico 3 — Cadastro de Equipamentos / Modelos (item 3 + 3a)

- **E3-S1** — Listar equipamentos (Nome, foto).
- **E3-S2** — Criar/editar equipamento (Nome + foto).
- **E3-S3** — Editor de BOM: adicionar/remover insumos com quantidade (autocomplete de insumo).
- **E3-S4** — Excluir equipamento (e sua BOM), preservando produtos já feitos.

---

## Épico 4 — Entrada de Estoque (item 4)

- **E4-S1** — Tela "cart": adicionar linhas de insumo via autocomplete + Quantidade + Preço unitário.
- **E4-S2** — Editar/remover linhas antes de finalizar; total da entrada exibido.
- **E4-S3** — Finalizar entrada (transação): para cada insumo, recalcular **custo médio ponderado** e incrementar Saldo.
  - `novoCusto = (Saldo*CustoUnitario + Σ qtd*preço) / (Saldo + Σ qtd)`
- **E4-S4** — Histórico de entradas (opcional, somente leitura).

---

## Épico 5 — Produção (item 5)

- **E5-S1** — Selecionar equipamento; exibir BOM com necessário x disponível e **custo estimado**.
- **E5-S2** — Validar disponibilidade; se faltar insumo, bloquear e listar os faltantes.
- **E5-S3** — Campo "Descrição adicional do produto".
- **E5-S4** — Produzir (transação): subtrair saldo, criar `Produto` + `ProdutoInsumo` (snapshot de custo), calcular `CustoTotal`.

---

## Épico 6 — Produtos (item 6 + 6a)

- **E6-S1** — Listar produtos produzidos (equipamento, descrição, data, custo total/unitário).
- **E6-S2** — Detalhe do produto com insumos consumidos e custos (snapshot).
- **E6-S3** — Excluir produto (transação): devolver insumos ao estoque (saldo).
- **E6-S4** — Garantir que alterar BOM de um modelo NÃO afeta produtos já criados.

---

## Épico 7 — Acabamento

- **E7-S1** — Seed de dados de exemplo (tipos + insumos) para teste.
- **E7-S2** — Validações de formulário e mensagens de erro amigáveis.
- **E7-S3** — Teste manual do fluxo completo: cadastro → entrada → produção → listagem → exclusão.
- **E7-S4** — README com instruções de execução e backup do `ptron.db`.

---

## Ordem sugerida de execução

~~E0~~ → **E1** (próximo) → E2 → E3 → E4 → E5 → E6 → E7

(As funcionalidades de valor real começam a aparecer no E4/E5; E1–E3 são pré-requisitos de dados.)
