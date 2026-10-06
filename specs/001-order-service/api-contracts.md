# API Contracts — REST

**Base URL:** `/`  
**Auth:** `Authorization: Bearer {token}` (exceto `/auth/token`)  
**Content-Type:** `application/json`  
**Erros:** `application/problem+json` (RFC 7807)

## 1. Endpoints

| Método | Rota | Auth | Handler / Use Case |
|--------|------|------|--------------------|
| POST | `/auth/token` | Público | `IssueTokenCommand` |
| POST | `/orders` | JWT | `CreateOrderCommand` |
| POST | `/orders/{id}/confirm` | JWT | `ConfirmOrderCommand` |
| POST | `/orders/{id}/cancel` | JWT | `CancelOrderCommand` |
| GET | `/orders/{id}` | JWT | `GetOrderByIdQuery` |
| GET | `/orders` | JWT | `ListOrdersQuery` |

## 2. Controllers & métodos (nomes finais)

### `AuthController`
```csharp
[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<TokenResponse>> IssueToken(
        [FromBody] IssueTokenRequest request,
        CancellationToken cancellationToken);
}
```

### `OrdersController`
```csharp
[ApiController]
[Authorize]
[Route("orders")]
public sealed class OrdersController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    public Task<ActionResult<OrderResponse>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken);

    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<OrderResponse>> Confirm(
        Guid id,
        CancellationToken cancellationToken);

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<OrderResponse>> Cancel(
        Guid id,
        CancellationToken cancellationToken);

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<OrderResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken);

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OrderResponse>), StatusCodes.Status200OK)]
    public Task<ActionResult<PagedResponse<OrderResponse>>> List(
        [FromQuery] ListOrdersRequest request,
        CancellationToken cancellationToken);
}
```

## 3. Request / Response DTOs

### Auth
```json
// POST /auth/token
{ "username": "demo", "password": "demo" }

// 200
{ "accessToken": "...", "expiresIn": 3600, "tokenType": "Bearer" }
```

### Create Order
```json
// POST /orders
{
  "customerId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "currency": "BRL",
  "items": [
    { "productId": "11111111-1111-1111-1111-111111111111", "quantity": 2 }
  ]
}

// 201
{
  "id": "...",
  "customerId": "...",
  "status": "Placed",
  "currency": "BRL",
  "total": 20.00,
  "createdAt": "2026-10-05T20:00:00Z",
  "items": [
    {
      "productId": "11111111-1111-1111-1111-111111111111",
      "unitPrice": 10.00,
      "quantity": 2,
      "lineTotal": 20.00
    }
  ]
}
```

### List
```http
GET /orders?customerId=&status=Placed&from=2026-01-01T00:00:00Z&to=2026-12-31T23:59:59Z&page=1&pageSize=20
```

```json
{
  "items": [ /* OrderResponse */ ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3
}
```

**Defaults:** `page=1`, `pageSize=20`  
**Limits:** `pageSize` max = 100

## 4. Status codes por operação

| Operação | 200 | 201 | 400 | 401 | 404 | 409 | 422 |
|----------|-----|-----|-----|-----|-----|-----|-----|
| IssueToken | ✓ | | inválido | | | | |
| Create | | ✓ | validação | ✓ | produto | estoque pré-check | regra domínio |
| Confirm | ✓ (idempotente) | | | ✓ | pedido | estoque/concorrência/estado | estado inválido |
| Cancel | ✓ (idempotente) | | | ✓ | pedido | | estado inválido |
| GetById | ✓ | | | ✓ | ✓ | | |
| List | ✓ | | page inválida | ✓ | | | |

> **409 Conflict:** estoque insuficiente na confirmação ou conflito de concorrência.  
> **422 Unprocessable:** transição de estado inválida (alternativa aceitável a 409 — fixar uma e documentar).  
> **Decisão fixa:** estado inválido → **409**; validação de input → **400**; not found → **404**.

## 5. Problem Details (exemplo)

```json
{
  "type": "https://orderservice.local/errors/insufficient-stock",
  "title": "Insufficient stock",
  "status": 409,
  "detail": "Product 11111111-1111-1111-1111-111111111111 has insufficient stock for quantity 10.",
  "traceId": "00-..."
}
```

## 6. Idempotência — contrato comportamental

| Endpoint | 1ª chamada | 2ª chamada (mesmo id) |
|----------|------------|------------------------|
| Confirm | Placed→Confirmed + reserve | 200 + mesmo estado; sem nova baixa |
| Cancel | →Canceled (+ release se Confirmed) | 200 + Canceled; sem novo release |

Não exige `Idempotency-Key` header neste teste; idempotência é por **estado do recurso**.
