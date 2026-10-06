# Data Model — Domínio & Persistência

**Feature:** `001-order-service`

## 1. Ubiquitous Language (EN code / PT glossário)

| Termo PT | Termo código | Significado |
|----------|--------------|-------------|
| Pedido | `Order` | Agregado raiz de compra |
| Item do pedido | `OrderItem` | Linha do pedido |
| Produto / Estoque | `Product` | Catálogo + quantidade disponível |
| Cliente | `CustomerId` (VO/Guid) | Identificador externo do cliente |
| Situação | `OrderStatus` | Draft/Placed/Confirmed/Canceled — usamos Placed→… |
| Moeda | `Currency` | Código ISO-4217 (ex.: BRL, USD) |
| Total | `Money` / `Total` | Valor monetário do pedido |
| Reservar estoque | `Reserve` | Baixa `AvailableQuantity` no Confirm |
| Liberar estoque | `Release` | Devolve quantidade no Cancel de Confirmed |

## 2. Agregados e limites

### Aggregate Root: `Order`

```text
Order (AR)
├── Id: Guid
├── CustomerId: Guid
├── Status: OrderStatus
├── Currency: string (3)
├── Items: IReadOnlyCollection<OrderItem>
├── Total: decimal (calculado / persistido)
├── CreatedAt: DateTimeOffset (UTC)
└── RowVersion: byte[] (concurrency, opcional no Order)
```

### Entity: `OrderItem` (dentro do agregado Order)

```text
OrderItem
├── Id: Guid
├── OrderId: Guid
├── ProductId: Guid
├── UnitPrice: decimal
├── Quantity: int
└── LineTotal => UnitPrice * Quantity
```

### Aggregate Root: `Product`

```text
Product (AR)
├── Id: Guid
├── Name: string
├── UnitPrice: decimal
├── AvailableQuantity: int
└── RowVersion: byte[]  # MUST para oversell
```

> `Draft` existe no enunciado como opção; **não usamos Draft**.
> Pedido nasce `Placed` (simples, justificável, atende MUST).

## 3. Enum `OrderStatus`

```csharp
public enum OrderStatus
{
    Placed = 1,
    Confirmed = 2,
    Canceled = 3
}
```

### Máquina de estados

```text
        create
          │
          ▼
       Placed ──────confirm───► Confirmed
          │                        │
          └────────cancel──────────┴──► Canceled
```

| De | Evento | Para | Efeito estoque |
|----|--------|------|----------------|
| — | Create | Placed | Nenhum (pré-check SHOULD) |
| Placed | Confirm | Confirmed | Reserve (baixa) |
| Placed | Cancel | Canceled | Nenhum |
| Confirmed | Cancel | Canceled | Release (libera) |
| Confirmed | Confirm | Confirmed | No-op idempotente |
| Canceled | Cancel | Canceled | No-op idempotente |
| Canceled | Confirm | — | Erro de domínio |
| Placed | Confirm (estoque insuficiente) | — | Erro; estado inalterado |

## 4. Invariantes (Domain MUST)

### Order
- Deve ter **≥ 1** item
- Toda `Quantity > 0`
- `Currency` não vazia, length 3 (ISO)
- `Total == Σ(UnitPrice * Quantity)`
- `Confirm()` só de `Placed`
- `Cancel()` só de `Placed` ou `Confirmed`
- Itens imutáveis após criação (sem editar itens neste escopo)

### Product
- `AvailableQuantity >= 0`
- `Reserve(qty)`: `qty > 0` e `AvailableQuantity >= qty`, senão domínio
- `Release(qty)`: `qty > 0`; soma de volta
- `UnitPrice >= 0`

## 5. Value Objects (recomendados)

| VO | Campos | Regras |
|----|--------|--------|
| `Quantity` | `int Value` | `> 0` |
| `Money` | `decimal Amount`, `string Currency` | `Amount >= 0`, currency 3 chars |
| `CustomerId` | `Guid Value` | `!= Empty` |

Se o prazo apertar, priorizar invariantes nas entidades; VOs são SHOULD de qualidade DDD.

## 6. Exceções de domínio (nomes EN)

```text
DomainException (abstract)
├── OrderNotFoundException
├── ProductNotFoundException
├── InvalidOrderStateException
├── InsufficientStockException
├── EmptyOrderItemsException
└── ConcurrencyConflictException
```

Mensagens podem ser em português (UX/API) se desejado; **nomes de tipos em inglês**.

## 7. Persistência EF Core

### Tabelas

| Tabela | PK | Índices |
|--------|----|---------|
| `orders` | `id` | `(customer_id)`, `(status)`, `(created_at)`, composto listagem |
| `order_items` | `id` | `(order_id)`, `(product_id)` |
| `products` | `id` | — |

### Configurações Fluent API
- Precisão monetária: `decimal(18,2)`
- `Order.Items` cascade delete
- `Product.RowVersion` como concurrency token (`xmin` no Npgsql ou `bytea`)
- Nomes de colunas: **snake_case** no banco (padrão Postgres profissional)

### Queries
- Leituras: `AsNoTracking()`
- GetById: `Include(o => o.Items)`
- List: projeção para DTO + `Skip/Take` + `CountAsync` separado ou window — preferir count + page
- Confirm/Cancel: tracking + transação explícita (`BeginTransactionAsync`)

## 8. Seed mínimo

| ProductId (fix) | Name | UnitPrice | AvailableQuantity |
|-----------------|------|-----------|-------------------|
| `11111111-1111-1111-1111-111111111111` | Widget A | 10.00 | 100 |
| `22222222-2222-2222-2222-222222222222` | Widget B | 25.50 | 50 |
| `33333333-3333-3333-3333-333333333333` | Widget C | 99.90 | 5 |

Documentar IDs no README para facilitar testes manuais.

## 9. Contratos de repositório (Application)

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Order order, CancellationToken ct);
    Task<PagedResult<Order>> ListAsync(OrderListFilter filter, CancellationToken ct);
}

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task<Product?> GetByIdForUpdateAsync(Guid id, CancellationToken ct);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct);
}
```

Nomes finais em inglês — ver `naming.md`.
