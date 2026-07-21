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
- **Listagem de produtos** produzidos com custo unitário; exclusão devolve os insumos ao estoque.

Especificação completa em [`especificacao.md`](especificacao.md). Plano de implementação (épicos e stories) em [`plano.md`](plano.md).

### Stack

- **Blazor Server** (.NET 8) — C# full-stack, sem JavaScript.
- **Entity Framework Core** + **SQLite** (arquivo `ptron.db`).
- **MudBlazor** — biblioteca de componentes de UI.
- Fotos salvas em `wwwroot/uploads/`.
- Idioma da interface: **Português (pt-BR)**; valores monetários em R$.

### Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)

### Como executar

```bash
dotnet restore
dotnet run --project src/PTRON
```

O banco SQLite (`ptron.db`) é criado automaticamente na primeira execução via migrations do EF Core. Depois, abra o endereço exibido no console (por padrão `https://localhost:7291`).

### Backup

Todo o estado fica no arquivo `ptron.db`. Para fazer backup, basta copiar esse arquivo com a aplicação parada.

### Regras de negócio importantes

- **Custo médio ponderado** na entrada de estoque:
  `novoCusto = (Saldo*CustoUnitario + Σ qtd*preço) / (Saldo + Σ qtd)`
- O custo dos insumos é **congelado (snapshot)** no momento da produção; alterar a lista de insumos de um modelo **não** altera produtos já produzidos.
- Excluir um produto **devolve** os insumos consumidos ao estoque.

### Status

Em desenvolvimento. Veja o progresso e o backlog em [`plano.md`](plano.md).

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
- **Product listing** of produced items with unit cost; deleting a product returns its components to stock.

Full specification in [`especificacao.md`](especificacao.md). Implementation plan (epics and stories) in [`plano.md`](plano.md).

### Stack

- **Blazor Server** (.NET 8) — full-stack C#, no JavaScript.
- **Entity Framework Core** + **SQLite** (`ptron.db` file).
- **MudBlazor** — UI component library.
- Photos stored in `wwwroot/uploads/`.
- UI language: **Portuguese (pt-BR)**; monetary values in R$.

### Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download)

### Running

```bash
dotnet restore
dotnet run --project src/PTRON
```

The SQLite database (`ptron.db`) is created automatically on first run via EF Core migrations. Then open the address shown in the console (by default `https://localhost:7291`).

### Backup

All state lives in the `ptron.db` file. To back it up, just copy that file while the app is stopped.

### Important business rules

- **Weighted-average cost** on stock entry:
  `newCost = (Balance*UnitCost + Σ qty*price) / (Balance + Σ qty)`
- Component costs are **frozen (snapshot)** at production time; changing a model's component list does **not** affect already-produced products.
- Deleting a product **returns** the consumed components to stock.

### Status

In development. See progress and backlog in [`plano.md`](plano.md).
