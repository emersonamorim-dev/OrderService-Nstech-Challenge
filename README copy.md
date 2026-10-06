# 🚀 OrderService

### Teste Técnico Nstech · .NET Senior Software Engineer

> API REST para gestão de pedidos, construída com **.NET 8**, **Clean Architecture**, **CQRS**, **PostgreSQL**, **EF Core**, **JWT** e práticas orientadas a produção.

O **OrderService** foi projetado para demonstrar não apenas a implementação dos requisitos funcionais, mas também decisões de arquitetura, qualidade de código, resiliência, testabilidade, observabilidade e preocupação com consistência de domínio.

---

## ✨ Visão Geral

A aplicação gerencia o ciclo de vida de pedidos e seus itens, incluindo:

- criação de pedidos;
- consulta e filtros;
- confirmação;
- cancelamento;
- controle de estoque;
- prevenção de overselling;
- autenticação via JWT;
- validações de domínio;
- tratamento padronizado de erros;
- migrations e seed automáticos;
- health checks;
- testes unitários e de integração;
- execução local ou completamente containerizada.

---

## 🧭 Arquitetura

A solução segue princípios de **Clean Architecture**, mantendo regras de negócio independentes de infraestrutura e frameworks.

```text
OrderService/
│
├── src/
│   ├── OrderService.Domain/
│   │   └── Entidades, Value Objects, regras e exceções de domínio
│   │
│   ├── OrderService.Application/
│   │   └── CQRS, MediatR, validators, use cases e ports
│   │
│   ├── OrderService.Infrastructure/
│   │   └── EF Core, PostgreSQL, JWT, migrations e seed
│   │
│   └── OrderService.Api/
│       └── Controllers, middleware, Swagger e health checks
│
├── tests/
│   ├── OrderService.UnitTests/
│   └── OrderService.IntegrationTests/
│
├── specs/
│   └── Especificações e artefatos de Spec Driven Development
│
├── docs/
│   ├── decisions.md
│   ├── local-run.md
│   ├── testing-guide.md
│   └── postman/
│
├── docker-compose.yml
├── OrderService.sln
└── README.md
```

### Fluxo arquitetural

```text
HTTP Request
     │
     ▼
┌───────────────┐
│      API      │
│ Controllers   │
└───────┬───────┘
        │
        ▼
┌───────────────┐
│  Application  │
│ CQRS/MediatR  │
│ Validators    │
└───────┬───────┘
        │
        ▼
┌───────────────┐
│    Domain     │
│ Business Rules│
│ Entities / VOs│
└───────┬───────┘
        │
        ▼
┌───────────────┐
│Infrastructure │
│ EF / Postgres │
│ JWT / Storage │
└───────────────┘
```

---

# 🛠️ Tech Stack

| Área | Tecnologia |
|---|---|
| Runtime | .NET 8 |
| Linguagem | C# |
| API | ASP.NET Core |
| Persistência | Entity Framework Core |
| Banco de dados | PostgreSQL |
| Arquitetura | Clean Architecture |
| Application Pattern | CQRS |
| Mediator | MediatR |
| Validação | FluentValidation |
| Autenticação | JWT Bearer |
| Containers | Docker / Docker Compose |
| Testes | xUnit |
| API Docs | Swagger / OpenAPI |
| CI | GitHub Actions |

---

# 📋 Pré-requisitos

Para executar o projeto localmente:

- **.NET 8 SDK** ou superior compatível com `net8.0`
- **Docker Desktop**
- **Git**
- IDE de sua preferência:
  - Visual Studio 2022/2026
  - JetBrains Rider
  - Visual Studio Code

---

# ▶️ Quick Start

A maneira mais simples de executar toda a solução é utilizando Docker.

```bash
git clone <repository-url>
cd OrderService

docker compose up --build
```

Após a inicialização:

| Serviço | URL |
|---|---|
| Swagger | http://localhost:8080/swagger |
| Health | http://localhost:8080/health |
| Readiness | http://localhost:8080/health/ready |
| Liveness | http://localhost:8080/health/live |

Durante o startup:

```text
PostgreSQL starts
        ↓
EF Core migrations
        ↓
Seed data
        ↓
Application startup
        ↓
Health checks ready
```

As **migrations e o seed são aplicados automaticamente**.

---

# 🐳 Executando com Docker

Para subir API e PostgreSQL:

```bash
docker compose up --build
```

Para executar em background:

```bash
docker compose up --build -d
```

Para interromper:

```bash
docker compose down
```

Para remover também os volumes:

```bash
docker compose down -v
```

---

# 💻 Executando localmente

A solução também suporta execução híbrida:

```text
API → máquina local
PostgreSQL → Docker
```

Nesse cenário, a aplicação utiliza:

```text
Host=localhost
Port=5432
```

Enquanto o ambiente Docker utiliza internamente:

```text
Host=postgres
```

Os dois modos permanecem isolados e não sobrescrevem configurações entre si.

---

## Opção A — Recomendada

### PostgreSQL no Docker + API no host

Suba apenas o banco:

```bash
docker compose up postgres -d
```

Restaure as dependências:

```bash
dotnet restore OrderService.sln
```

Execute a API:

```bash
dotnet run \
  --project src/OrderService.Api/OrderService.Api.csproj \
  --launch-profile http
```

Endpoints locais:

| Serviço | URL |
|---|---|
| Swagger | http://localhost:5232/swagger |
| Health | http://localhost:5232/health |
| Readiness | http://localhost:5232/health/ready |

---

## Opção B — PostgreSQL instalado localmente

Crie o banco:

```text
Database: orderservice
Username: postgres
Password: postgres
```

Depois execute:

```bash
dotnet run \
  --project src/OrderService.Api/OrderService.Api.csproj \
  --launch-profile http
```

Caso utilize credenciais diferentes, configure através de:

```text
Environment Variables
```

ou:

```text
dotnet user-secrets
```

Não é necessário modificar o `docker-compose.yml`.

📖 Guia completo:

[`docs/local-run.md`](docs/local-run.md)

---

# 🧑‍💻 Visual Studio

Abra:

```text
OrderService.sln
```

ou:

```text
OrderService.slnx
```

O `.sln` clássico oferece maior compatibilidade entre versões do Visual Studio.

Depois:

1. Defina `OrderService.Api` como **Startup Project**.
2. Garanta que o PostgreSQL esteja disponível.
3. Execute utilizando o perfil `http` ou `https`.
4. Acesse `/swagger`.

Para subir somente o PostgreSQL:

```bash
docker compose up postgres -d
```

---

# 🔐 Autenticação

Os endpoints de pedidos são protegidos por **JWT Bearer Authentication**.

### Gerar token

```http
POST /auth/token
Content-Type: application/json

{
  "username": "demo",
  "password": "demo"
}
```

A resposta contém um:

```json
{
  "accessToken": "..."
}
```

Utilize o token nas chamadas protegidas:

```http
Authorization: Bearer {accessToken}
```

---

# 🌐 Endpoints

| Método | Endpoint | Autenticação | Descrição |
|---|---|---|---|
| `POST` | `/auth/token` | Público | Gera token JWT |
| `POST` | `/orders` | JWT | Cria um pedido |
| `POST` | `/orders/{id}/confirm` | JWT | Confirma um pedido |
| `POST` | `/orders/{id}/cancel` | JWT | Cancela um pedido |
| `GET` | `/orders/{id}` | JWT | Consulta um pedido |
| `GET` | `/orders` | JWT | Pesquisa pedidos |

### Filtros disponíveis

```http
GET /orders
    ?customerId=
    &status=
    &from=
    &to=
    &page=
    &pageSize=
```

---

# 📦 Exemplo — Criando um pedido

## Produtos disponíveis no seed

| ProductId | Produto | Preço | Estoque |
|---|---|---:|---:|
| `11111111-1111-1111-1111-111111111111` | Widget A | R$ 10,00 | 100 |
| `22222222-2222-2222-2222-222222222222` | Widget B | R$ 25,50 | 50 |
| `33333333-3333-3333-3333-333333333333` | Widget C | R$ 99,90 | 5 |

### Request

```bash
curl -X POST http://localhost:8080/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "currency": "BRL",
    "items": [
      {
        "productId": "11111111-1111-1111-1111-111111111111",
        "quantity": 2
      }
    ]
  }'
```

---

# 🧠 Regras de Domínio

O domínio foi modelado para manter as principais invariantes dentro das próprias regras de negócio.

## Ciclo de vida

```text
                  ┌───────────┐
                  │  Placed   │
                  └─────┬─────┘
                        │
             ┌──────────┴──────────┐
             │                     │
          Confirm               Cancel
             │                     │
             ▼                     ▼
      ┌─────────────┐        ┌───────────┐
      │  Confirmed  │        │ Canceled  │
      └──────┬──────┘        └───────────┘
             │
           Cancel
             │
             ▼
      ┌───────────┐
      │ Canceled  │
      └───────────┘
```

### Comportamentos

**Novo pedido**

```text
→ Placed
```

**Confirm**

```text
Placed → Confirmed
```

Ao confirmar:

```text
✔ valida estoque
✔ baixa estoque
✔ persiste alteração
```

A operação é **idempotente**.

---

**Cancel**

```text
Placed → Canceled
```

ou:

```text
Confirmed → Canceled
```

Caso o pedido estivesse confirmado:

```text
✔ estoque é devolvido
```

A operação também é **idempotente**.

---

# 💰 Cálculo do pedido

O total é calculado exclusivamente no domínio:

```text
Total = Σ(UnitPrice × Quantity)
```

O cliente não controla diretamente o preço final do pedido.

Os preços são derivados dos produtos conhecidos pelo sistema.

---

# 🛡️ Validações

Entre as regras implementadas:

```text
Pedido sem itens
        ↓
Erro de domínio

Quantity <= 0
        ↓
Erro de validação

Produto inexistente
        ↓
Not Found / Domain Error

Estoque insuficiente
        ↓
Business Rule Violation
```

Os erros são retornados utilizando o padrão:

```text
RFC 7807
Problem Details
```

---

# ⚔️ Controle de Concorrência

Um dos principais riscos do domínio é:

```text
Overselling
```

Exemplo:

```text
Estoque disponível = 1

Request A → compra 1
Request B → compra 1

Executados simultaneamente
```

Sem controle de concorrência, ambos poderiam ser confirmados.

Para mitigar esse cenário, `Product` possui:

```text
Version
```

utilizado como **Concurrency Token pelo EF Core**.

Fluxo simplificado:

```text
Request A
   │
   ├── lê Product Version 5
   │
   └── UPDATE ... WHERE Version = 5
                       │
                       ▼
                  Version = 6

Request B
   │
   ├── ainda possui Version 5
   │
   └── UPDATE ... WHERE Version = 5
                       │
                       ▼
                0 rows affected
                       │
                       ▼
             concurrency conflict
```

Assim, somente uma das operações consegue alterar o registro com a versão esperada.

---

# ♻️ Idempotência

As operações:

```text
POST /orders/{id}/confirm
POST /orders/{id}/cancel
```

foram projetadas para possuir **idempotência de negócio**.

Exemplo:

```text
Confirm
Confirm
Confirm
```

não gera múltiplas baixas de estoque.

Da mesma forma:

```text
Cancel
Cancel
Cancel
```

não produz múltiplas reposições.

A decisão de não utilizar `Polly Retry` automaticamente nesse fluxo é intencional e documentada.

📖 Ver:

[`docs/decisions.md`](docs/decisions.md)

---

# ⏱️ Cancellation & Timeouts

O projeto utiliza:

```text
CancellationToken
```

de ponta a ponta.

Fluxo:

```text
HTTP Request
      │
      ▼
Controller
      │
      ▼
MediatR Handler
      │
      ▼
Repository
      │
      ▼
EF Core / PostgreSQL
```

Dessa forma, requisições canceladas pelo cliente podem interromper trabalho desnecessário no backend.

Também são utilizados **timeouts controlados para operações HTTP e EF Core**.

---

# 🧪 Testes

Execute toda a suíte:

```bash
dotnet test OrderService.sln
```

A solução possui dois níveis principais:

```text
tests/
├── OrderService.UnitTests/
└── OrderService.IntegrationTests/
```

### Unit Tests

Cobrem principalmente:

```text
Domain Rules
Application Handlers
Validators
State transitions
Calculations
```

### Integration Tests

Validam o comportamento integrado da aplicação, incluindo infraestrutura e API quando aplicável.

---

# 🧪 Testes Manuais

Foi disponibilizado um roteiro completo para execução dos principais cenários através do Swagger e Postman.

### Documentação

- [`docs/testing-guide.md`](docs/testing-guide.md)
- [`docs/local-run.md`](docs/local-run.md)

### Postman

Collection:

[`docs/postman/OrderService.postman_collection.json`](docs/postman/OrderService.postman_collection.json)

Environment Docker:

[`docs/postman/OrderService.Docker.postman_environment.json`](docs/postman/OrderService.Docker.postman_environment.json)

Environment Local:

[`docs/postman/OrderService.Local.postman_environment.json`](docs/postman/OrderService.Local.postman_environment.json)

### Execução recomendada

No Postman:

```text
Import
   ↓
Collection + Environment
   ↓
OrderService — Docker
        ou
OrderService — Local
   ↓
01 — Auth
   ↓
02 — Happy Path
```

---

# ❤️ Health Checks

A API disponibiliza três níveis de verificação.

### Health geral

```http
GET /health
```

### Readiness

```http
GET /health/ready
```

Valida se dependências necessárias para receber tráfego estão disponíveis, incluindo o banco.

### Liveness

```http
GET /health/live
```

Indica se o processo da aplicação está ativo.

Essa separação é compatível com cenários de execução em ambientes orquestrados como Kubernetes.

---

# 🔄 CI Pipeline

Pipeline localizado em:

```text
.github/workflows/ci.yml
```

Fluxo:

```text
Checkout
   ↓
Restore
   ↓
Build
   ↓
Test
```

Principais comandos:

```bash
dotnet restore
dotnet build
dotnet test
```

---

# 📐 Spec Driven Development

Além da implementação, o projeto mantém especificações em:

```text
/specs
```

O objetivo é tornar explícito:

```text
Requirement
    ↓
Specification
    ↓
Architecture
    ↓
Implementation
    ↓
Validation
```

Isso reduz ambiguidades e melhora rastreabilidade entre requisito e código.

---

# 🏛️ Decisões de Arquitetura

As principais decisões técnicas estão documentadas em:

[`docs/decisions.md`](docs/decisions.md)

Entre elas:

- Clean Architecture;
- CQRS;
- MediatR;
- FluentValidation;
- JWT;
- EF Core;
- PostgreSQL;
- optimistic concurrency;
- idempotência;
- migrations automáticas;
- tratamento de erros;
- estratégia de testes;
- política de retry;
- timeouts;
- CancellationToken.

---

# 🎯 Princípios adotados

O projeto busca privilegiar:

```text
Clareza > complexidade acidental

Domínio > framework

Explicitness > magic

Testabilidade > acoplamento

Consistência > conveniência

Observabilidade > debugging tardio
```

---

# ✅ Checklist do desafio

### Funcional

- [x] Criar pedidos
- [x] Consultar pedido
- [x] Listar pedidos
- [x] Confirmar pedido
- [x] Cancelar pedido
- [x] Validar estoque
- [x] Atualizar estoque

### Segurança

- [x] JWT
- [x] Endpoints protegidos
- [x] Autorização básica

### Persistência

- [x] PostgreSQL
- [x] Entity Framework Core
- [x] Migrations
- [x] Seed automático

### Engenharia

- [x] Clean Architecture
- [x] CQRS
- [x] MediatR
- [x] FluentValidation
- [x] CancellationToken end-to-end
- [x] Optimistic Concurrency
- [x] Problem Details

### Qualidade

- [x] Unit Tests
- [x] Integration Tests
- [x] `dotnet test`
- [x] Health Checks
- [x] Swagger
- [x] Docker

### DevEx / Entrega

- [x] API executável localmente
- [x] Ambiente completamente containerizado
- [x] CI com GitHub Actions
- [x] Collection Postman
- [x] Guia de testes
- [x] Guia de execução local
- [x] Decisões técnicas documentadas

---

# 🧩 Resumo técnico

```text
.NET 8
   │
   ├── ASP.NET Core
   ├── Clean Architecture
   ├── CQRS + MediatR
   ├── FluentValidation
   ├── JWT
   ├── EF Core
   │      └── Optimistic Concurrency
   │
   ├── PostgreSQL
   ├── Docker Compose
   ├── Health Checks
   ├── Swagger
   ├── Unit Tests
   ├── Integration Tests
   └── GitHub Actions
```

---

## 🏁 Resultado

O objetivo do projeto é apresentar uma API relativamente simples no domínio, mas construída com preocupações típicas de um ambiente real de engenharia:

**consistência, concorrência, segurança, testabilidade, manutenibilidade e operação.**

> Não apenas fazer o endpoint funcionar — mas construir uma solução previsível, evolutiva e segura para produção.