# Resilience — Exceptions, Retries & Timeouts

**Feature:** `001-order-service`  
**Objetivo:** tratar falhas de forma profissional **sem over-engineering** para um teste de 3 dias.

## 1. Veredito executivo

| Mecanismo | Necessário neste teste? | Nível | Motivo |
|-----------|-------------------------|-------|--------|
| Exception handling global (Problem Details) | **SIM — MUST** | Completo | Qualidade de API; avaliável |
| Domain / Application exceptions tipadas | **SIM — MUST** | Completo | Regras de negócio claras |
| `CancellationToken` end-to-end | **SIM — MUST** | Completo | Async profissional |
| Request timeout / Kestrel limits | **SIM — SHOULD** | Leve | Evita requests eternas |
| EF `CommandTimeout` | **SIM — SHOULD** | Leve | Protege pool do Postgres |
| Polly retry (transient DB) | **NÃO — WONT** | — | Idempotência de negócio cobre o MUST; retry não é exigido |
| Polly timeout policy | **NÃO — WONT** | — | Cobertura via CT + CommandTimeout |
| Circuit Breaker | **NÃO — WONT** | — | Sem dependências HTTP externas |
| Retry em Confirm/Cancel de negócio | **NÃO — WONT** | — | Idempotência ≠ retry cego |
| Bulkhead / rate limit avançado | **NÃO — WONT** | — | Fora do escopo |
| Outbox + retry de mensageria | **NÃO — WONT** | — | Sem broker |

## 2. Exception strategy (MUST)

### Hierarquia

```text
Exception
└── DomainException                     // regras / invariantes
    ├── OrderNotFoundException
    ├── ProductNotFoundException
    ├── InvalidOrderStateException
    ├── InsufficientStockException
    ├── EmptyOrderItemsException
    └── ConcurrencyConflictException
ApplicationValidation → FluentValidation.ValidationException
UnauthorizedAccessException / SecurityTokenException → 401
```

### Middleware `ExceptionHandlingMiddleware`

Responsabilidades:
1. Capturar exceções não tratadas no pipeline
2. Mapear para Problem Details + status HTTP
3. Logar com Serilog (Warning para domínio; Error para 500)
4. Nunca vazar stack trace em Production

### Mapping table

| Exception | Status | Title |
|-----------|--------|-------|
| `ValidationException` | 400 | Validation failed |
| `OrderNotFoundException` / `ProductNotFoundException` | 404 | Resource not found |
| `InsufficientStockException` | 409 | Insufficient stock |
| `InvalidOrderStateException` | 409 | Invalid order state |
| `ConcurrencyConflictException` / `DbUpdateConcurrencyException` | 409 | Concurrency conflict |
| `EmptyOrderItemsException` | 400 | Invalid order |
| Outras | 500 | Internal server error |

### Regras de ouro
- Controllers **não** fazem try/catch de regra de negócio
- Handlers deixam o domínio lançar; middleware traduz
- Não usar `catch (Exception)` engolindo erro em repositórios

## 3. Timeouts (SHOULD → implementar)

### 3.1 CancellationToken (MUST)
- Todos os métodos async públicos: último parâmetro `CancellationToken cancellationToken = default`
- Propagar até EF (`ToListAsync(ct)`, `SaveChangesAsync(ct)`, `MigrateAsync(ct)`)

### 3.2 HTTP request timeout
```csharp
builder.Services.AddRequestTimeouts(o =>
{
    o.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
});
```
Ou configurar Kestrel/`HttpClient` timeouts se aplicável.

### 3.3 EF Core command timeout
```csharp
options.UseNpgsql(cs, npgsql => npgsql.CommandTimeout(30));
```

### 3.4 Valores sugeridos (configuráveis)

| Setting | Default | Config key |
|---------|---------|------------|
| HTTP request timeout | 30s | `Resilience:RequestTimeoutSeconds` |
| EF command timeout | 30s | `Resilience:DbCommandTimeoutSeconds` |
| JWT lifetime | 60m | `Jwt:ExpirationMinutes` |

## 4. Retries com Polly (SHOULD — escopo mínimo)

### Quando retry FAZ sentido
- Falhas **transitórias** de Postgres (timeout de rede, deadlock transitório, startup do container)
- Apenas em operações de infraestrutura encapsuladas (ex.: `SaveChanges` via policy, ou health-related)

### Quando retry NÃO deve existir
- Validação / domínio (400/409)
- Confirmar pedido duas vezes via retry automático após sucesso parcial sem transação
- Qualquer operação sem garantia idempotente de infraestrutura

### Política mínima recomendada

```csharp
// Pseudocódigo — Infrastructure/Resilience
Policy
  .Handle<NpgsqlException>(ex => ex.IsTransient)
  .Or<TimeoutException>()
  .WaitAndRetryAsync(
      retryCount: 2,
      sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * attempt),
      onRetry: (ex, delay, attempt, ctx) => Log.Warning(ex, "Transient DB retry {Attempt}", attempt));
```

Aplicar em:
- `IUnitOfWork.SaveChangesAsync` **ou**
- Execução de migration no startup (com cuidado)

**Não** wrapping MediatR inteiro com retry — handlers de negócio não são todos idempotentes no nível de infra.

### Alternativa aceitável se tempo curto
Documentar em `docs/decisions.md`:
> Retry Polly deferido; mitigação via CancellationToken + CommandTimeout + idempotência de Confirm/Cancel.

Isso ainda é profissional se justificado. O diferencial Nstech é ter o Polly mínimo.

## 5. Transações & concorrência (parte da resiliência de negócio)

Confirm/Cancel **MUST**:
```text
BeginTransaction
  Load Order (tracked)
  Idempotent short-circuit?
  Load Products FOR UPDATE / tracked + RowVersion
  Domain mutations
  SaveChanges
Commit
```

Em `DbUpdateConcurrencyException`:
- Mapear para `409 Concurrency conflict`
- Cliente pode reconsultar e tentar de novo (retry do **cliente**, não do servidor cego)

## 6. Idempotência vs Retry — distinção crítica

```text
Idempotência (MUST do enunciado)
  = mesma intenção de negócio repetida → mesmo estado final
  = implementada no domínio/handler (if already Confirmed return)

Retry (Polly)
  = repetir chamada técnica após falha transitória
  = só seguro se a unidade for transacional / idempotente na infra
```

Na entrevista: explicar essa distinção demonstra senioridade.

## 7. Checklist de implementação de resiliência

- [ ] `ExceptionHandlingMiddleware` + Problem Details
- [ ] Exceções de domínio tipadas
- [ ] `CancellationToken` em toda a cadeia
- [ ] EF CommandTimeout configurado
- [ ] Request timeout (ou equivalente)
- [ ] Transação + concurrency token em Confirm
- [ ] Polly retry mínimo **ou** ADR justificando ausência
- [ ] Sem circuit breaker / sem retry de regra de negócio
