# Design — Arquitetura & Padrões

**Feature:** `001-order-service`  
**Estilo:** Clean Architecture + CQRS (MediatR) + DDD prático

## 1. Diagrama de camadas

```text
                    ┌─────────────────────────────┐
                    │     OrderService.Api        │
                    │ Controllers, Auth, Middleware│
                    │ Filters, DI composition     │
                    └──────────────▲──────────────┘
                                   │
                    ┌──────────────┴──────────────┐
                    │  OrderService.Application   │
                    │ Commands/Queries, Handlers  │
                    │ Validators, Behaviors, DTOs │
                    │ Ports (interfaces)          │
                    └──────────────▲──────────────┘
                                   │
              ┌────────────────────┼────────────────────┐
              │                    │                    │
┌─────────────┴──────────┐ ┌───────┴────────┐ ┌────────┴──────────┐
│ OrderService.Domain    │ │                │ │ OrderService.     │
│ Entities, VOs, Enums   │ │   depends on   │ │ Infrastructure    │
│ DomainExceptions       │ │   Domain only  │ │ EF, JWT, Polly,   │
│ Domain Services        │ │                │ │ Repositories      │
└────────────────────────┘ └────────────────┘ └───────────────────┘
```

**Regra de dependência:** Api → Application → Domain ← Infrastructure  
Infrastructure referencia Application (para implementar ports) e Domain.

## 2. Estrutura de projetos (alvo)

```text
src/
  OrderService.Domain/
    Entities/
    ValueObjects/
    Enums/
    Exceptions/
    Repositories/          # interfaces de repositório (ports de domínio) OU em Application
  OrderService.Application/
    Abstractions/          # IOrderRepository, IProductRepository, IUnitOfWork, IDateTimeProvider
    Orders/
      Commands/
        CreateOrder/
        ConfirmOrder/
        CancelOrder/
      Queries/
        GetOrderById/
        ListOrders/
    Auth/
      Commands/IssueToken/
    Behaviors/
      ValidationBehavior.cs
    Common/
      Models/PagedResult.cs
      Exceptions/          # ApplicationException hierarchy se necessário
    DependencyInjection.cs
  OrderService.Infrastructure/
    Persistence/
      OrderDbContext.cs
      Configurations/
      Repositories/
      Migrations/
      Seed/
    Auth/
      JwtTokenService.cs
    Resilience/
      PollyPolicies.cs
    DependencyInjection.cs
  OrderService.Api/
    Controllers/
      AuthController.cs
      OrdersController.cs
    Middleware/
      ExceptionHandlingMiddleware.cs
    Extensions/
    Program.cs
tests/
  OrderService.UnitTests/
    Domain/
    Application/
  OrderService.IntegrationTests/
    Api/
    Persistence/
```

## 3. Design Patterns aplicados

| Pattern | Onde | Por quê |
|---------|------|---------|
| **CQRS** | Application (Commands/Queries) | Separar escrita/leitura; alinhado Nstech |
| **Mediator** | MediatR | Controllers magros; pipeline behaviors |
| **Repository + Unit of Work** | Application ports / Infrastructure | Abstrair EF; transações em confirm/cancel |
| **Specification (leve)** | Queries de listagem | Filtros compostos sem poluir repositório |
| **Factory / factory method** | `Order.Create(...)` | Invariantes na criação |
| **Value Object** | `Money`, `Quantity`, `CustomerId` | Tipagem forte; evita primitive obsession |
| **Result / Domain Exception** | Domínio + middleware | Erros de negócio previsíveis |
| **Strategy (estoque)** | `Product.Reserve / Release` | Encapsular baixa/liberação |
| **Options Pattern** | Jwt, Pagination, Resilience | Config tipada |
| **Decorator/Pipeline** | MediatR behaviors | Cross-cutting sem poluir handlers |
| **Idempotent Consumer (estilo)** | Confirm/Cancel handlers | Mesmo resultado em replay |

## 4. SOLID — aplicação concreta

| Princípio | Aplicação |
|-----------|-----------|
| **S** | `ConfirmOrderHandler` só orquestra; `Order.Confirm()` aplica regra; `Product.Reserve()` mexe em estoque |
| **O** | Novos use cases = novos handlers; domínio aberto a extensão via métodos claros |
| **L** | Repositórios concretos substituíveis por fakes nos testes |
| **I** | `IOrderReadRepository` vs `IOrderWriteRepository` se necessário; evitar God-interface |
| **D** | Application depende de abstrações; Infrastructure injeta implementações |

## 5. Fluxos principais

### 5.1 Create Order

```text
HTTP POST /orders
  → AuthZ JWT
  → OrdersController.Create
  → MediatR CreateOrderCommand
  → ValidationBehavior (FluentValidation)
  → CreateOrderHandler
       - load products
       - Order.Create(customerId, currency, items+prices)
       - persist
  → 201 Created + OrderResponse
```

### 5.2 Confirm (idempotent + stock)

```text
HTTP POST /orders/{id}/confirm
  → ConfirmOrderHandler (transaction)
       - load order (tracked)
       - if Confirmed → return same DTO (idempotent)
       - if not Placed → DomainException
       - for each item: Product.Reserve(qty) with concurrency token
       - order.Confirm()
       - SaveChanges
  → 200 OK
```

### 5.3 Cancel (idempotent + release)

```text
HTTP POST /orders/{id}/cancel
  → CancelOrderHandler (transaction)
       - if Canceled → return same DTO
       - if Confirmed → Release stock then Cancel
       - if Placed → Cancel only
       - else DomainException
  → 200 OK
```

## 6. Controllers vs Minimal APIs

**Decisão:** Controllers (`ApiController`).

Motivo:
- Autorização por atributo clara
- Swagger/OpenAPI maduro
- Alinha com template atual e avaliação de organização
- Minimal API é válida, mas Controllers facilitam leitura no review

## 7. Mapeamento de erros (camada)

| Origem | Tipo | HTTP |
|--------|------|------|
| FluentValidation | ValidationException | 400 |
| Domínio (regra) | DomainException / OrderDomainException | 409 ou 422 |
| Não encontrado | NotFoundException | 404 |
| Auth | Unauthorized / Forbidden | 401 / 403 |
| Concorrência estoque | ConcurrencyException / DbUpdateConcurrencyException | 409 |
| Infra não tratada | Exception | 500 |

Problem Details (`application/problem+json`) no middleware global.

## 8. Segurança

- `POST /auth/token` → emite JWT (claim `sub`, `role` opcional)
- Policy: `Authorize` em `OrdersController`
- Segredo JWT via env / user-secrets (nunca commitado)
- HTTPS redirection em ambiente não-docker-dev conforme necessidade

## 9. Migrations automáticas

No startup da Api (somente com flag / ambiente controlado):

```csharp
await db.Database.MigrateAsync(cancellationToken);
await ProductSeed.EnsureSeedAsync(db, cancellationToken);
```

Documentar risco em produção e justificar para o teste.

## 10. Observabilidade mínima

- Serilog request logging (SHOULD)
- Correlation id simples via header (SHOULD)
- OpenTelemetry: WONT neste prazo

## 11. O que NÃO fazer (anti-patterns do teste)

- Lógica de negócio em Controller
- Anemic Domain (setters públicos sem invariantes)
- God `OrderService` com tudo
- Retry em cima de regra de negócio
- Expor entidades EF diretamente na API
- `pageSize` ilimitado
- Testes só de “happy path”
