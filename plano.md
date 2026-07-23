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
| 1 | Cadastro de Tipos | ✅ Concluído |
| 2 | Cadastro de Insumos | ✅ Concluído |
| 3 | Equipamentos / Modelos | ✅ Concluído |
| 4 | Entrada de Estoque | ✅ Concluído |
| 5 | Produção | ✅ Concluído |
| 6 | Produtos | ✅ Concluído |
| 7 | Acabamento | ✅ Concluído |

> Última atualização: Épico 7 finalizado — seed, validações, smoke test do fluxo completo e README atualizado. **Plano completo (E0–E7).**

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

## Épico 1 — Cadastro de Tipos (item 1) ✅

- [x] **E1-S1** — Listar tipos em `MudTable` (ordenado por nome, estado vazio e loading).
- [x] **E1-S2** — Criar/editar tipo (campo Nome) via `TipoDialog` com validação (`MudForm`).
- [x] **E1-S3** — Excluir tipo com `ConfirmDialog`, bloqueando quando houver insumos vinculados (aviso via `Snackbar`).

**Implementação:** `TipoInsumoService` (CRUD + `CountInsumosAsync`) via `IDbContextFactory`; `ConfirmDialog` reutilizável; feedback por `ISnackbar`. Registrado em DI.

---

## Épico 2 — Cadastro de Insumos (item 2) ✅

- [x] **E2-S1** — Listar insumos (Foto, Nome, Tipo, Especificações, Saldo, Custo unitário) em `MudTable`.
- [x] **E2-S2** — Criar/editar via `InsumoDialog`: `MudSelect` de Tipo, campos opcionais, upload de foto (`MudFileUpload` → `ImageUploadService`) com preview/remover.
- [x] **E2-S3** — Saldo e Custo unitário exibidos somente-leitura (visíveis apenas na edição); zerados no cadastro.
- [x] **E2-S4** — Excluir insumo bloqueando quando em BOM, com movimentações de estoque, usado em produtos ou com saldo.
- [x] **E2-S5** — Busca por nome (debounce) + filtro por tipo.

**Implementação:** `InsumoService` (CRUD, limpeza de fotos, guardas de exclusão). Testado no navegador.

---

## Épico 3 — Cadastro de Equipamentos / Modelos (item 3 + 3a) ✅

- [x] **E3-S1** — Listar equipamentos (Foto, Nome, contagem de insumos) em `MudTable`.
- [x] **E3-S2** — Criar/editar via `EquipamentoDialog` (Nome + foto).
- [x] **E3-S3** — Editor de BOM: `MudAutocomplete` de insumo + quantidade, adicionar/remover linhas (mescla duplicados).
- [x] **E3-S4** — Excluir equipamento e sua BOM (cascade); **bloqueado** quando há produtos produzidos, preservando-os.

**Implementação:** `EquipamentoService` (CRUD + reconciliação de BOM, limpeza de foto, guarda de produtos). Testado no navegador (BOM recarrega na edição).

> Decisão: como `Produto` referencia `Equipamento` (RESTRICT), a exclusão é bloqueada quando existem produtos — assim os produtos já feitos são preservados.

---

## Épico 4 — Entrada de Estoque (item 4) ✅

- [x] **E4-S1** — Tela "cart" com autocomplete de insumo + Quantidade + Preço unitário.
- [x] **E4-S2** — Editar/remover linhas no carrinho; total exibido; mescla de duplicados.
- [x] **E4-S3** — Finalizar em transação: grava `EntradaEstoque` + itens e recalcula custo médio ponderado.
  - `novoCusto = (Saldo*CustoUnitario + Σ qtd*preço) / (Saldo + Σ qtd)`
- [x] **E4-S4** — Aba Histórico (somente leitura) com diálogo de detalhe.

**Implementação:** `EntradaEstoqueService.FinalizarAsync`; `Entradas.razor` com abas Nova entrada / Histórico; `EntradaDetalheDialog`. Verificado no navegador com o exemplo da especificação.

---

## Épico 5 — Produção (item 5) ✅

- [x] **E5-S1** — Selecionar equipamento; exibir BOM com necessário × disponível e custo estimado.
- [x] **E5-S2** — Validar disponibilidade; se faltar, bloquear "Produzir" e listar faltantes.
- [x] **E5-S3** — Campo "Descrição adicional do produto".
- [x] **E5-S4** — Produzir (transação): subtrair saldo, criar `Produto` + `ProdutoInsumo` (snapshot), calcular `CustoTotal`.

**Implementação:** `ProducaoService` (`GetPreviewAsync`, `ProduzirAsync`); `Producao.razor`. Verificado no navegador (bloqueio → estoque → produção → saldos atualizados).

---

## Épico 6 — Produtos (item 6 + 6a) ✅

- [x] **E6-S1** — Listar produtos (Id, equipamento, descrição, data, custo total).
- [x] **E6-S2** — Detalhe com insumos consumidos e custos (snapshot) via `ProdutoDetalheDialog`.
- [x] **E6-S3** — Excluir produto (transação): devolve quantidades ao `Saldo` dos insumos.
- [x] **E6-S4** — Alterar BOM do modelo **não** altera produtos já feitos (snapshot independente).

**Implementação:** `ProdutoService` (`GetAllAsync`, `GetWithInsumosAsync`, `DeleteAsync`); `Produtos.razor`. Verificado no navegador (listagem → detalhe → BOM editada sem afetar produto → exclusão restaurando estoque).

---

## Épico 7 — Acabamento ✅

- [x] **E7-S1** — Seed idempotente (`SeedData`): 6 tipos + 7 insumos quando o banco está vazio; pasta `uploads/` criada no startup.
- [x] **E7-S2** — Validações reforçadas: nome obrigatório (trim), tipo obrigatório no insumo, nome de tipo único (case-insensitive); mensagens em português via Snackbar.
- [x] **E7-S3** — Smoke test do fluxo: produção → listagem → exclusão (+ validação de tipo duplicado).
- [x] **E7-S4** — README atualizado (execução, seed, backup WAL/uploads, status completo).

**Implementação:** `Data/SeedData.cs`; `Services/Validation.cs`; ajustes em `TipoInsumoService` / `InsumoService` / `EquipamentoService` / `Program.cs` / `README.md`.

---

## Ordem sugerida de execução

~~E0~~ → ~~E1~~ → ~~E2~~ → ~~E3~~ → ~~E4~~ → ~~E5~~ → ~~E6~~ → ~~E7~~ ✅

**Plano completo.**
