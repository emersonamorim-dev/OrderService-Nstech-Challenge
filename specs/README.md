# Spec Driven Development (SDD) — OrderService

Este diretório é a **fonte da verdade** do teste técnico Nstech.
Nenhuma feature entra em código sem spec aprovada e tasks rastreáveis.

## Por que `specs/` na raiz da solução?

| Opção | Avaliação | Decisão |
|-------|-----------|---------|
| `specs/` (raiz da solution) | Padrão SDD/OpenSpec; visível no PR; independente de camadas | **Escolhido** |
| `docs/specs/` | Mistura ADR com requisitos vivos | Evitado |
| Dentro de `src/` ou `OrderService/` | Spec vira artefato de código e some no refactor | Evitado |
| `.cursor/` apenas | Ferramenta de IDE, não entrega de engenharia | Complementar |

Estrutura alvo da solution (após implementação):

```text
OrderService/                          # raiz da solution
├── specs/                             # SDD (este diretório)
│   └── 001-order-service/
├── docs/
│   ├── decisions.md                   # exigido pelo enunciado
│   └── adr/                           # ADRs numerados
├── src/
│   ├── OrderService.Domain/
│   ├── OrderService.Application/
│   ├── OrderService.Infrastructure/
│   └── OrderService.Api/
├── tests/
│   ├── OrderService.UnitTests/
│   └── OrderService.IntegrationTests/
├── docker-compose.yml
├── OrderService.slnx
└── README.md
```

> Pastas atuais `OrderService/{Domain,Application,Infrastructure,API}` são esqueleto
> do template. A implementação migra para **projetos separados** em `src/` (Clean Architecture real).

## Fluxo SDD (obrigatório)

```text
1. Research  → decisões e trade-offs
2. Spec      → requisitos MUST / SHOULD / WONT
3. Design    → arquitetura, patterns, SOLID
4. Data Model→ entidades, invariantes, VO
5. API       → contratos REST + erros
6. Resilience→ exceptions, timeouts, retries (scoped)
7. Tasks     → backlog TDD-friendly (vermelho → verde)
8. Implement → código só contra tasks
9. Verify    → dotnet test + docker compose + checklist
```

## Índice da feature `001-order-service`

| Artefato | Conteúdo |
|----------|----------|
| [spec.md](./001-order-service/spec.md) | Requisitos funcionais e não funcionais |
| [design.md](./001-order-service/design.md) | Arquitetura, SOLID, patterns, alinhamento Nstech |
| [data-model.md](./001-order-service/data-model.md) | Domínio, invariantes, persistência |
| [api-contracts.md](./001-order-service/api-contracts.md) | Endpoints, DTOs, status codes |
| [resilience.md](./001-order-service/resilience.md) | Exceptions, retries, timeouts — o que entra e o que não |
| [naming.md](./001-order-service/naming.md) | Idioma, nomenclatura de classes/métodos |
| [tasks.md](./001-order-service/tasks.md) | Plano de execução TDD (3 dias) |
| [research.md](./001-order-service/research.md) | Mapeamento empresa + decisões de escopo |

## Convenção de status

- `MUST` — bloqueia entrega
- `SHOULD` — diferencial profissional alinhado à Nstech, sem over-engineering
- `WONT` — fora do escopo deste teste (documentado)
