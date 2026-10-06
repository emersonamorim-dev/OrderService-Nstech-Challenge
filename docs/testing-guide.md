# Guia de Testes Manuais — OrderService

Roteiro técnico para validar a API via **Postman**, **Swagger** ou **HTTP**.  
Complementa os testes automatizados (`dotnet test`).

## 1. Pré-requisitos

| Item | Valor |
|------|--------|
| Subir stack (Docker completo) | `docker compose up --build` |
| Base URL (Docker) | `http://localhost:8080` |
| Base URL (Local / `dotnet run`) | `http://localhost:5232` |
| Swagger Docker | `http://localhost:8080/swagger` |
| Swagger Local | `http://localhost:5232/swagger` |
| Health | `/health` na mesma base URL |
| Ready (DB) | `/health/ready` |
| Live | `/health/live` |
| Usuário demo | `demo` / `demo` |
| Auth | JWT Bearer em todos os `/orders/*` |
| Guia local | [`local-run.md`](local-run.md) |

### Produtos seed

| ProductId | Nome | Preço | Estoque inicial |
|-----------|------|-------|-----------------|
| `11111111-1111-1111-1111-111111111111` | Widget A | 10.00 | 100 |
| `22222222-2222-2222-2222-222222222222` | Widget B | 25.50 | 50 |
| `33333333-3333-3333-3333-333333333333` | Widget C | 99.90 | 5 |

> Após vários confirms sem cancel, o estoque diminui. Para resetar: `docker compose down -v && docker compose up --build`.

---

## 2. Importar no Postman

Arquivos em [`docs/postman/`](postman/):

| Arquivo | Uso |
|---------|-----|
| `OrderService.postman_collection.json` | Collection com todos os endpoints + asserts |
| `OrderService.Docker.postman_environment.json` | Environment Docker (`baseUrl=http://localhost:8080`) |
| `OrderService.Local.postman_environment.json` | Environment Local (`baseUrl=http://localhost:5232`) |

### Passos

1. Abra o Postman → **Import**
2. Selecione a collection + o environment do modo desejado
3. No canto superior direito, selecione **OrderService — Docker** ou **OrderService — Local**
4. Execute na ordem:
   1. `01 — Auth` → **POST Auth Token (sucesso)**
   2. `02 — Happy Path` (sequencial)
   3. Pastas `03`, `04` e `05` conforme necessidade

O request de token grava automaticamente `accessToken`.  
O create order grava `orderId` para confirm/cancel/get.

### Collection Runner (opcional)

1. Abra a collection → **Run**
2. Selecione as pastas `01 — Auth` e `02 — Happy Path`
3. Mantenha a ordem dos requests
4. Execute — todos os testes da pasta devem ficar verdes

---

## 3. Mapa de endpoints

| # | Método | Rota | Auth | Sucesso |
|---|--------|------|------|---------|
| T01 | `POST` | `/auth/token` | Não | `200` |
| T02 | `POST` | `/orders` | JWT | `201` |
| T03 | `GET` | `/orders/{id}` | JWT | `200` |
| T04 | `GET` | `/orders` | JWT | `200` |
| T05 | `POST` | `/orders/{id}/confirm` | JWT | `200` |
| T06 | `POST` | `/orders/{id}/cancel` | JWT | `200` |
| T00 | `GET` | `/health` | Não | `200` |

### Códigos de erro esperados (Problem Details)

| Situação | HTTP |
|----------|------|
| Validação / quantidade inválida / itens vazios | `400` |
| Sem JWT / credenciais inválidas | `401` |
| Pedido ou produto não encontrado | `404` |
| Estoque insuficiente / estado inválido / concorrência | `409` |

---

## 4. Matriz de casos de teste

### 4.1 Autenticação

| ID | Caso | Request | Esperado |
|----|------|---------|----------|
| AUTH-01 | Token válido | `POST /auth/token` com `demo`/`demo` | `200` + `accessToken`, `tokenType=Bearer`, `expiresIn>0` |
| AUTH-02 | Credenciais inválidas | password errado | `401` |
| AUTH-03 | Orders sem token | qualquer `/orders` sem `Authorization` | `401` |

**Body AUTH-01**

```json
{
  "username": "demo",
  "password": "demo"
}
```

**Header para demais requests**

```http
Authorization: Bearer {{accessToken}}
Content-Type: application/json
```

---

### 4.2 Criar pedido — `POST /orders`

| ID | Caso | Payload-chave | Esperado |
|----|------|---------------|----------|
| ORD-C01 | Happy path | 2x Widget A | `201`, `status=Placed`, `total=20.00` |
| ORD-C02 | Sem itens | `items: []` | `400` |
| ORD-C03 | Quantity ≤ 0 | `quantity: 0` | `400` |
| ORD-C04 | Produto inexistente | GUID fake | `404` |
| ORD-C05 | Estoque insuficiente | Widget C qty `999` | `409` |

**Body ORD-C01**

```json
{
  "customerId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "currency": "BRL",
  "items": [
    {
      "productId": "11111111-1111-1111-1111-111111111111",
      "quantity": 2
    }
  ]
}
```

**Checklist da resposta**

- `id` GUID gerado
- `status` = `Placed`
- `total` = Σ (`unitPrice` × `quantity`)
- `items[].lineTotal` coerente
- `createdAt` preenchido

---

### 4.3 Consultar — `GET /orders/{id}`

| ID | Caso | Esperado |
|----|------|----------|
| ORD-G01 | Id existente | `200` + pedido com itens |
| ORD-G02 | Id inexistente | `404` |

---

### 4.4 Listar — `GET /orders`

Query params: `customerId`, `status`, `from`, `to`, `page`, `pageSize`.

| ID | Caso | Query | Esperado |
|----|------|-------|----------|
| ORD-L01 | Paginação padrão | `page=1&pageSize=20` | `200`, campos `items/page/pageSize/totalCount/totalPages` |
| ORD-L02 | Filtro customer | `customerId=aaaaaaaa-...` | só pedidos do cliente |
| ORD-L03 | Filtro status | `status=Placed` | só `Placed` |
| ORD-L04 | Intervalo de datas | `from`/`to` ISO-8601 | restringe por `CreatedAt` |
| ORD-L05 | Status inválido | `status=Unknown` | `400` |

Exemplo:

```http
GET /orders?customerId=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&status=Placed&page=1&pageSize=20
```

---

### 4.5 Confirmar — `POST /orders/{id}/confirm`

Pré-condição: pedido em **Placed**. Sem body.

| ID | Caso | Esperado |
|----|------|----------|
| ORD-CF01 | Confirmar Placed | `200`, `status=Confirmed`, estoque baixado |
| ORD-CF02 | Confirmar 2ª vez | `200`, permanece `Confirmed` (idempotente) |
| ORD-CF03 | Confirmar Canceled | `409` Invalid order state |
| ORD-CF04 | Id inexistente | `404` |

**Como validar baixa de estoque (conceitual)**

1. Crie pedido com qty `2` do Widget A  
2. Confirme  
3. Tente criar outro pedido com qty maior que o estoque restante esperado  
4. Deve retornar `409` quando estourar o disponível  

---

### 4.6 Cancelar — `POST /orders/{id}/cancel`

Pré-condição: pedido **Placed** ou **Confirmed**. Sem body.

| ID | Caso | Esperado |
|----|------|----------|
| ORD-CN01 | Cancelar Placed | `200`, `Canceled` (estoque ainda não havia sido baixado) |
| ORD-CN02 | Cancelar Confirmed | `200`, `Canceled` + estoque liberado |
| ORD-CN03 | Cancelar 2ª vez | `200`, permanece `Canceled` (idempotente) |
| ORD-CN04 | Id inexistente | `404` |

---

## 5. Fluxos end-to-end recomendados

### Fluxo A — Ciclo completo (obrigatório)

```text
AUTH-01 → ORD-C01 → ORD-G01 → ORD-L01 → ORD-CF01 → ORD-CF02 → ORD-CN02 → ORD-CN03
```

Valida: auth, criação, consulta, listagem, confirm idempotente, cancel com liberação de estoque.

### Fluxo B — Cancel direto de Placed

```text
AUTH-01 → ORD-C01 → ORD-CN01 → ORD-CN03
```

### Fluxo C — Validações negativas

```text
AUTH-01 → AUTH-03 → ORD-C02 → ORD-C03 → ORD-C04 → ORD-C05 → ORD-G02 → ORD-L05
```

### Fluxo D — Confirmar após cancel (estado inválido)

```text
AUTH-01 → ORD-C01 → ORD-CN01 → ORD-CF03
```

---

## 6. Execução via Swagger

1. Abra `http://localhost:8080/swagger`
2. `POST /auth/token` → copie `accessToken`
3. Clique em **Authorize** → cole o token (sem prefixo `Bearer`)
4. Execute os endpoints na ordem do Fluxo A
5. Observações:
   - Confirm/Cancel não têm body — só o `id` no path
   - Em Create, use os ProductIds da tabela seed

---

## 7. Execução via curl (smoke rápido)

```bash
# Token
TOKEN=$(curl -s -X POST http://localhost:8080/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"demo","password":"demo"}' | jq -r .accessToken)

# Create
ORDER_ID=$(curl -s -X POST http://localhost:8080/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "currency":"BRL",
    "items":[{"productId":"11111111-1111-1111-1111-111111111111","quantity":2}]
  }' | jq -r .id)

# Confirm
curl -s -X POST "http://localhost:8080/orders/$ORDER_ID/confirm" \
  -H "Authorization: Bearer $TOKEN" | jq

# Cancel
curl -s -X POST "http://localhost:8080/orders/$ORDER_ID/cancel" \
  -H "Authorization: Bearer $TOKEN" | jq
```

> No Windows PowerShell, use o Postman/Swagger ou adapte com `ConvertFrom-Json`.

---

## 8. Testes automatizados (complementar)

```bash
dotnet test OrderService.sln
```

| Projeto | Foco |
|---------|------|
| `OrderService.UnitTests` | Regras de domínio, handlers, bordas |
| `OrderService.IntegrationTests` | API + persistência |

Os testes manuais deste guia cobrem a experiência operacional (JWT real, Docker, Postman).  
Os automatizados cobrem regressão contínua das regras de negócio.

---

## 9. Checklist de aceite manual

- [ ] `GET /health` → 200
- [ ] `POST /auth/token` → JWT válido
- [ ] `/orders` sem token → 401
- [ ] Create → Placed + total correto
- [ ] Get/List com paginação e filtros
- [ ] Confirm idempotente (2x)
- [ ] Cancel de Placed e de Confirmed
- [ ] Cancel idempotente (2x)
- [ ] Validações: sem itens, qty ≤ 0, produto inexistente, estoque insuficiente
- [ ] Confirm de pedido Canceled → 409

---

## 10. Referências

- Collection Postman: [`docs/postman/`](postman/)
- Decisões técnicas: [`docs/decisions.md`](decisions.md)
- Specs: [`specs/`](../specs/)
- README principal: [`README.md`](../README.md)
