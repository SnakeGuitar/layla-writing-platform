# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What Layla is

Collaborative creative-writing / worldbuilding platform. Two backend services, three clients, three databases, one message bus, one API gateway. The user base persona is **fiction authors who co-write novels**.

## Repository layout

```
src/
├── server-core/                     ASP.NET Core 10 — auth, users, projects, SignalR
├── server-worldbuilding/            Node.js 22 + Express 5 + TypeScript — manuscripts, wiki, graph
├── infraestructure-api_gateway/     YARP reverse proxy (single entry on :5000)
├── client-desktop/                  WPF .NET 9 — main writing workspace
├── client-web/                      Blazor Server .NET 9 — public reader + admin
├── client-android/                  Kotlin + Compose — mobile companion
└── docker-compose.yml               All 8 services orchestrated together
deploy/                              Vagrant + Puppet for 3-VM deployment (see deploy/README.md)
```

Solution file is `src/server-core/Layla.Core.slnx` (only contains the server-core projects). `client-desktop` and `client-web` have their own independent `.sln` files. `src/README.md` is the long-form architecture doc; treat it as authoritative. `WEB_CLIENT_GUIDE.md` is the authoritative reference for the Blazor client's DI/auth/HTTP wiring.

## Common commands

### Build / run individual services
```bash
# server-core
cd src/server-core && dotnet run --project Layla.Api

# server-worldbuilding
cd src/server-worldbuilding && pnpm install && pnpm run dev

# client-web
cd src/client-web && dotnet run

# api-gateway
cd src/infraestructure-api_gateway && dotnet run
```

### Run everything via Docker
```bash
cd src
cp .env.Development .env    # then fill in real values
docker compose up -d
docker compose logs -f server-core
```

### Run everything via Vagrant (3 VMs)
```bash
cd deploy
vagrant up                  # creates 3 VMs (data, apps, edge), provisions with Puppet
vagrant ssh apps -c "sudo docker ps"
vagrant destroy -f          # tear down
```

## Cross-service architecture (the big picture)

The two backends are split by **what kind of data they own**, not by feature:

- **server-core** owns SQL Server: `AppUser` (extends `IdentityUser`), `Project`, `ProjectRole` (join with `OWNER`/`EDITOR`/`READER`). Issues JWTs (24 h, validated with `TokenVersion` for session invalidation). Hosts SignalR (`/hubs/voice`, `/hubs/presence`).
- **server-worldbuilding** owns MongoDB (manuscripts, wiki entries, RTF chapter content) and Neo4j (narrative graph). Consumes `project.created` events from `server-core` to bootstrap a new project's MongoDB documents and Neo4j nodes.
- Messaging is **outbox-after-commit**: `server-core` publishes to the `worldbuilding.events` RabbitMQ topic exchange *after* the SQL transaction commits, so MongoDB/Neo4j never see ghost projects.
- Clients **never call the two backends directly in production** — they go through `api-gateway` (YARP at `:5000`), which routes by path: `/api/tokens|projects|users|hubs/*` → server-core, `/api/wiki|graph|manuscripts` → server-worldbuilding.

## server-core conventions (matters when adding endpoints)

- `Program.cs` delegates to four modules: `Secrets.cs` (fail-fast env validation), `Builder.cs` (DI + Kestrel), `Services.cs` (singletons), `Secure.cs` (CORS/JWT/rate-limit/`TokenVersionValidator`).
- All services return `Result<T>` (`IsSuccess`, `Data`, `Error`). Controllers extend `ApiControllerBase` and map errors with `RespondWithError(ErrorCode?)` — **never use magic strings**, always the `ErrorCode` enum (`Layla.Core/Common/ErrorCode.cs`). HTTP status mapping is centralized in `ApiControllerBase`.
- Project role checks use the `ProjectRoles` constant class (`Layla.Core/Constants/ProjectRoles.cs`) — `IsValid`/`Normalize` (case-insensitive).
- `Secrets.RequireConfig` is called at startup for every required key. **Adding a new required env var requires updating `Secrets.cs`** or the service will fail-fast in production.

## client-web (Blazor Server) conventions

- Blazor **Server**, not WASM — every user gets a SignalR circuit. Anything holding user identity is `Scoped` (`ApiClient`, `ISessionManager`, `IAuthService`, `LaylaAuthenticationStateProvider`, `IProjectService`). Anything holding room/process state is `Singleton` (`IVoiceService`, `ISignalRClient`).
- JWT lives in `ProtectedSessionStorage` (`layla.session` key). **`ProtectedSessionStorage` is unavailable during prerender** — use `OnAfterRenderAsync(firstRender)` and call `Session.InitializeAsync()` to hydrate.
- `Routes.razor` uses `AuthorizeRouteView` so `[Authorize]` on `@page` components works. Unauthenticated routes redirect via `RedirectToLogin.razor` with `?returnUrl=...`.
- `ApiClient` is a typed `HttpClient` with a Polly retry policy bound at registration. Errors throw `APIException`; services catch and return `AuthResult.Fail(...)` or degrade — exceptions never bubble to the UI.
- Wire JSON is **camelCase** at the wire (matches server-core defaults), PascalCase in C#.

## Deployment gotchas (from real debugging)

When deploying behind the YARP gateway (or in any containerized production setup), keep these in mind:

- **`server-core/Layla.Api/Config/Builder.cs`** and **`client-web/Config/Builder.cs`** call `builder.WebHost.UseUrls(...)` with a `localhost` host that overrides `ASPNETCORE_URLS`. They are patched to bind to `+` when `IsProduction()` — keep this when refactoring or the services won't be reachable from other containers/VMs.
- **`server-core/Layla.Api/Program.cs`** does **not** call `UseHttpsRedirection()` in production by design — the gateway is the TLS terminator. If you re-enable it, YARP will receive 307s and fail to proxy.
- **`server-worldbuilding`** reads `process.env["PORT"]` (singular), not `PORT_HTTP`/`PORT_HTTPS`. The docker-compose env block must set `PORT`.
- **`Secrets.cs` requires 6 `EmailConfigs:*` keys** even though SMTP isn't used in most demos — provide dummy values or remove the requirement.
- **MongoDB 7+ needs AVX**, which VirtualBox doesn't expose by default — `deploy/files/compose/compose.data.yml` pins `mongo:4.4` to avoid SIGILL crashes on CPU-restricted VMs.

## Reference URLs

| Service | Dev URL |
|---|---|
| server-core (Swagger) | `https://localhost:7166/swagger` |
| server-worldbuilding (Swagger) | `https://localhost:3000/api-docs` |
| api-gateway | `http://localhost:5000` |
| Web client | `https://localhost:5288` |
| RabbitMQ management | `http://localhost:15672` |
| Neo4j browser | `http://localhost:7474` |

## Things NOT in this repo

- `Layla.Api/appsettings.json` is gitignored. Only `appsettings.Development.json` is committed. Real secrets go via env vars (compose `.env`) or User Secrets in dev.
- Docs in `docs/` are `.docx`/`.pdf` only — they cannot be read directly. Export to text if you need their content.
