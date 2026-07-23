# PTRON

> [Português](#português) · [English](#english)

---

## Português

Aplicativo de **controle de produção de equipamentos eletrônicos** para uso pessoal.

Permite cadastrar insumos (componentes), montar equipamentos (modelos) a partir desses insumos, dar entrada de estoque com cálculo de custo médio e registrar a produção de produtos finais com o custo total apurado automaticamente.

### Funcionalidades

- **Cadastro de tipos** de componente (resistor, diodo, válvula, etc.).
- **Cadastro de insumos** (componentes) com tipo, atributos opcionais (valor, potência, voltagem), foto, saldo e custo unitário.
- **Cadastro de equipamentos (modelos)** com foto e lista de insumos (BOM).
- **Entrada de estoque** estilo "carrinho", com recálculo de **custo médio ponderado** por insumo.
- **Produção**: valida disponibilidade de insumos, estima o custo, consome o estoque e gera o produto.
- **Listagem de produtos** produzidos com custo; exclusão devolve os insumos ao estoque.

Especificação completa em [`especificacao.md`](especificacao.md). Plano de implementação (épicos e stories) em [`plano.md`](plano.md).

### Stack

- **Blazor Server** (.NET 8) — C# full-stack, sem JavaScript.
- **Entity Framework Core** + **SQLite** (arquivo `ptron.db`, criado em `src/PTRON/` ao executar).
- **MudBlazor** — biblioteca de componentes de UI.
- Fotos salvas em `wwwroot/uploads/`.
- Idioma da interface: **Português (pt-BR)**; valores monetários em R$.

### Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download) (o repositório fixa o SDK via `global.json`)

### Como executar

Na raiz do repositório:

```bash
dotnet restore
dotnet run --project src/PTRON
```

Na primeira execução:

1. O banco SQLite (`src/PTRON/ptron.db`) é criado e as migrations são aplicadas.
2. Se o banco estiver vazio, dados de exemplo são carregados (tipos + insumos).
3. Abra o endereço exibido no console (por padrão `https://localhost:7291`).

Se o certificado HTTPS de desenvolvimento pedir confiança:

```bash
dotnet dev-certs https --trust
```

### Backup

Todo o estado fica no arquivo `ptron.db` (e eventuais `ptron.db-wal` / `ptron.db-shm` se o SQLite estiver em modo WAL).

1. Pare a aplicação.
2. Copie `src/PTRON/ptron.db` (e os arquivos `-wal`/`-shm`, se existirem) para um local seguro.
3. Para restaurar, substitua esses arquivos com a aplicação parada.

As fotos enviadas ficam em `src/PTRON/wwwroot/uploads/` — inclua essa pasta no backup se usar imagens.

### Regras de negócio importantes

- **Custo médio ponderado** na entrada de estoque:
  `novoCusto = (Saldo*CustoUnitario + Σ qtd*preço) / (Saldo + Σ qtd)`
- O custo dos insumos é **congelado (snapshot)** no momento da produção; alterar a lista de insumos de um modelo **não** altera produtos já produzidos.
- Excluir um produto **devolve** as quantidades dos insumos ao estoque (o custo médio unitário do insumo não é recalculado na devolução).

### Status

**Completo** (épicos 0–7). Veja o detalhamento em [`plano.md`](plano.md).

---

## English

Personal-use application for **production control of electronic equipment**.

It lets you register components (insumos), build equipment (models) from those components, record stock entries with average-cost calculation, and register the production of final products with the total cost computed automatically.

### Features

- **Type registry** for components (resistor, diode, valve, etc.).
- **Component (insumo) registry** with type, optional attributes (value, power, voltage), photo, stock balance and unit cost.
- **Equipment (model) registry** with photo and bill of materials (BOM).
- **Stock entry** in a "cart" style, recalculating the **weighted-average cost** per component.
- **Production**: validates component availability, estimates cost, consumes stock and creates the product.
- **Product listing** of produced items with cost; deleting a product returns its components to stock.

Full specification in [`especificacao.md`](especificacao.md). Implementation plan (epics and stories) in [`plano.md`](plano.md).

### Stack

- **Blazor Server** (.NET 8) — full-stack C#, no JavaScript.
- **Entity Framework Core** + **SQLite** (`ptron.db` file, created under `src/PTRON/` when you run).
- **MudBlazor** — UI component library.
- Photos stored in `wwwroot/uploads/`.
- UI language: **Portuguese (pt-BR)**; monetary values in R$.

### Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download) (pinned via `global.json`)

### Running

From the repository root:

```bash
dotnet restore
dotnet run --project src/PTRON
```

On first run:

1. The SQLite database (`src/PTRON/ptron.db`) is created and migrations are applied.
2. If the database is empty, sample data is seeded (types + components).
3. Open the address shown in the console (by default `https://localhost:7291`).

If the HTTPS development certificate needs to be trusted:

```bash
dotnet dev-certs https --trust
```

### Backup

All state lives in `ptron.db` (plus `ptron.db-wal` / `ptron.db-shm` if SQLite is in WAL mode).

1. Stop the app.
2. Copy `src/PTRON/ptron.db` (and `-wal`/`-shm` if present) somewhere safe.
3. To restore, replace those files while the app is stopped.

Uploaded photos live in `src/PTRON/wwwroot/uploads/` — include that folder in backups if you use images.

### Important business rules

- **Weighted-average cost** on stock entry:
  `newCost = (Balance*UnitCost + Σ qty*price) / (Balance + Σ qty)`
- Component costs are **frozen (snapshot)** at production time; changing a model's component list does **not** affect already-produced products.
- Deleting a product **returns** component quantities to stock (the component's average unit cost is not recalculated on return).

### Status

**Complete** (epics 0–7). See details in [`plano.md`](plano.md).
