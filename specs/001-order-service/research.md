# Research — Alinhamento Nstech + Escopo do Teste

**Feature:** `001-order-service`  
**Empresa:** Nstech (Brasil)  
**Contexto:** Teste técnico .NET Sênior — 3 dias corridos  
**Data:** 2026-10-05

## Como a Nstech implementa projetos (mapeamento)

Fonte: perfil técnico das vagas Nstech (.NET / microserviços).

| Camada | Stack Nstech (produção) | Aplicação neste teste |
|--------|-------------------------|------------------------|
| Runtime | C# / .NET 10 | **.NET 8+** (MUST do enunciado) |
| API | gRPC + JSON Transcoding + OpenAPI | **ASP.NET Core Web API Controllers** (REST MUST) |
| Arquitetura | Clean Architecture + CQRS (MediatR) | **Clean Architecture + CQRS leve (MediatR)** |
| Persistência | EF Core + PostgreSQL (Npgsql) | **EF Core + Postgres via Docker** |
| Validação | Behaviors MediatR + FluentValidation | **FluentValidation + ValidationBehavior** |
| Resiliência | Polly (retry, CB, timeout) | **Polly scoped** — ver `resilience.md` |
| Segurança | JWT Bearer + Keycloak | **JWT Bearer local (HS256)** — sem Keycloak |
| Observabilidade | OpenTelemetry + Serilog | **Serilog SHOULD**; OTel WONT (tempo) |
| Cache | Redis | **WONT** (sem requisito) |
| Mensageria | Kafka | **WONT** (monólito de API) |
| Testes | xUnit + Moq + FluentAssertions + coverlet | **xUnit + FluentAssertions + NSubstitute/Moq** |
| Containers | Docker / K8s | **docker compose API + Postgres** |

### Princípios de implementação Nstech (aplicados aqui)

1. Domínio rico (invariantes nas entidades, não em controllers).
2. CQRS com pipeline behaviors (validação; resiliência só onde há I/O transitório).
3. Interfaces na Application; implementações na Infrastructure.
4. Auth JWT; autorização básica por endpoint.
5. Testes como gate de qualidade (`dotnet test`).
6. Docker como experiência de “pronto para produção” no essencial.

## Decisões de escopo (anti over-engineering)

| Item | Necessário neste teste? | Motivo |
|------|-------------------------|--------|
| Multi-project `src/` | **SIM** | Clean Architecture real e avaliável |
| MediatR + CQRS | **SIM (SHOULD→MUST interno)** | Alinha Nstech e separa use cases |
| FluentValidation | **SIM** | Validação de input fora do domínio |
| Domain Events | **NÃO** | Sem consumers; complexidade sem ROI |
| Outbox / Kafka | **NÃO** | Fora do enunciado |
| Redis | **NÃO** | Fora do enunciado |
| gRPC | **NÃO** | Enunciado exige REST |
| Keycloak | **NÃO** | JWT mínimo basta |
| Polly retry em Confirm/Cancel | **NÃO** | Idempotência de negócio ≠ retry HTTP |
| Polly em DbContext transient | **SIM (leve)** | Demonstra maturidade Nstech sem overkill |
| CancellationToken end-to-end | **SIM** | Async profissional |
| CommandTimeout / RequestTimeout | **SIM** | Timeout profissional mínimo |
| Soft delete | **NÃO** | Sem requisito |
| Multi-tenancy | **NÃO** | Fora do enunciado |

## Estratégia de estoque (escolhida)

**Modelo:** reserva no `Confirm` (baixa `AvailableQuantity`); liberação no `Cancel` se estava `Confirmed`.

Justificativa:
- `Placed` não consome estoque (pedido criado e ainda não confirmado).
- `Confirm` é o ponto transacional de baixa (com checagem de estoque + concorrência otimista).
- `Cancel` devolve estoque apenas se havia reserva (`Confirmed → Canceled`).
- Idempotência: segunda chamada de confirm/cancel é no-op com mesmo resultado.

Alternativa rejeitada: baixar estoque no `POST /orders` (mistura criação com reserva e dificulta cancelamento de `Placed`).

## Idioma (decisão)

Ver `naming.md`.

**Resumo:** código em **inglês**; documentação/spec/README/ADR em **português**.
Padrão de empresas brasileiras maduras (Nstech inclusa) que usam stack internacional.

## Critério de sucesso da research

- [x] Stack alinhada ao enunciado e ao perfil Nstech
- [x] Over-engineering evitado e documentado
- [x] Modelo de estoque escolhido e justificável na entrevista técnica
