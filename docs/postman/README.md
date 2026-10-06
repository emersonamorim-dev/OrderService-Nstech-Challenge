# Postman — OrderService

Arquivos prontos para importação no Postman.

## Conteúdo

| Arquivo | Descrição |
|---------|-----------|
| `OrderService.postman_collection.json` | Todos os endpoints + scripts de teste |
| `OrderService.Docker.postman_environment.json` | API em Docker (`http://localhost:8080`) |
| `OrderService.Local.postman_environment.json` | API local `dotnet run` (`http://localhost:5232`) |

## Importação rápida

1. Postman → **Import** → collection + environment (Docker e/ou Local)
2. Selecione **OrderService — Docker** ou **OrderService — Local**
3. Rode `01 — Auth` → **POST Auth Token (sucesso)**
4. Rode a pasta `02 — Happy Path` na ordem

## Variáveis principais

| Variável | Origem |
|----------|--------|
| `baseUrl` | Environment (`http://localhost:8080`) |
| `username` / `password` | Environment (`demo` / `demo`) |
| `accessToken` | Preenchido pelo Auth Token |
| `orderId` | Preenchido pelo Create Order |
| `customerId` / `productIdA|B|C` | Seed fixo |

## Pastas da collection

| Pasta | Objetivo |
|-------|----------|
| `00 — Health` | Smoke operacional |
| `01 — Auth` | JWT sucesso/falha |
| `02 — Happy Path` | Create → Get → List → Confirm → Cancel (+ idempotência) |
| `03 — Cancel from Placed` | Cancel sem confirm prévio |
| `04 — Edge Cases` | 400/401/404/409 |
| `05 — Filtros` | Listagem por customer/status/datas |

Guia completo: [`../testing-guide.md`](../testing-guide.md)
