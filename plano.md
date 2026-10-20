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
| 8 | Gestão de Usuários | ⏸ Em pausa — S1–S9 concluídas; S10 e S11 pendentes |
| 9 | Entrada de Estoque com 3 moedas | ✅ Concluído |
| 10 | Página de boas-vindas | ✅ Concluído |
| 11 | Idiomas (português, inglês, espanhol) | 🚧 S1 a S4 feitas; S5 descartada; S6–S7 pendentes |
| 12 | Código aberto | 🚧 S1 feita (PR #32); S2–S12 pendentes |

> Última atualização: Épico 10 concluído — a primeira página de quem não entrou é a boas-vindas; quem já entrou continua no início do aplicativo. Épico 8 segue em pausa (S10 e S11 pendentes). Próximo é o Épico 11 (português, inglês e espanhol). Épico 9 concluído. Épicos E0–E7 completos.

## Modelo de dados

```
TipoInsumo        (Id, Nome)
Insumo            (Id, TipoInsumoId, Nome, Valor?, Potencia?, Voltagem?,
                   Saldo=0, CustoUnitario=0, FotoPath?)
Equipamento       (Id, Nome, FotoPath?)
EquipamentoInsumo (Id, EquipamentoId, InsumoId, Qtd)            -- BOM (item 3a)
EntradaEstoque    (Id, Data, MoedaProdutos, CambioProdutos, MoedaImpostos,
                   CambioImpostos, MoedaFrete, MoedaDestino=BRL, Frete, Impostos)
EntradaEstoqueItem(Id, EntradaEstoqueId, InsumoId, Qtd, PrecoUnitario,
                   FreteRateado, ImpostoRateado, CustoUnitario)  -- custo em BRL
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

## Épico 8 — Gestão de Usuários 🚧 (em andamento — S1–S9 concluídas)

Hoje o acesso é um único usuário fixo (`Leo`, em `AccountController`, com senha no código) e todos os dados (tipos, insumos, equipamentos, entradas, produções) são globais. Este épico transforma o PTRON em um sistema multiusuário — **web** — com cadastro, ativação por e-mail, recuperação de senha e **isolamento total dos dados por usuário**. O **app mobile fica fora deste épico** (continua funcionando como hoje, ver D7).

### Requisitos (do pedido)

1. O usuário **Leo** passa a ser **reservado**, usado apenas para emergência. Seu e-mail é **pedrosa.leonardo@gmail.com**.
2. Novos usuários podem se **cadastrar**. Obrigatórios: **e-mail, país, estado e senha**.
3. O usuário recebe um **link de ativação por e-mail**.
4. Somente depois de ativado o usuário consegue usar o sistema.
5. O usuário pode **alterar a senha**.
6. Na tela de login informa-se o **e-mail** (o Leo também pode entrar apenas com `Leo`).
7. Na tela de login há **"Esqueci a senha"**: um e-mail é enviado para redefinir a senha.
8. Tipos, insumos, equipamentos, entradas de estoque, produções e produtos passam a ser **específicos de cada usuário**.

### Decisões já tomadas (respostas às perguntas do rascunho)

| # | Decisão |
|---|---------|
| Q1 | Os dados existentes ficam com o **Leo**. |
| Q2 | O cadastro oferece uma **lista de países** (fornecida pelo sistema); **estado é texto livre**. |
| Q3 | A senha do Leo fica **no banco de dados** (hash), não mais no código. |
| Q4 | Cada novo usuário, ao ativar a conta, **recebe os tipos padrão** (Resistor, Capacitor, Diodo, Transistor, CI, Válvula). Não recebe insumos de exemplo. |
| Q5 | O **app mobile NÃO entra neste épico** (fica para um épico futuro). A API continua com o token fixo atual, que passa a enxergar apenas os dados do Leo. |
| Q6 | Configuração do SMTP (e demais segredos) por **variáveis de ambiente**, feita por você depois que o épico estiver pronto. |

### Decisões de arquitetura propostas

**D1 — Identidade: ASP.NET Core Identity (EF Core) em vez de implementação própria.**
Já fornece hash de senha (PBKDF2), tokens de confirmação de e-mail e de redefinição de senha, bloqueio por tentativas (lockout) e invalidação de sessões ao trocar a senha (`SecurityStamp`). Só usamos o núcleo (`UserManager`/`SignInManager`); as telas continuam em Blazor/MudBlazor, em português. O cookie de autenticação atual é mantido na web.

**D2 — Leo é um usuário real, porém reservado.**
- Linha em `AspNetUsers` com `Id = "leo"` (o mesmo `NameIdentifier` usado hoje), `UserName = "Leo"`, **e-mail `pedrosa.leonardo@gmail.com`** (configurável em `Leo:Email`), e-mail já confirmado (não precisa ativar) e marcada `Reservado`.
- A senha é guardada como **hash no banco**. Senha inicial vem da variável de ambiente `PTRON_Leo__InitialPassword` na primeira execução; se ela não existir, usa-se a senha atual como último recurso para não trancar o acesso — e a story de segurança recomenda trocá-la logo após o primeiro login. Depois disso a senha vive só no banco e o valor no código é removido.
- Login do Leo: e-mail **ou** o apelido `Leo` + senha. Como tem e-mail real, "Esqueci a senha" e "Alterar senha" funcionam normalmente para ele.
- Cadastro bloqueia o e-mail do Leo e o nome `leo`.
- Privilégios exclusivos do Leo (flag `Reservado`): console SQL.
- **Dados existentes** (tudo que já está em `ptron.db`) são atribuídos ao Leo na migração.

**D3 — Isolamento de dados (multi-tenant por coluna `UserId`).**
- `UserId` (FK para o usuário, `NOT NULL`) nas entidades raiz: `TipoInsumo`, `Insumo`, `Equipamento`, `EntradaEstoque`, `Produto`. As tabelas-filho (`EquipamentoInsumo`, `EntradaEstoqueItem`, `ProdutoInsumo`) herdam o isolamento pelo pai e **cada FK entre entidades é validada para pertencer ao mesmo usuário** (ex.: não permitir `Insumo` de A numa BOM de B).
- *Global query filter* do EF Core (`e.UserId == CurrentUserId`) em todas as entidades raiz, para que uma consulta esquecida nunca vaze dados.
- `UserId` é carimbado automaticamente no `SaveChanges`; nunca vem do cliente.
- Como os serviços usam `IDbContextFactory<AppDbContext>` (e o Blazor Server cria contextos fora do escopo da requisição), o `UserId` atual é injetado por uma fábrica própria (`TenantDbContextFactory`) a partir de um `ICurrentUser` (lido do `AuthenticationStateProvider` na UI e do `HttpContext` na API).
- Unicidade do nome do tipo passa a ser **por usuário**: índice único `(UserId, Nome)`.
- Pontos que hoje são globais e precisam de tratamento: console **SQL** (restrito ao Leo), **uploads** de fotos, **seed** inicial, **API REST**.

**D4 — Banco de dados: manter SQLite (recomendação).** Ver seção própria abaixo.

**D5 — E-mail: abstração `IAppEmailSender` com SMTP (MailKit), configurada por variáveis de ambiente.**
Todas as variáveis começam com o prefixo **`PTRON_`** (carregadas com `AddEnvironmentVariables(prefix: "PTRON_")`; `__` = seção aninhada, ex.: `PTRON_Smtp__Host` → `Smtp:Host`). Apenas as variáveis com esse prefixo fazem parte do contrato de configuração:

| Variável | Uso |
|----------|-----|
| `PTRON_Smtp__Host`, `PTRON_Smtp__Port` | Servidor SMTP (Cloudflare) |
| `PTRON_Smtp__SecureSocketOptions` | `StartTls` / `SslOnConnect` / `None` |
| `PTRON_Smtp__User`, `PTRON_Smtp__Password` | Credenciais |
| `PTRON_Smtp__FromAddress`, `PTRON_Smtp__FromName` | Remetente |
| `PTRON_App__PublicBaseUrl` | URL pública usada nos links dos e-mails |
| `PTRON_Leo__InitialPassword` | Senha inicial do Leo (só na primeira execução) |

No servidor Linux elas entram no serviço systemd (`Environment=` / `EnvironmentFile=`), que fica **fora** da pasta limpa pelo deploy. Em desenvolvimento, sem SMTP configurado, o e-mail é gravado no log/console (o link aparece no terminal); em produção, SMTP ausente gera aviso claro no log na inicialização. Textos dos e-mails em português.

**D6 — Pontos operacionais críticos (fáceis de esquecer):**
- **Chaves do Data Protection** precisam ser persistidas fora da pasta de deploy (o deploy faz `rm -rf /opt/ptron/*`). Sem isso, cada deploy/restart **invalida cookies e links de ativação/redefinição** já enviados. Será usada uma pasta persistente (ex.: `/opt/ptron-data/keys`, configurável) que o deploy não toca.
- **URL pública** (`PTRON_App__PublicBaseUrl`) para montar os links dos e-mails corretamente (atrás de proxy/HTTPS).
- Tokens: ativação válida por 24 h (com opção de reenviar); redefinição de senha válida por 1 h e uso único.

**D7 — API REST e app mobile: sem mudança de contrato neste épico.**
O app mobile não é alterado. A API continua autenticada pelo token fixo (`Api:Token`), mas ele passa a representar o **usuário Leo**: o `ApiTokenMiddleware` define o `ICurrentUser` como Leo, então o app atual segue funcionando e enxerga exatamente os dados que já tem hoje (que pertencem ao Leo). Os demais usuários usam apenas a web. Login por usuário na API/app (JWT + refresh token, cadastro e recuperação de senha no celular) fica para um **épico futuro**.

### Banco de dados — SQLite ou migrar?

**Recomendação: manter SQLite neste épico.**

- A aplicação roda como **um único processo** em **um único servidor** (systemd), o que é o cenário em que o SQLite funciona bem.
- A carga esperada (uso pessoal/pequeno grupo de usuários, escritas curtas) cabe com folga no SQLite em modo **WAL**; leituras são concorrentes e há um único escritor por vez, o que é imperceptível nessa escala.
- O pipeline de deploy, o backup (copiar `ptron.db`) e o custo operacional continuam simples; nada de servidor de banco para manter.
- O isolamento por `UserId` é igual em qualquer banco, e o código usa EF Core — **a migração futura para PostgreSQL é viável e barata**, desde que evitemos SQL específico de SQLite (a única exceção hoje é o console SQL, restrito ao Leo).

**Ajustes incluídos no épico:** habilitar `PRAGMA journal_mode=WAL` e `busy_timeout`; **backup automático** periódico do `ptron.db` (agora ele guarda dados e credenciais de várias pessoas); manter `ptron.db` e a pasta de chaves fora da limpeza do deploy.

**Sinais de que vale migrar para PostgreSQL (não agora):** mais de algumas dezenas de usuários ativos simultâneos, necessidade de mais de uma instância do app, requisitos de alta disponibilidade/backup gerenciado, ou erros frequentes de `database is locked`.

### Stories

**Backend e web**

- [x] **E8-S1** — **Fundação de identidade.** Adicionar `Microsoft.AspNetCore.Identity.EntityFrameworkCore` e `MailKit`. Criar `ApplicationUser` (herda `IdentityUser`; campos `Pais` (código ISO), `Estado` (texto), `CriadoEm`, `Reservado`). `AppDbContext` passa a herdar de `IdentityDbContext<ApplicationUser>`. Política de senha (mín. 8 caracteres, letra e número), `RequireConfirmedEmail`, e-mail único, lockout (5 tentativas / 15 min). Persistir chaves do Data Protection. Migration com tabelas de identidade, criação do Leo (`Id="leo"`, e-mail confirmado, senha em hash conforme D2).
  **Implementação:** `Models/ApplicationUser.cs`; `Identity/` (`IdentityServiceCollectionExtensions`, `LeoBootstrap`, `LeoOptions`, `ReservedUsers`, `LetterPasswordValidator`, `ReservedUserValidator`); migration `IdentityFoundation` (tabelas Identity + linha do Leo, sem hash); `AppDbContext : IdentityDbContext<ApplicationUser>` com índice único de e-mail; `Program.cs` (prefixo `PTRON_`, Data Protection em `DataProtection:KeysPath`, padrão `../ptron-data/keys` em produção e `App_Data/keys` em desenvolvimento, bootstrap do Leo após `Migrate`); `build.yml` cria/preserva a pasta de chaves. Refresh tokens foram removidos do escopo junto com o mobile. O login ainda usa o `AccountController` atual (substituído em E8-S6); o hash do Leo já fica no banco. 19 testes automatizados (`IdentityFoundationTests`).
- [x] **E8-S2** — **Dados por usuário.** Migration adiciona `UserId` nas 5 entidades raiz (dados existentes → Leo), índices e FKs; índice único `(UserId, Nome)` em tipos. `ICurrentUser`, `TenantDbContextFactory`, *query filters* e carimbo automático de `UserId`. Revisar todos os serviços (`TipoInsumo`, `Insumo`, `Equipamento`, `EntradaEstoque`, `Producao`, `Produto`) para validar que FKs referenciadas pertencem ao usuário atual e que consultas por Id respeitam o filtro (retornam "não encontrado" para dados de outro usuário).
  **Implementação:** `UserId` em `TipoInsumo`, `Insumo`, `Equipamento`, `EntradaEstoque` e `Produto`; migration `UserOwnership` (linhas existentes → `leo`, FK `Restrict`, índice único `(UserId, Nome)` em tipos). `ICurrentUser`/`CurrentUser` (cookie ou estado do circuito Blazor), `TenantDbContextFactory` no lugar da factory padrão, *query filters* também nas tabelas-filho. `SaveChanges` carimba `UserId` e não deixa alterá-lo. Serviços recusam tipo/insumo/equipamento de outro usuário com "não encontrado". O token fixo da API já define o usuário Leo no `ApiTokenMiddleware`, senão a API ficaria sem dados; o teste de regressão da API continua em E8-S10. 6 testes (`UserOwnershipTests`).
- [x] **E8-S3** — **Envio de e-mail.** `IAppEmailSender` + implementação SMTP (MailKit) + implementação de desenvolvimento (log). Opções `Smtp` e `App:PublicBaseUrl` lidas de variáveis de ambiente com prefixo `PTRON_` (D5). Templates HTML/texto em português (ativação, redefinição e aviso de senha alterada) com a identidade visual do Makelectron. Falha de envio é registrada em log e não derruba a requisição.
  **Implementação:** `Email/` (`IAppEmailSender`, `SmtpAppEmailSender`, `LoggingAppEmailSender`, `EmailTemplates`, `AppLinks`). SMTP quando `Host` e `FromAddress` existem; senão log (em desenvolvimento o link aparece no log; em produção só um aviso, sem o token). Aviso na inicialização se SMTP ou `PublicBaseUrl` faltarem em produção. Links: `/ativar`, `/redefinir-senha` e `/login`. Cadastro ainda não envia (E8-S4). 5 testes (`EmailSenderTests`).
- [x] **E8-S4** — **Cadastro (`/cadastro`).** Formulário: e-mail, **país (lista)**, **estado (texto livre)**, senha e confirmação de senha. Lista de países ISO 3166 em português, mantida no código (`Paises`), armazenando o código ISO; Brasil no topo. Validações: formato de e-mail, unicidade, senha conforme política, e-mail/nome reservado bloqueado, estado obrigatório (máx. 100 caracteres). Cria usuário **inativo** e envia o e-mail de ativação; tela "Verifique seu e-mail". Resposta idêntica para e-mail já existente (evita enumeração de contas); se a conta existente ainda não foi ativada, um novo e-mail de ativação é reenviado.
  **Implementação:** `Pages/Cadastro.razor` (formulário com validação em português, tela "Verifique seu e-mail" e link "Criar conta" na tela de login); `Identity/SignUpService` (valida e-mail, país, estado e senha; bloqueia o e-mail do Leo; cria o usuário com `EmailConfirmed=false`, gera o token e envia o e-mail de ativação; e-mail já existente devolve o mesmo sucesso, sem criar conta (se a conta está pendente, reenvia a ativação com token novo; se já ativa, não envia nada); falha de envio é só registrada em log); `Identity/Paises` (249 países ISO 3166 em português, Brasil no topo, grava o código); `PortugueseIdentityErrorDescriber` (mensagens de senha/e-mail em português). O e-mail é o `UserName`. `Identity/ActivationEmailLimiter` limita a 7 e-mails de ativação por endereço em 24 h (em memória, reutilizável pelo reenvio de E8-S5). 18 testes (`SignUpTests`).
- [x] **E8-S5** — **Ativação (`/ativar`).** Link `…/ativar?userId=…&token=…` confirma o e-mail e ativa a conta; **ao ativar, cria os 6 tipos padrão do usuário** (idempotente). Mensagens para sucesso, link inválido/expirado e conta já ativa. Opção "Reenviar e-mail de ativação" (com limite de frequência).
  **Implementação:** `Pages/Ativar.razor` (anônima; a ativação roda só no circuito interativo, em `OnAfterRenderAsync`, para que pré-renderização ou *scanner* de links do e-mail não consumam o token; telas de sucesso, conta já ativa e link inválido/expirado com campo de e-mail e botão "Reenviar e-mail de ativação"); `Identity/ActivationService` (`ActivateAsync` confirma o e-mail e cria os 6 tipos padrão do usuário só nomes que faltam, sem duplicar; `ResendAsync` envia novo link só para conta pendente e não reservada, sem revelar se o e-mail existe; compartilha com o cadastro o limite de 7 e-mails por endereço em 24 h de `ActivationEmailLimiter`). `SignUpService` passou a usar `ActivationService` para enviar. Não há link para reenviar na tela de login ainda (E8-S6). 11 testes (`ActivationTests`).
- [x] **E8-S6** — **Login por e-mail.** Campo "E-mail" (aceita também o apelido `Leo`). Usuário não ativado não entra e recebe mensagem com a opção de reenviar a ativação. Mensagem genérica para credenciais inválidas; lockout por tentativas. Login passa a usar token antifalsificação (hoje é `IgnoreAntiforgeryToken`) e cookie `Secure`/`HttpOnly`/`SameSite`. Links "Esqueci a senha" e "Criar conta". Nome/e-mail do usuário exibido no topo com menu (Alterar senha, Sair).
  **Implementação:** `Identity/LoginService` (aceita e-mail ou o apelido `Leo`; senha errada e usuário inexistente dão a mesma resposta; conta não ativada só é informada depois que a senha confere, sem revelar e-mails cadastrados; `AccessFailed`/lockout de 5 tentativas em 15 min; zera o contador ao entrar); `AccountController` sem a senha fixa, valida o token antifalsificação (falha volta ao login com aviso) e grava no cookie o id, o nome (e-mail, ou `Leo`) e o e-mail; cookie de autenticação e de antifalsificação `HttpOnly`, `SameSite=Lax` e `Secure` fora de desenvolvimento; `Login.razor` com campo "E-mail", `<AntiforgeryToken />` (exige `<persist-component-state />` no `_Layout.cshtml`, senão o token some quando o circuito Blazor inicia) e mensagens para credenciais inválidas, conta não ativada (com link para `/ativar?email=…`, que agora também funciona como tela só de reenvio), bloqueio e página expirada; `MainLayout` com menu da conta (nome/e-mail e Sair). Os links "Esqueci a senha" (E8-S7) e "Alterar senha" (E8-S8) entram nessas stories. 9 testes (`LoginTests`); fluxo verificado em navegador.
- [x] **E8-S7** — **Esqueci a senha (`/esqueci-senha` e `/redefinir-senha`).** Informa o e-mail; sempre responde "se o e-mail existir, enviamos instruções" (sem enumeração). Link com token de 1 h e uso único leva à tela de nova senha + confirmação. Só para contas ativadas (inclui o Leo). Limite de frequência por e-mail/IP.
  **Implementação:** `Pages/EsqueciSenha.razor` (formulário HTML comum com token antifalsificação que faz POST em `AccountController.ForgotPassword`, o único ponto com acesso ao IP; sempre volta para `?sent=1`); `Pages/RedefinirSenha.razor` (checa o link sem consumi-lo, formulário de nova senha + confirmação, telas de sucesso e de link inválido/expirado); `Identity/PasswordResetService` (`RequestAsync` só envia para conta ativada, inclusive o Leo, e termina igual para e-mail inexistente, pendente, inválido ou limitado; `ResetAsync` troca a senha, tira o bloqueio por tentativas, e envia o aviso de senha alterada); `Identity/PasswordResetTokenProvider` com validade de 1 h (ativação continua 24 h); o token é de uso único porque a troca de senha renova o `SecurityStamp`, e um token de ativação não serve para redefinir; `Identity/PasswordResetLimiter` (5 e-mails por endereço e 20 por IP, por hora, contados mesmo quando o e-mail não existe) sobre `SlidingWindowLimiter` (em memória, com limpeza de chaves antigas); `Identity/ClientIp` lê `CF-Connecting-IP`, depois `X-Forwarded-For`, e só serve para limitar frequência. Link "Esqueci a senha" na tela de login. As outras sessões do usuário só passam a ser derrubadas em E8-S8. 18 testes (`PasswordResetTests`); fluxo verificado em navegador, inclusive em modo Production.
- [x] **E8-S8** — **Alterar senha (`/conta/senha`).** Usuário logado informa senha atual, nova senha e confirmação. Ao concluir, as outras sessões são invalidadas (`SecurityStamp`) e um e-mail de aviso é enviado.
  **Implementação:** `Pages/AlterarSenha.razor` (formulário HTML com token antifalsificação que faz POST em `AccountController.ChangePassword`, para o cookie novo sair na mesma resposta); `Identity/ChangePasswordService` (senha atual, confirmação, política de senha e e-mail de aviso; falha de envio só vai para o log); `AuthPrincipal` grava o `SecurityStamp` e, para o Leo, a claim `ptron:reservado`; `SessionStampValidator` recusa o cookie quando o carimbo não bate ou a conta não está ativa (e-mail não confirmado); `SessionCircuitHandler` manda o circuito Blazor que ainda tem o carimbo antigo para `/account/logout`. O login passa a usar o mesmo principal. Link "Alterar senha" no menu da conta. 8 testes (`ChangePasswordTests`).
- [x] **E8-S9** — **Demais áreas por usuário.** (a) Console SQL e menu "SQL" visíveis/permitidos **somente ao Leo** (`Reservado`). (b) Fotos em `uploads/{userId}/` com nomes GUID; exclusão/limpeza restritas ao dono. (c) O seed antigo ("se o banco está vazio" com insumos de exemplo) é removido; os tipos padrão passam a ser criados por usuário na ativação (S5). (d) Todas as páginas exigem usuário autenticado e ativo; `RedirectToLogin` mantido.
  **Implementação:** menu e atalho da home só aparecem para o principal reservado (a claim ou, em cookies antigos, o id `leo`); `/sql` exige a política `Reserved`; `SqlConsoleService` consulta `Reservado` antes de qualquer comando. `ImageUploadService` grava em `uploads/{userId}/{guid}.ext` e `Delete` só apaga arquivo dessa pasta; fotos antigas em `uploads/{arquivo}` continuam do Leo. `SeedData` foi removido (os 6 tipos padrão seguem em `ActivationService`, na ativação). Páginas continuam com `[Authorize]` e `RedirectToLogin`; o cookie deixa de valer se o e-mail não estiver confirmado. 5 testes (`UserAreasTests`).

**API (compatibilidade)**

- [ ] **E8-S10** — **API continua funcionando como Leo.** Toda requisição `/api` autenticada pelo token fixo passa a ter `ICurrentUser = Leo` (via `ApiTokenMiddleware`), de modo que *query filters*, carimbo de `UserId` e uploads (`uploads/leo/` ou pasta existente) funcionem sem alterar rotas, DTOs ou o app Expo. Sem token válido, continua `401`. Teste de regressão cobrindo as rotas principais com o token fixo. O `ApiTokenMiddleware` já grava o claim do Leo (feito em S2 para os filtros não esvaziarem a API) e as fotos novas da API caem em `uploads/leo/` (S9). Falta o teste de regressão das rotas.

**Qualidade e operação**

- [ ] **E8-S11** — **Testes, documentação e operação.** Testes em `tests/PTRON.Tests`: isolamento entre usuários (A não lê/edita/exclui dados de B, nem referencia insumo de B), cadastro/ativação/redefinição (com `IAppEmailSender` falso), bloqueio de login sem ativação, política de senha, tipos padrão criados uma única vez, Leo com dados migrados e apelido `Leo`. Habilitar WAL + `busy_timeout`; backup periódico do `ptron.db` (`sqlite3 .backup`) no servidor; `build.yml` preserva a pasta de chaves; README documenta as variáveis de ambiente (D5), `PublicBaseUrl`, usuários e backup; este `plano.md` atualizado.

### Ordem sugerida dentro do épico

`E8-S1` → `E8-S2` → `E8-S3` → `E8-S4` → `E8-S5` → `E8-S6` → `E8-S7` → `E8-S8` → `E8-S9` → `E8-S10` → `E8-S11`.
(S3 pode rodar em paralelo a S2; S4–S8 dependem de S1 e S3; S9–S10 dependem de S2 e S6.)

**Atenção:** o app mobile continua com o token fixo e, portanto, só acessa os dados do Leo até o épico futuro de login mobile.

### Fora do escopo deste épico

Login social (Google etc.), autenticação em dois fatores, troca de e-mail da conta, exclusão de conta, perfis/papéis além do Leo, compartilhamento de dados entre usuários, painel administrativo de usuários, migração para PostgreSQL, **app mobile (login por usuário, cadastro e recuperação de senha no celular, JWT/refresh token) — épico futuro**, deep links do e-mail para dentro do app.

### Pontos a confirmar (com padrão sugerido)

- **C1** — Senha inicial do Leo: usar `PTRON_Leo__InitialPassword` se existir; senão cair na senha atual (e você a troca no primeiro login). *Padrão: assim.*
- **C2** — O token fixo da API passa a representar o Leo (D7), de modo que o app mobile atual continua funcionando apenas com os dados dele. *Padrão: assim.*
- **C3** — Links de ativação/redefinição abrem páginas web (inclusive no celular). *Padrão: assim.*

---

## Épico 9 — Entrada de Estoque com 3 moedas ✅

A entrada deixa de ter uma única moeda. Produtos, impostos e frete podem estar em moedas diferentes. O custo do estoque continua gravado em **reais**.

### Decisões

| # | Decisão |
|---|---------|
| D1 | Moedas suportadas, lista fixa: **BRL, USD, EUR, GBP, CNY**. |
| D2 | A moeda destino é **sempre BRL**. O custo unitário do insumo e o snapshot do produto não mudam de moeda. Escolher a moeda por usuário fica para depois (depende do país do Épico 8, que está em pausa). |
| D3 | Dois câmbios, informados na hora e gravados na entrada: produtos → reais, e impostos → reais. Sem cotação automática. Se produtos e impostos estão na mesma moeda, vale o câmbio dos produtos. Real ignora câmbio (fica 1). |
| D4 | O frete fica na moeda dos produtos, na moeda dos impostos ou em reais. O padrão da tela nova é reais; uma entrada antiga migra com o frete na moeda que ela já usava. |
| D5 | O rateio continua pela proporção do valor dos produtos (ou pela quantidade, se todos os preços forem zero). Frete e impostos são rateados na moeda em que foram informados; a última linha absorve o arredondamento de 4 casas. O custo *landed* em reais é `subtotal × câmbio dos produtos + frete rateado × câmbio do frete + imposto rateado × câmbio dos impostos`. |
| D6 | A migração copia a moeda e o câmbio antigos para produtos, impostos e frete, com destino BRL. Itens e custos já gravados não são recalculados. |
| D7 | A API aceita o corpo antigo (`moeda` + `cambio`, uma moeda para tudo) e o corpo novo. A resposta traz os campos novos e mantém `moeda`/`cambio` como a moeda e o câmbio dos produtos. |

### Stories

- [x] **E9-S1** — **Modelo e migração.** `EntradaEstoque` guarda `MoedaProdutos`, `CambioProdutos`, `MoedaImpostos`, `CambioImpostos`, `MoedaFrete` e `MoedaDestino`. A migration `EntradaTresMoedas` renomeia `Moeda`/`Cambio` para a moeda e o câmbio dos produtos e preenche impostos, frete e destino sem alterar `CustoUnitario` dos itens.
- [x] **E9-S2** — **Cálculo.** `EntradaCustoCalculator` converte cada moeda com o próprio câmbio e mantém o custo médio ponderado em reais.
- [x] **E9-S3** — **Telas web.** Nova entrada, pré-visualização, histórico e detalhe mostram cada valor na sua moeda e o total em reais.
- [x] **E9-S4** — **API e app mobile.** Preview e finalização aceitam as três moedas; o app Expo usa a mesma lista e os mesmos câmbios.
- [x] **E9-S5** — **Testes.** Casos de reais, dólar único (comportamento anterior), produtos em dólar com impostos em reais, frete em reais, euro e dólar com câmbios diferentes, payload antigo da API, e migração de uma entrada em dólar sem mudar o custo gravado.

---

## Épico 10 — Página de boas-vindas ✅

Hoje quem abre o endereço cai no login. Quem já entrou vê só o início do aplicativo (atalhos). Este épico coloca na frente uma página pública, no espírito da [home do Onshape](https://www.onshape.com/en/): a pessoa entende o produto e escolhe **Criar conta** ou **Entrar**. O login continua em `/login`.

A referência é a estrutura, não o conteúdo. O Onshape vende CAD para times e pode mostrar clientes, setores e "10× mais rápido". O Makelectron serve para o produto final feito de muitas peças. É aí que estoque, produção e custo deixam de caber na planilha ou na memória. Nasceu na eletrônica, onde um equipamento junta muita coisa, e o mesmo fluxo vale para o artesão ou para qualquer outro produto montado peça por peça. A página só promete o que o sistema já faz.

### Posicionamento

Uma frase, dita para quem monta um produto de lista longa:

> **Muitas peças. Tudo sob controle.**
>
> Estoque, produção e o custo de cada unidade.

O apoio, logo abaixo: quando o produto final é feito de muitas partes, controlar isso na mão deixa de funcionar. O Makelectron guarda a lista, confere o estoque na hora de produzir e calcula o custo em reais. Feito para eletrônica, e do mesmo jeito para artesanato ou qualquer outra montagem.

Dois botões, sempre visíveis no topo e de novo no fim da página:

- **Criar conta** — preenchido, leva a `/cadastro`.
- **Entrar** — contorno, leva a `/login`.

### Decisões

| # | Decisão |
|---|---------|
| D1 | `/` passa a ser público. Visitante anônimo vê a boas-vindas e **não** é mandado para `/login`. |
| D2 | Quem já está autenticado e abre `/` continua no início do aplicativo (os atalhos de hoje). A página de marketing é para quem ainda não entrou; quem usa todo dia não deve atravessá-la para trabalhar. |
| D3 | A página tem layout próprio, sem gaveta nem menu do aplicativo. Barra fixa: logo, Entrar, Criar conta. Tema claro fixo, com o logo e a cor primária que o app já usa. Sem alternância claro/escuro aqui — a primeira impressão é a mesma para todo mundo. |
| D4 | Texto em português. O caso é o produto final de muitas peças, em qualquer ofício: eletrônica, artesanato ou outra montagem. Não é uma ferramenta para um item de um material só — o valor aparece quando a lista é longa. Sem uma faixa por ofício e sem número inventado de peças ("dezenas", "10×"). Sem tabela de preço e sem "grátis por N meses". Criar conta é o convite. |
| D5 | Nada inventado. Sem depoimento, logo de cliente, número de usuários ou frase do tipo "10× mais rápido". Sem captura de tela falsa. O visual do miolo mostra muitas peças entrando num modelo e saindo uma unidade com custo em R$, passando por estoque e produção. |
| D6 | O aplicativo de celular **não entra** na página. Hoje ele autentica com o token fixo e só enxerga os dados do Leo. Prometer "também no celular" para quem acabou de criar conta seria mentira. Isso volta quando existir o épico de login no app. |
| D7 | Login, cadastro, ativação, esqueci a senha e redefinição continuam anônimos e no lugar em que estão. As áreas de trabalho (tipos, insumos, equipamentos, entradas, produção, produtos, SQL) continuam exigindo sessão. |
| D8 | A página não diz que a conta já chega pronta para outros ofícios. Na ativação, os seis tipos padrão continuam os de eletrônica (resistor, capacitor, diodo, transistor, CI, válvula). A pessoa cria os tipos que quiser — madeira, tecido, frasco — porque tipo é só um nome. Trocar ou tornar opcional essa lista padrão fica fora deste épico. |

### A página, de cima para baixo

1. **Barra.** Logo Makelectron à esquerda. À direita, Entrar e Criar conta.
2. **Abertura.** A frase e o apoio acima, os dois botões, e o diagrama do fluxo ao lado (no celular, o diagrama vai para baixo do texto).
3. **Para quem é.** Uma linha, não um grid de setores: quem faz um produto final de muitas peças — equipamento eletrônico, trabalho de artesão ou qualquer outra montagem. O exemplo visual é o diagrama único; não há ilustração separada por ofício.
4. **O que hoje dá errado.** Três situações, as que a lista longa cria:
   - A lista não cabe na cabeça. Na hora de montar, falta uma peça no meio de muitas.
   - Cada peça entrou com um preço, às vezes em outra moeda. O custo da unidade não fecha.
   - Produziu uma unidade, ou desmontou: o estoque de cada peça tem de andar junto.
5. **Como funciona.** Quatro passos, na ordem em que a pessoa usa o sistema:
   1. Cadastra cada peça — tipo, foto, saldo e custo. O tipo é um nome: resistor, madeira, tecido, frasco.
   2. Monta o modelo — a lista inteira do que o produto final leva.
   3. Dá entrada no estoque — produtos, impostos e frete podem estar em moedas diferentes; o custo de cada peça fica em reais.
   4. Produz — confere a lista contra o saldo e para se faltar alguma peça. A unidade sai com o custo de todas elas.
6. **O que passa a ficar registrado.** Grade curta, um ícone e uma frase por item, só com capacidade que já existe: tipos com o nome que você quiser, foto da peça, a lista inteira do modelo, custo médio ponderado, compra em BRL, USD, EUR, GBP ou CNY, produção que confere o saldo de cada peça, custo da unidade congelado (mudar o modelo não reescreve o que já foi feito), exclusão da unidade devolvendo cada peça ao estoque.
7. **Fecho.** A mesma frase, de novo os dois botões.
8. **Rodapé.** Nome, a linha "Estoque, produção e custo para produtos feitos de muitas peças.", e os links Entrar e Criar conta.

### Stories

- [x] **E10-S1** — **Rota pública e layout.** `/` deixa de exigir login. Anônimo vê a boas-vindas; autenticado vê o início atual (atalhos, inclusive SQL para o Leo). Novo layout sem gaveta: barra com logo, Entrar (`/login`) e Criar conta (`/cadastro`). Tema claro fixo. `RedirectToLogin` permanece nas páginas de trabalho. Título da aba: "Makelectron".
- [x] **E10-S2** — **Abertura.** Frase "Muitas peças. Tudo sob controle.", o apoio (estoque, produção e custo do produto final de muitas partes; eletrônica, artesanato ou qualquer outra montagem) e os dois botões. Diagrama de muitas peças entrando num modelo e saindo uma unidade com custo em R$, legível, sem captura de tela. No celular o diagrama fica abaixo do texto; os botões ocupam a largura.
- [x] **E10-S3** — **Para quem é, o problema e os quatro passos.** A linha do produto de muitas peças, as três situações e a sequência cadastrar cada peça → montar a lista do modelo → dar entrada → produzir, com o texto definido acima. O exemplo do tipo (resistor, madeira, tecido, frasco) aparece no primeiro passo. Um único `h1` na página (a frase da abertura); o resto são títulos de seção.
- [x] **E10-S4** — **O que fica registrado.** Grade com as oito capacidades reais listadas na seção 6. Cada item é uma frase, não um parágrafo. Ícones do conjunto que o app já usa.
- [x] **E10-S5** — **Fecho, rodapé e leitura.** Repete a frase e os dois botões. Rodapé mínimo. A página inteira funciona em tela estreita (barra não estoura, seções empilham, botões continuam alcançáveis). Contraste do texto no tema claro e foco visível ao navegar por teclado. Meta description em português, uma frase, para quem cola o link.
- [x] **E10-S6** — **Conferência no navegador.** Anônimo em `/` não cai no login; Criar conta abre `/cadastro`; Entrar abre `/login` e, depois de autenticar, o início do app. Autenticado em `/` vê os atalhos, não a página de marketing. Tipos, insumos e as demais áreas continuam mandando para o login quando não há sessão. Desktop e celular.

**Implementação:** `WelcomeLayout` (barra, tema claro fixo) e `WelcomePage` (o texto do épico). `Index` em `/` é anônimo: sem sessão mostra a boas-vindas; com sessão mostra o início de antes (`AppHome`, com SQL para o Leo) dentro do `MainLayout`. A abertura usa `fancy_image.jpg` (peças, lista, estoque, produção e o custo). Conferido no navegador em tela larga e no celular: anônimo não cai no login, Criar conta abre `/cadastro`, `/tipos` volta para `/login`, e a conta do Leo em `/` vê os atalhos.

### Ordem sugerida dentro do épico

`E10-S1` → `E10-S2` → `E10-S3` → `E10-S4` → `E10-S5` → `E10-S6`.

### Fora deste épico

Blog, changelog, depoimentos, preços, captura de tela do produto, página "sobre", e qualquer menção ao aplicativo de celular. A página nasce em português; inglês e espanhol ficam no Épico 11. O Épico 8 continua em pausa.

---

## Épico 11 — Idiomas (português, inglês e espanhol) 🚧

A interface, os e-mails e o aplicativo estão em português do Brasil. Este épico acrescenta inglês e espanhol, e deixa a pessoa escolher. Vem **depois** do Épico 10: a página de boas-vindas nasce em português e aqui passa a existir nos três idiomas, junto com o resto do produto.

O que a pessoa cadastrou — nome de tipo, peça, modelo, descrição de produto — não se traduz. Isso é conteúdo dela.

### Decisões

| # | Decisão |
|---|---------|
| D1 | Três idiomas: **português (pt-BR)**, **inglês (en)** e **espanhol (es)**. O português continua o do Brasil, que já está no produto. O espanhol é o da América Latina, sem tratar o da Espanha como quarto idioma. O inglês é um só (`en`). |
| D2 | O padrão continua o português. Conta já existente, inclusive o Leo, fica em pt-BR. Nada que já está gravado muda de idioma sozinho. |
| D3 | Visitante sem escolha usa o idioma do navegador, se for um dos três; senão, português. Um seletor grava um cookie. Na conta, o idioma fica em `ApplicationUser` e vale mais que o cookie: ao entrar, a interface segue a conta. O cadastro grava o idioma da página em que a pessoa criou a conta. |
| D4 | O seletor mostra **Português**, **English** e **Español**, cada um escrito na própria língua. Fica na barra da página pública (Épico 10), nas telas de entrar e criar conta, e na barra do aplicativo. No celular, fica nas configurações. |
| D5 | Textos da interface em recursos do ASP.NET (`IStringLocalizer`), inclusive as mensagens do MudBlazor (paginador e rótulos prontos), os erros de identidade e a lista de países. Brasil continua no topo da lista; o nome do país muda com o idioma, o código ISO não. |
| D6 | Dinheiro não muda de moeda. O custo segue em reais, com **R$**. O separador de milhar e de decimal, e o formato da data, seguem o idioma ativo. Em inglês, `R$ 1,234.56`; em português e espanhol, `R$ 1.234,56`. |
| D7 | E-mail de ativação, de redefinição e de senha alterada sai no idioma da conta. No cadastro e no "esqueci a senha", sai no idioma da página, porque a conta ainda pode não ter essa preferência gravada. |
| D8 | Os seis tipos criados na ativação nascem no idioma da conta. Em português continuam Resistor, Capacitor, Diodo, Transistor, CI e Válvula. Inglês e espanhol têm o equivalente nos recursos. Conta já ativada não é renomeada. |
| D9 | A API devolve a mensagem de erro no idioma do cabeçalho `Accept-Language` e, sem ele, em português. O aplicativo envia esse cabeçalho conforme a escolha feita nele. |

### Stories

- [x] **E11-S1** — **Fundação.** Culturas `pt-BR`, `en` e `es` no `UseRequestLocalization`, padrão pt-BR, cookie e `Accept-Language`. `IStringLocalizer` e localização do MudBlazor. `html lang` acompanha o idioma. Seletor que grava o cookie e recarrega. `Format` usa a cultura ativa para número, percentual e data; o símbolo da moeda permanece (R$ para real).
- [x] **E11-S2** — **Idioma da conta.** Campo `Idioma` em `ApplicationUser` (migração; contas existentes e o Leo ficam `pt-BR`). O seletor, com sessão, grava na conta e no cookie. O login aplica o idioma da conta. O cadastro grava o idioma da página.
- [x] **E11-S3** — **Textos da web.** Páginas, diálogos, menu, avisos, validação, erros de identidade, títulos e a página de boas-vindas do Épico 10. Lista de países nos três idiomas. Um teste confirma que a mesma chave difere nas três culturas.

  **Implementação:** textos em `Resources/SharedResource*.resx` (chave = texto em português; `pt-BR` é o padrão, `en` e `es` traduzem). Páginas e diálogos usam `IStringLocalizer`; serviços, erros de identidade e validação do cadastro usam `StaticText`. Países em três idiomas (`Paises`), Brasil primeiro. A API segue em português até a S6 (provedor de cultura fixo em `/api`). `WebTextsTests` falha se uma chave usada no código faltar em algum idioma, se en/es forem cópia do português ou se os `{0}` não casarem.
- [x] **E11-S4** — **E-mails.** Ativação, redefinição de senha e aviso de senha alterada, assunto e corpo, nos três idiomas, pela regra da D7.

  **Implementação:** os textos dos três e-mails estão nos recursos (`EmailTemplates` usa `StaticText`). A língua é a cultura ativa na hora do envio: cadastro e "esqueci a senha" usam a da página; a ativação reenviada e o aviso de senha alterada rodam sob `CultureScope.Use(user.Idioma)`, a língua da conta. `EmailLanguageTests` cobre os três idiomas e a regra da conta.
- [~] **E11-S5** (descartada)  — **Tipos padrão.** A ativação cria os seis nomes no idioma da conta. Quem já ativou conserva os nomes que tem.
- [ ] **E11-S6** — **API e aplicativo.** Mensagens de erro da API seguem `Accept-Language`. As telas do app Expo e o formato de número e data seguem um idioma guardado no aparelho, padrão português, com seletor nas configurações. O app envia `Accept-Language`.
- [ ] **E11-S7** — **Conferência.** Na web: trocar o idioma na boas-vindas, no login e dentro do app; a escolha sobrevive a recarregar; a conta autenticada manda no idioma; um e-mail de ativação sai na língua da página (no log, em desenvolvimento). No celular: trocar o idioma e ver uma tela de trabalho acompanhando. Cada texto é escrito nos três idiomas no mesmo conjunto de mudanças, não deixado para uma passagem automática rever depois.

### Ordem sugerida dentro do épico

`E11-S1` → `E11-S2` → `E11-S3` → `E11-S4` → `E11-S5` → `E11-S6` → `E11-S7`.

(S4 e S5 dependem de S2 e S3. S6 depende de S1.)

### Fora deste épico

Traduzir o que o usuário digitou. Português de Portugal como idioma separado. Mais idiomas além destes três. Tradução automática de nomes de peças. O Épico 8 continua em pausa.

---

## Épico 12 — Código aberto ⬜

O repositório vai ficar público. Este épico deixa o código, a documentação e o serviço hospedado prontos para isso. A primeira parte (licença MIT, `SECURITY.md`, seção de licença no README e o link "Código aberto" no rodapé da boas-vindas) já está no PR #32. Aqui ficam os itens que sobraram.

### Decisões

- **D1.** Licença MIT, com Leonardo Pedrosa como titular. Se for outra (por exemplo AGPL, para impedir cópia hospedada fechada), decidir **antes** de tornar público; trocar depois é difícil.
- **D2.** O link para o código fica só no rodapé da boas-vindas. Não vai no topo nem no destaque, que seguem com "Criar conta" e "Entrar".
- **D3.** A licença cobre o código. Nome e logotipo não entram nela; o README diz isso.

### Stories

- [x] **E12-S1** — Licença MIT, `SECURITY.md`, seção de licença no README (PT/EN) e link no rodapé da boas-vindas. *(PR #32)*
- [ ] **E12-S2** — **Segredos.** `Api:Token` está commitado em `appsettings.json` (`ptron-dev-token-...`). Produção passa a ler de variável de ambiente ou segredo; o valor do repositório vira só um marcador de desenvolvimento. Se o valor já foi usado em produção, trocar. Conferir também `appsettings.Development.json` e a configuração do SMTP.
- [ ] **E12-S3** — **E-mail pessoal.** `pedrosa.leonardo@gmail.com` aparece em `appsettings.json`, `LeoOptions.cs`, na migração `IdentityFoundation`, nos testes e no `plano.md`. Decidir se fica público; se não, passar o valor do Leo para configuração e usar um endereço neutro nos testes e na documentação. A migração já aplicada não pode ser editada sem cuidado.
- [ ] **E12-S4** — **Dados e histórico.** Reconferir que nenhum `.db`, foto de usuário ou chave entrou no histórico do git (a conferência inicial não achou nada), com uma ferramenta de varredura de segredos (gitleaks ou trufflehog). Conferir que `.gitignore` cobre tudo.
- [ ] **E12-S5** — **Nome, logotipo e imagens.** Escolher um nome público (a interface diz Makelectron, o repositório diz PTRON). Confirmar que logotipo e `fancy_image.jpg` podem ser publicados e dizer no README o que a licença cobre.
- [ ] **E12-S6** — **README.** Captura de tela, texto curto de "por quê", estado beta, como rodar o próprio exemplar (hospedado ou próprio) e links para licença, `SECURITY.md` e contribuição.
- [ ] **E12-S7** — **Comunidade.** `CONTRIBUTING.md`, modelos de issue (erro e sugestão) e modelo de pull request. `CODE_OF_CONDUCT.md` se houver contribuições externas.
- [ ] **E12-S8** — **Integração contínua.** Fluxo do GitHub Actions que compila e roda os testes em cada pull request.
- [ ] **E12-S9** — **Revisão de segurança do serviço público.** Conferir autorização e limite de requisições no cadastro, na API `/api` e no envio de fotos; confirmar que a configuração de produção não tem segredos no repositório; relato privado de vulnerabilidades ativado no GitHub.
- [ ] **E12-S10** — **Privacidade e termos.** Página curta de privacidade e termos, linkada no rodapé, enquanto o serviço guardar dados de outras pessoas. Aparece nos três idiomas se o Épico 11 já estiver feito.
- [ ] **E12-S11** — **Documentos e versões.** Decidir onde ficam `plano.md` e `especificacao.md` (por exemplo `docs/`) e se terão versão em inglês. Criar `CHANGELOG.md` e a primeira tag de versão.
- [ ] **E12-S12** — **Tornar público.** Última conferência (S2 a S5 e S9 feitas), trocar a visibilidade do repositório para pública e conferir o link do rodapé na boas-vindas.

### Ordem sugerida dentro do épico

`E12-S2` → `E12-S3` → `E12-S4` → `E12-S5` → `E12-S6` → `E12-S7` → `E12-S8` → `E12-S9` → `E12-S10` → `E12-S11` → `E12-S12`.

(S2 a S5 e S9 **bloqueiam** S12. S6 a S8, S10 e S11 podem seguir depois de público, mas é melhor antes.)

### Fora deste épico

Aceitar contribuições de código no ritmo de um projeto grande, empacotamento (Docker, instalador) e traduzir a documentação para outros idiomas.

---

## Ordem sugerida de execução

~~E0~~ → ~~E1~~ → ~~E2~~ → ~~E3~~ → ~~E4~~ → ~~E5~~ → ~~E6~~ → ~~E7~~ ✅ → E8 (em pausa, S1–S9 feitas) → ~~E9~~ ✅ → **E10** → **E11** → **E12**

**E8 em pausa.** S1 a S9 concluídas. Faltam S10 (regressão da API) e S11 (testes de isolamento, WAL, backup e README).

**E9 concluído.** Entrada de estoque com moeda dos produtos, moeda dos impostos e moeda do frete; custo do estoque em reais.

**E10 concluído.** Quem não entrou vê a página de boas-vindas. Quem já entrou continua no início do aplicativo.

**E11 a fazer, depois do E10.** Português, inglês e espanhol na web, nos e-mails e no aplicativo. O padrão continua o português.

**E12 em andamento.** S1 feita. Faltam segredos, e-mail pessoal, varredura do histórico, nome e imagens, README, comunidade, CI, revisão de segurança, privacidade, documentos e a troca para público.
