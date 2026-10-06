# Naming — Idioma e Nomenclatura

**Feature:** `001-order-service`

## 1. Decisão de idioma (fechada)

| Artefato | Idioma | Motivo |
|----------|--------|--------|
| Classes, métodos, propriedades, namespaces | **Inglês** | Stack .NET/Nstech internacional; MediatR/EF/Polly em EN; PRs e libs |
| Specs, ADR, README, comentários de decisão | **Português** | Empresa brasileira; enunciado em PT; entrevista em PT |
| Mensagens de erro da API (`detail`) | **Inglês** (padrão) ou PT se preferir UX local | **Escolha:** inglês curto + `type` estável |
| Nomes de tabelas/colunas no Postgres | **snake_case inglês** | `orders`, `available_quantity` |

### Por que NÃO português no código?

Empresas brasileiras sênior (Nstech inclusa) tipicamente:
- Código em inglês (frameworks, hiring internacional, menos atrito)
- Documentação de produto/decisão em português

Misturar `CriarPedidoAsync` com `DbContext` / `IMediator` gera ruído e parece júnior em review.

**Exceção:** pastas de documentação (`docs/`, `specs/`) em português.

## 2. Catálogo oficial de nomes

### 2.1 Domain

| Tipo | Nome |
|------|------|
| Entity AR | `Order` |
| Entity | `OrderItem` |
| Entity AR | `Product` |
| Enum | `OrderStatus` (`Placed`, `Confirmed`, `Canceled`) |
| VO | `Money`, `Quantity` |
| Exceptions | `DomainException`, `InsufficientStockException`, `InvalidOrderStateException`, `OrderNotFoundException`, `ProductNotFoundException`, `EmptyOrderItemsException`, `ConcurrencyConflictException` |

#### Métodos de domínio (comportamento)

```csharp
// Order
public static Order Create(Guid customerId, string currency, IEnumerable<OrderItemData> items, DateTimeOffset now);
public void Confirm();
public void Cancel();
public bool CanConfirm { get; }
public bool CanCancel { get; }

// Product
public void Reserve(int quantity);
public void Release(int quantity);
```

### 2.2 Application — Commands / Queries / Handlers

| Use case | Command/Query | Handler | Validator |
|----------|---------------|---------|-----------|
| Emitir token | `IssueTokenCommand` | `IssueTokenCommandHandler` | `IssueTokenCommandValidator` |
| Criar pedido | `CreateOrderCommand` | `CreateOrderCommandHandler` | `CreateOrderCommandValidator` |
| Confirmar | `ConfirmOrderCommand` | `ConfirmOrderCommandHandler` | `ConfirmOrderCommandValidator` |
| Cancelar | `CancelOrderCommand` | `CancelOrderCommandHandler` | `CancelOrderCommandValidator` |
| Obter | `GetOrderByIdQuery` | `GetOrderByIdQueryHandler` | — |
| Listar | `ListOrdersQuery` | `ListOrdersQueryHandler` | `ListOrdersQueryValidator` |

#### Ports

```csharp
IOrderRepository
IProductRepository
IUnitOfWork
IJwtTokenService
IDateTimeProvider
IProductStockService // opcional; preferir métodos no Product
```

#### Resultados

```csharp
OrderDto / OrderResponse
OrderItemDto
PagedResult<T> / PagedResponse<T>
TokenResponse
OrderListFilter
```

### 2.3 Infrastructure

| Classe | Responsabilidade |
|--------|------------------|
| `OrderDbContext` | EF Core context |
| `OrderRepository` | Persistência de Order |
| `ProductRepository` | Persistência de Product |
| `UnitOfWork` | Save + transactions |
| `OrderConfiguration` | Fluent API |
| `ProductConfiguration` | Fluent API |
| `JwtTokenService` | Emissão JWT |
| `ProductSeed` | Dados iniciais |
| `PostgresResiliencePipeline` | Polly (opcional) |

### 2.4 Api

| Classe | Métodos |
|--------|---------|
| `AuthController` | `IssueToken` |
| `OrdersController` | `Create`, `Confirm`, `Cancel`, `GetById`, `List` |
| `ExceptionHandlingMiddleware` | `InvokeAsync` |
| `DependencyInjection` / `ServiceCollectionExtensions` | `AddApplication`, `AddInfrastructure`, `AddApiServices` |

### 2.5 Tests

```text
OrderTests.Confirm_WhenPlaced_ShouldBecomeConfirmed
OrderTests.Confirm_WhenAlreadyConfirmed_ShouldBeIdempotent
ProductTests.Reserve_WhenInsufficientStock_ShouldThrow
CreateOrderCommandHandlerTests.Handle_WhenProductMissing_ShouldThrow
OrdersApiTests.Confirm_Twice_ShouldReturnSameState
```

Padrão: `Method_Scenario_Expected`

## 3. Namespaces

```text
OrderService.Domain
OrderService.Domain.Entities
OrderService.Domain.Enums
OrderService.Domain.Exceptions
OrderService.Domain.ValueObjects

OrderService.Application
OrderService.Application.Abstractions
OrderService.Application.Orders.Commands.CreateOrder
OrderService.Application.Orders.Commands.ConfirmOrder
OrderService.Application.Orders.Commands.CancelOrder
OrderService.Application.Orders.Queries.GetOrderById
OrderService.Application.Orders.Queries.ListOrders
OrderService.Application.Auth.Commands.IssueToken
OrderService.Application.Behaviors
OrderService.Application.Common.Models

OrderService.Infrastructure.Persistence
OrderService.Infrastructure.Persistence.Configurations
OrderService.Infrastructure.Persistence.Repositories
OrderService.Infrastructure.Auth
OrderService.Infrastructure.Resilience

OrderService.Api.Controllers
OrderService.Api.Middleware
OrderService.Api.Contracts.Requests
OrderService.Api.Contracts.Responses
```

## 4. Convenções C#

- Classes/records sealed quando não há herança intencional
- `Async` suffix em métodos async públicos
- Preferir `record` para Commands/Queries/DTOs imutáveis
- Evitar abreviações (`qty` → `quantity` em APIs públicas; domínio pode usar `quantity`)
- Booleanos: `CanConfirm`, `IsTransient` — não `Confirmavel`
- Coleções: `IReadOnlyCollection<T>` / `IReadOnlyList<T>` nas fronteiras

## 5. Glossário rápido PT → EN (para entrevista)

| Falar em PT | Código |
|-------------|--------|
| criar pedido | `CreateOrder` |
| confirmar pedido | `ConfirmOrder` |
| cancelar pedido | `CancelOrder` |
| reservar estoque | `Reserve` |
| liberar estoque | `Release` |
| estoque insuficiente | `InsufficientStockException` |
| idempotência | idempotent confirm/cancel handlers |
