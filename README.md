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
- **REST API** em `/api` (JSON camelCase) para clientes externos, inclusive Android.
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

### API REST (Android)

Base URL de desenvolvimento: `http://localhost:5083`. JSON em camelCase. Erros: `{ "error": "mensagem" }`.

Autenticação: envie o token de `Api:Token` em `appsettings.json` (padrão `ptron-dev-token-8f4c2a91`) em todo pedido `/api`:

```http
Authorization: Bearer ptron-dev-token-8f4c2a91
```

Alternativa: cabeçalho `X-Api-Key: ptron-dev-token-8f4c2a91`. Sem token a API responde `401`. A UI Blazor não usa esse token.

| Método | Caminho | Descrição |
|--------|---------|-----------|
| GET | `/api` | Ping (`name`, `version`) |
| GET/POST | `/api/tipos` | Listar / criar tipo |
| GET/PUT/DELETE | `/api/tipos/{id}` | Obter / alterar / excluir tipo |
| GET/POST | `/api/insumos` | Listar (`?search=&tipoId=`) / criar insumo |
| GET/PUT/DELETE | `/api/insumos/{id}` | Obter / alterar / excluir insumo |
| GET/POST | `/api/equipamentos` | Listar / criar equipamento (BOM no body) |
| GET/PUT/DELETE | `/api/equipamentos/{id}` | Detalhe com BOM / alterar / excluir |
| GET/POST | `/api/entradas` | Histórico / finalizar entrada |
| GET | `/api/entradas/{id}` | Detalhe da entrada |
| GET | `/api/producao/preview/{equipamentoId}` | Preview (faltantes, custo estimado) |
| POST | `/api/producao` | Produzir `{ equipamentoId, descricaoAdicional }` |
| GET | `/api/produtos` | Listar produtos |
| GET/DELETE | `/api/produtos/{id}` | Detalhe (snapshot) / excluir (devolve estoque) |
| POST | `/api/uploads` | Multipart campo `file` → `{ "fotoPath": "/uploads/..." }` |

Fotos: `fotoPath` é relativo (ex. `/uploads/abc.png`). No Android use `baseUrl + fotoPath`. Swagger em desenvolvimento: `http://localhost:5083/swagger`.

Emulador Android: `http://10.0.2.2:5083`. Celular na mesma rede:

```bash
dotnet run --project src/PTRON --launch-profile PTRON-LAN
```

Use o IP da máquina, por exemplo `http://192.168.0.10:5083`. No Swagger, clique em **Authorize** e cole o token.

App mobile (Expo) em [`mobile/`](mobile/README.md):

```bash
cd mobile
npm install
npm start
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
- **REST API** at `/api` (camelCase JSON) for external clients, including Android.
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

### REST API (Android)

Development base URL: `http://localhost:5083`. camelCase JSON. Errors: `{ "error": "message" }`.

Auth: send the `Api:Token` value from `appsettings.json` (default `ptron-dev-token-8f4c2a91`) on every `/api` request:

```http
Authorization: Bearer ptron-dev-token-8f4c2a91
```

Alternatively: header `X-Api-Key: ptron-dev-token-8f4c2a91`. Missing token → `401`. The Blazor UI does not use this token.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api` | Ping (`name`, `version`) |
| GET/POST | `/api/tipos` | List / create component type |
| GET/PUT/DELETE | `/api/tipos/{id}` | Get / update / delete type |
| GET/POST | `/api/insumos` | List (`?search=&tipoId=`) / create component |
| GET/PUT/DELETE | `/api/insumos/{id}` | Get / update / delete component |
| GET/POST | `/api/equipamentos` | List / create equipment (BOM in body) |
| GET/PUT/DELETE | `/api/equipamentos/{id}` | Detail with BOM / update / delete |
| GET/POST | `/api/entradas` | History / finalize stock entry |
| GET | `/api/entradas/{id}` | Stock-entry detail |
| GET | `/api/producao/preview/{equipamentoId}` | Preview (shortages, estimated cost) |
| POST | `/api/producao` | Produce `{ equipamentoId, descricaoAdicional }` |
| GET | `/api/produtos` | List products |
| GET/DELETE | `/api/produtos/{id}` | Detail (snapshot) / delete (returns stock) |
| POST | `/api/uploads` | Multipart field `file` → `{ "fotoPath": "/uploads/..." }` |

Photos: `fotoPath` is relative (e.g. `/uploads/abc.png`). On Android use `baseUrl + fotoPath`. Swagger in Development: `http://localhost:5083/swagger`.

Android emulator: `http://10.0.2.2:5083`. Physical device on the same LAN:

```bash
dotnet run --project src/PTRON --launch-profile PTRON-LAN
```

Use the PC's IP, e.g. `http://192.168.0.10:5083`. In Swagger, click **Authorize** and paste the token.

Mobile app (Expo) in [`mobile/`](mobile/README.md):

```bash
cd mobile
npm install
npm start
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
