# Execução local (sem container da API)

A API já está preparada para rodar **fora do Docker** conectando no Postgres via `localhost:5432`.  
O modo Docker (`docker compose up`) permanece intacto e usa `Host=postgres`.

## Como a configuração se separa

| Modo | Ambiente | Connection string | URL da API |
|------|----------|-------------------|------------|
| Local (dotnet / VS) | `Development` | `Host=localhost;Port=5432;...` | `http://localhost:5232` (perfil `http`) |
| Docker Compose | `Docker` | `Host=postgres;Port=5432;...` | `http://localhost:8080` |

Arquivos relevantes:

- `appsettings.json` / `appsettings.Development.json` → localhost (local)
- `appsettings.Docker.json` + env do compose → host `postgres` (Docker)
- `Program.cs` → `MigrateAsync` + seed em **ambos** os modos

Nenhuma alteração de compose/Dockerfile é necessária para o fluxo local.

---

## Modo A — Recomendado: API local + Postgres no Docker

Use quando quiser debugar no Visual Studio sem subir a API em container.

```bash
# 1) Sobe só o banco (API do compose NÃO precisa estar no ar)
docker compose up postgres -d

# 2) Restaura e sobe a API local
dotnet restore OrderService.sln
dotnet run --project src/OrderService.Api/OrderService.Api.csproj --launch-profile http
```

- Swagger: http://localhost:5232/swagger  
- Health: http://localhost:5232/health  
- Postman: environment **OrderService — Local**

### Visual Studio

1. Abra `OrderService.sln`
2. Startup project: `OrderService.Api`
3. Perfil: `http` (ou `https` se o cert local estiver confiável)
4. Garanta Postgres acessível em `localhost:5432` (Modo A ou B)
5. F5

> Se o container `orderservice-api` também estiver rodando, não há conflito de porta (8080 vs 5232). Eles compartilham o mesmo banco — evite misturar testes se quiser isolamento.

---

## Modo B — 100% local (sem Docker)

Requisitos:

- PostgreSQL 16+ instalado no Windows
- Usuário/senha alinhados à connection string (padrão do projeto: `postgres` / `postgres`)
- Database `orderservice` (pode ser criado vazio; migrations rodam no startup)

### Criar o banco (psql ou pgAdmin)

```sql
CREATE DATABASE orderservice;
```

Se o usuário/senha/porta forem diferentes, sobrescreva **sem editar Docker**:

**PowerShell (sessão atual):**

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=orderservice;Username=SEU_USER;Password=SUA_SENHA"
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/OrderService.Api/OrderService.Api.csproj --launch-profile http
```

**User Secrets (persistente, só no seu machine):**

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=orderservice;Username=SEU_USER;Password=SUA_SENHA" --project src/OrderService.Api
```

Depois:

```bash
dotnet run --project src/OrderService.Api/OrderService.Api.csproj --launch-profile http
```

No boot a API aplica migrations + seed automaticamente.

---

## HTTPS local (opcional)

Perfil `https` usa `https://localhost:7178`. Uma vez:

```bash
dotnet dev-certs https --trust
```

Para o teste técnico, o perfil `http` é suficiente.

---

## Smoke test local

```powershell
# Health / readiness
Invoke-RestMethod http://localhost:5232/health
Invoke-RestMethod http://localhost:5232/health/ready
Invoke-RestMethod http://localhost:5232/health/live

# Token
Invoke-RestMethod http://localhost:5232/auth/token -Method POST -ContentType 'application/json' -Body '{"username":"demo","password":"demo"}'
```

Ou importe no Postman:

- Collection: `docs/postman/OrderService.postman_collection.json`
- Environment: `docs/postman/OrderService.Local.postman_environment.json`

---

## Troubleshooting

| Sintoma | Causa provável | Correção |
|---------|----------------|----------|
| `Database initialization failed` / connection refused | Postgres parado ou porta errada | Modo A: `docker compose up postgres -d` · Modo B: iniciar serviço PostgreSQL |
| Auth falha / DB authentication failed | User/senha diferentes do padrão | Override via env var ou user-secrets |
| Porta 5432 ocupada por outro Postgres | Conflito de instâncias | Pare um dos serviços ou mude a porta na connection string local |
| Swagger não abre | API não subiu / perfil errado | Confirme `http://localhost:5232/swagger` e logs do `dotnet run` |
| Cert HTTPS inválido | Dev cert não confiado | `dotnet dev-certs https --trust` ou use perfil `http` |

---

## O que NÃO mudar para “fazer local funcionar”

- Não altere `Host=postgres` no Docker
- Não remova `ASPNETCORE_ENVIRONMENT=Docker` do compose
- Não mude a porta publicada `8080:8080` do compose só por causa do local
- Use overrides (`env` / user-secrets) para credenciais locais personalizadas
