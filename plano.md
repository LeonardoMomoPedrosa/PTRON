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
| 8 | Gestão de Usuários | 🚧 Em andamento — S1–S4 concluídas |

> Última atualização: Épico 8 em andamento — E8-S1 a E8-S4 concluídas (identidade, dados por usuário, e-mail e cadastro). Épicos E0–E7 completos.

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

## Épico 8 — Gestão de Usuários 📝 (proposto — decisões registradas, aguardando aprovação final)

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
- [x] **E8-S4** — **Cadastro (`/cadastro`).** Formulário: e-mail, **país (lista)**, **estado (texto livre)**, senha e confirmação de senha. Lista de países ISO 3166 em português, mantida no código (`Paises`), armazenando o código ISO; Brasil no topo. Validações: formato de e-mail, unicidade, senha conforme política, e-mail/nome reservado bloqueado, estado obrigatório (máx. 100 caracteres). Cria usuário **inativo** e envia o e-mail de ativação; tela "Verifique seu e-mail". Resposta idêntica para e-mail já existente (evita enumeração de contas).
  **Implementação:** `Pages/Cadastro.razor` (formulário com validação em português, tela "Verifique seu e-mail" e link "Criar conta" na tela de login); `Identity/SignUpService` (valida e-mail, país, estado e senha; bloqueia o e-mail do Leo; cria o usuário com `EmailConfirmed=false`, gera o token e envia o e-mail de ativação; e-mail já existente devolve o mesmo sucesso, sem criar conta nem enviar e-mail; falha de envio é só registrada em log); `Identity/Paises` (249 países ISO 3166 em português, Brasil no topo, grava o código); `PortugueseIdentityErrorDescriber` (mensagens de senha/e-mail em português). O e-mail é o `UserName`. Reenvio com limite de frequência fica em E8-S5. 14 testes (`SignUpTests`).
- [ ] **E8-S5** — **Ativação (`/ativar`).** Link `…/ativar?userId=…&token=…` confirma o e-mail e ativa a conta; **ao ativar, cria os 6 tipos padrão do usuário** (idempotente). Mensagens para sucesso, link inválido/expirado e conta já ativa. Opção "Reenviar e-mail de ativação" (com limite de frequência).
- [ ] **E8-S6** — **Login por e-mail.** Campo "E-mail" (aceita também o apelido `Leo`). Usuário não ativado não entra e recebe mensagem com a opção de reenviar a ativação. Mensagem genérica para credenciais inválidas; lockout por tentativas. Login passa a usar token antifalsificação (hoje é `IgnoreAntiforgeryToken`) e cookie `Secure`/`HttpOnly`/`SameSite`. Links "Esqueci a senha" e "Criar conta". Nome/e-mail do usuário exibido no topo com menu (Alterar senha, Sair).
- [ ] **E8-S7** — **Esqueci a senha (`/esqueci-senha` e `/redefinir-senha`).** Informa o e-mail; sempre responde "se o e-mail existir, enviamos instruções" (sem enumeração). Link com token de 1 h e uso único leva à tela de nova senha + confirmação. Só para contas ativadas (inclui o Leo). Limite de frequência por e-mail/IP.
- [ ] **E8-S8** — **Alterar senha (`/conta/senha`).** Usuário logado informa senha atual, nova senha e confirmação. Ao concluir, as outras sessões são invalidadas (`SecurityStamp`) e um e-mail de aviso é enviado.
- [ ] **E8-S9** — **Demais áreas por usuário.** (a) Console SQL e menu "SQL" visíveis/permitidos **somente ao Leo** (`Reservado`). (b) Fotos em `uploads/{userId}/` com nomes GUID; exclusão/limpeza restritas ao dono. (c) O seed antigo ("se o banco está vazio" com insumos de exemplo) é removido; os tipos padrão passam a ser criados por usuário na ativação (S5). (d) Todas as páginas exigem usuário autenticado e ativo; `RedirectToLogin` mantido.

**API (compatibilidade)**

- [ ] **E8-S10** — **API continua funcionando como Leo.** Toda requisição `/api` autenticada pelo token fixo passa a ter `ICurrentUser = Leo` (via `ApiTokenMiddleware`), de modo que *query filters*, carimbo de `UserId` e uploads (`uploads/leo/` ou pasta existente) funcionem sem alterar rotas, DTOs ou o app Expo. Sem token válido, continua `401`. Teste de regressão cobrindo as rotas principais com o token fixo. O `ApiTokenMiddleware` já grava o claim do Leo (feito em S2 para os filtros não esvaziarem a API); falta o teste de regressão e a pasta de uploads por usuário (S9).

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

## Ordem sugerida de execução

~~E0~~ → ~~E1~~ → ~~E2~~ → ~~E3~~ → ~~E4~~ → ~~E5~~ → ~~E6~~ → ~~E7~~ ✅ → E8 (S1–S3)

**E8 em andamento.** S1, S2 e S3 concluídos.
