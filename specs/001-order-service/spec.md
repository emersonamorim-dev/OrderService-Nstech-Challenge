# Spec — Order Service (MUST)

**Feature ID:** `001-order-service`  
**Status:** Approved for implementation  
**Owner:** Candidato .NET Sênior  
**Prazo:** até 3 dias corridos

## 1. Visão

API REST de gestão de pedidos com itens, validação de estoque, autenticação JWT,
persistência PostgreSQL (EF Core), testes automatizados e execução via Docker Compose —
entrega “pronta para produção” no essencial, alinhada a Clean Architecture / SOLID.

## 2. Personas / Atores

| Ator | Descrição |
|------|-----------|
| Cliente autenticado | Obtém JWT e opera pedidos |
| Sistema | Aplica invariantes de domínio, estoque e idempotência |
| Operador local | Sobe stack com `docker compose up` e `dotnet test` |

## 3. Requisitos funcionais

### FR-01 — Autenticar e obter token
- **MUST** `POST /auth/token`
- Credenciais de demo configuráveis (appsettings / env)
- Retorna JWT Bearer válido para endpoints de pedidos

### FR-02 — Criar pedido
- **MUST** `POST /orders`
- Payload: `customerId`, `currency`, `items[{ productId, quantity }]`
- Regras:
  - Pedido sem itens → rejeitar
  - `quantity` deve ser `> 0`
  - Produto deve existir
  - Não pode exceder estoque disponível **no momento da confirmação** (ver design de estoque); na criação, **SHOULD** pré-validar disponibilidade para fail-fast
  - `Total = Σ (UnitPrice × Quantity)`
- Estado inicial: **`Placed`**
- Autorização: autenticado

### FR-03 — Confirmar pedido (idempotente)
- **MUST** `POST /orders/{id}/confirm`
- Só confirma pedido em `Placed`
- Deve **reservar/baixar estoque** atomicamente
- 2ª chamada: mesmo resultado (idempotente) — se já `Confirmed`, retorna sucesso sem alterar estoque novamente
- Transição: `Placed → Confirmed`
- Concorrência: proteção contra oversell (row version / transação)

### FR-04 — Cancelar pedido (idempotente)
- **MUST** `POST /orders/{id}/cancel`
- Permite cancelar `Placed` e `Confirmed`
- Se `Confirmed`, **libera estoque** reservado
- Se já `Canceled`, no-op idempotente
- Transição: `Placed|Confirmed → Canceled`
- Não permite cancelar estados inexistentes / inválidos com erro de domínio claro

### FR-05 — Consultar pedido
- **MUST** `GET /orders/{id}`
- Retorna pedido + itens em DTO adequado
- 404 se não existir

### FR-06 — Listar pedidos (paginação + filtros)
- **MUST** `GET /orders?customerId=&status=&from=&to=&page=&pageSize=`
- Paginação obrigatória (`page`, `pageSize` com defaults e limites)
- Filtros: `customerId`, `status`, intervalo de `CreatedAt` (`from`/`to`)
- Query eficiente (sem N+1; projeção DTO)

### FR-07 — Catálogo mínimo de produtos
- **MUST** seed de produtos com estoque para demo/testes
- Produto: `Id`, `Name` (opcional), `UnitPrice`, `AvailableQuantity`

## 4. Requisitos não funcionais

### NFR-01 — Clean Architecture / SOLID
- Camadas: Domain / Application / Infrastructure / Api
- Dependências apontam para dentro
- Invariantes no domínio; use cases na Application; I/O na Infrastructure

### NFR-02 — Async end-to-end
- Todos os caminhos I/O usam `async/await` + `CancellationToken`

### NFR-03 — Testes
- xUnit; `dotnet test` verde
- Cobertura das regras de negócio, bordas e idempotência
- Unitários de domínio + application; integração de API/EF (mínimo viável)

### NFR-04 — Segurança
- JWT Bearer
- Endpoints de pedidos exigem autenticação
- `/auth/token` público

### NFR-05 — Persistência
- EF Core + migrations
- Postgres via Docker
- Migrations aplicadas automaticamente no startup

### NFR-06 — Operacional
- `docker compose up` sobe API + banco
- README com rodar / testar / usar
- Decisões em `docs/decisions.md`

### NFR-07 — Resiliência (escopo profissional mínimo)
- Exception handling global com Problem Details
- Timeouts de request/DB + CancellationToken
- Retry apenas para falhas transitórias de infraestrutura (Polly)
- Detalhes em `resilience.md`

### NFR-08 — Performance (mínimo)
- Índices em filtros de listagem
- `AsNoTracking` em leituras
- Transações curtas em confirm/cancel
- Paginação com limite máximo de `pageSize`

## 5. Critérios de aceite (checklist do enunciado)

- [ ] API roda local e via Docker
- [ ] Migrations aplicadas automaticamente
- [ ] Endpoints MUST implementados
- [ ] JWT + autorização básica funcionando
- [ ] `dotnet test` passando
- [ ] README com passo a passo
- [ ] Decisões técnicas documentadas

## 6. Fora de escopo (WONT)

- Keycloak / OAuth completo
- Redis, Kafka, gRPC
- Multi-tenant
- UI
- Soft delete / auditoria completa
- Circuit breaker distribuído / service mesh
- OpenTelemetry completo (opcional se sobrar tempo)

## 7. Definição de Pronto (DoD)

Uma task só fecha quando:
1. Spec da regra atendida
2. Teste automatizado cobre o comportamento
3. Código nas camadas corretas
4. Sem warnings relevantes novos
5. Documentação atualizada se houver decisão nova
