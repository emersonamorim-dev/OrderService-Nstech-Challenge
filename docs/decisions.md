# Decisões Técnicas — OrderService (Nstech Challenge)

Documento exigido pelo enunciado. Consolida as decisões da Spec Driven Development em `specs/001-order-service/`.

## D1 — Spec Driven Development na raiz `specs/`

**Decisão:** fonte da verdade em `/specs/001-order-service/`, não dentro de `src/`.

**Por quê:** mantém requisitos versionáveis e independentes do refactor de camadas; padrão profissional SDD; facilita a conversa técnica pós-entrega.

## D2 — Idioma: código EN, documentação PT

**Decisão:** classes/métodos/namespaces em inglês; specs/README/ADR em português.

**Por quê:** alinhado a empresas brasileiras maduras (Nstech) com stack internacional (.NET, MediatR, EF, Polly). Evita híbrido `CriarPedido` + `DbContext`.

## D3 — Clean Architecture multi-project em `src/`

**Decisão:** quatro projetos — Domain, Application, Infrastructure, Api — + testes em `tests/`.

**Por quê:** o enunciado exige separação clara de camadas; pastas dentro de um único `.csproj` não impõem a regra de dependência.

## D4 — Controllers (não Minimal APIs)

**Decisão:** ASP.NET Core Controllers + MediatR.

**Por quê:** autorização/atributos claros, Swagger previsível, controllers magros com CQRS.

## D5 — CQRS com MediatR + FluentValidation

**Decisão:** um handler por use case; `ValidationBehavior` no pipeline.

**Por quê:** espelha o padrão Nstech (Clean Architecture + CQRS/MediatR) sem exagerar em eventos/mensageria.

## D6 — Estado inicial `Placed` (sem Draft)

**Decisão:** `Order.Create` nasce `Placed`.

**Por quê:** atende o MUST; Draft→Placed adiciona complexidade sem requisito de edição de rascunho.

## D7 — Estoque: reserva no Confirm, liberação no Cancel

**Decisão:**
- Create: valida produtos e **pré-checa** estoque (fail-fast), mas não baixa
- Confirm: baixa estoque (transação + concurrency)
- Cancel de Confirmed: libera estoque
- Cancel de Placed: só muda status

**Por quê:** separa intenção (pedido) de compromisso (confirmação); idempotência fica natural.

## D8 — Idempotência por estado do recurso

**Decisão:** sem header `Idempotency-Key`; Confirm/Cancel já confirmados/cancelados retornam 200 com o mesmo resultado.

**Por quê:** suficiente para o enunciado; simples de testar e explicar.

## D9 — JWT local (sem Keycloak)

**Decisão:** `POST /auth/token` emite JWT HS256 com segredo configurável.

**Por quê:** MUST de autenticação atendido; Keycloak seria over-engineering no prazo.

## D10 — Exceptions, timeouts e retries (escopo)

**Decisão:**
- MUST: middleware Problem Details + exceções tipadas + `CancellationToken`
- SHOULD: EF CommandTimeout + request timeout HTTP
- WONT: Polly retry / circuit breaker — **idempotência de negócio ≠ retry técnico**
- Confirm/Cancel são idempotentes por estado do recurso; retry cego poderia mascarar falhas ou duplicar efeitos se mal aplicado

**Por quê:** o enunciado exige idempotência, não resiliência Polly. Timeouts + CT + transações + concurrency token cobrem o essencial de produção neste escopo.

## D11 — Testes

**Decisão:** xUnit + FluentAssertions; unitários de Domain/Application primeiro; integração mínima API/EF no dia 3.

**Por quê:** TDD-friendly nas regras; integração garante Docker/migrations/JWT.

## D12 — Postgres snake_case + migrations no startup

**Decisão:** Fluent API com nomes snake_case; `Database.MigrateAsync` no boot da Api (ambiente do teste).

**Por quê:** experiência `docker compose up` sem passo manual; documentar que em produção real o migrate costuma ser job separado.

## D13 — Clareza de ports de estoque + health operacional

**Decisão:**
- Renomear lookup tracked para `GetTrackedByIdsAsync` (tracking + concurrency token; sem lock pessimista)
- Expor `/health`, `/health/live`, `/health/ready`
- Healthcheck da API no `docker-compose`
- Workflow CI (`dotnet restore/build/test`)

**Por quê:** eleva testabilidade e maturidade operacional do desafio sem alterar o contrato funcional dos endpoints de negócio.

## Referências SDD

- [specs/README.md](../specs/README.md)
- [specs/001-order-service/design.md](../specs/001-order-service/design.md)
- [specs/001-order-service/resilience.md](../specs/001-order-service/resilience.md)
- [specs/001-order-service/naming.md](../specs/001-order-service/naming.md)
