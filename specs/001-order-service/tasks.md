# Tasks — Plano TDD (3 dias)

**Feature:** `001-order-service`  
**Regra:** cada task de domínio/application começa pelo teste vermelho.

## Dia 1 — Fundação + Domínio

### T01 — Estrutura da solution
- [ ] Criar `src/` com projetos Domain, Application, Infrastructure, Api
- [ ] Criar `tests/` UnitTests + IntegrationTests
- [ ] Referências Clean Architecture corretas
- [ ] Remover template WeatherForecast
- [ ] Atualizar `OrderService.slnx` / solution

### T02 — Domínio Order (TDD)
- [ ] Testes: Create com itens válidos → Placed + Total correto
- [ ] Testes: Create sem itens → EmptyOrderItemsException
- [ ] Testes: Quantity <= 0 → exceção
- [ ] Implementar `Order`, `OrderItem`, `OrderStatus`

### T03 — Domínio Product + máquina de estados (TDD)
- [ ] Reserve / Release / InsufficientStock
- [ ] Confirm Placed→Confirmed; Confirm duplicado no-op no agregado (ou via handler)
- [ ] Cancel Placed/Confirmed; Cancel inválido de Canceled→Confirm

### T04 — Application skeleton
- [ ] MediatR + FluentValidation + ValidationBehavior
- [ ] Ports: `IOrderRepository`, `IProductRepository`, `IUnitOfWork`, `IJwtTokenService`
- [ ] DTOs + `PagedResult`

## Dia 2 — Use cases + Infra + API

### T05 — CreateOrder (TDD handler)
- [ ] Validator FluentValidation
- [ ] Handler carrega produtos, cria Order, persiste
- [ ] Testes com fakes/mocks

### T06 — ConfirmOrder / CancelOrder (TDD)
- [ ] Transação + estoque
- [ ] Idempotência
- [ ] Concorrência (teste de unidade + nota de integração)

### T07 — Queries Get/List
- [ ] GetById 404
- [ ] List com filtros + paginação + limites

### T08 — Infrastructure EF Core
- [ ] `OrderDbContext` + Fluent configs + snake_case
- [ ] Repositories + UnitOfWork
- [ ] Migrations iniciais
- [ ] Seed de produtos
- [ ] `RowVersion` / concurrency

### T09 — JWT Auth
- [ ] `POST /auth/token`
- [ ] `JwtTokenService`
- [ ] `[Authorize]` em Orders

### T10 — Api Controllers + Exception middleware
- [ ] Controllers conforme `api-contracts.md`
- [ ] Problem Details
- [ ] Swagger com Bearer

## Dia 3 — Docker, resiliência, polimento

### T11 — Docker Compose
- [ ] Dockerfile Api
- [ ] `docker-compose.yml` (api + postgres)
- [ ] Migrate + seed no startup
- [ ] Healthcheck básico

### T12 — Resiliência
- [ ] CancellationToken E2E
- [ ] EF CommandTimeout + request timeout
- [ ] Polly retry mínimo **ou** ADR de exclusão

### T13 — Testes de integração (mínimo)
- [ ] WebApplicationFactory + Testcontainers **ou** postgres compose
- [ ] Fluxo: token → create → confirm → get → cancel
- [ ] Idempotência confirm/cancel

### T14 — Docs de entrega
- [ ] README (rodar, testar, usar, exemplos curl)
- [ ] `docs/decisions.md` consolidado
- [ ] Revisar checklist MUST do enunciado

### T15 — Hardening final
- [ ] Índices de listagem
- [ ] `AsNoTracking` em reads
- [ ] Remover código morto
- [ ] `dotnet test` + `docker compose up` validação manual

## Ordem SDD (não pular)

```text
research ✓ → spec ✓ → design ✓ → data-model ✓ → api ✓ → resilience ✓ → naming ✓ → tasks (agora) → implement
```

## Estimativa de risco

| Risco | Mitigação |
|-------|-----------|
| Oversell em Confirm concorrente | RowVersion + transação + teste |
| Tempo curto em integração | Priorizar unitários de domínio; 1–2 testes API |
| Over-engineering Polly/OTel | Seguir `resilience.md` WONT list |
| Pastas monolíticas atuais | Migrar cedo (T01) para `src/` multi-project |
