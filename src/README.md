# Layla

Collaborative creative-writing and worldbuilding platform. Multiple authors can co-write novels, manage wiki entries, visualize narrative graphs, and communicate through a voice session — all in real time.

---

## Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                         Clients                                │
│  Desktop (WPF/Net9)  │  Web (Blazor/Net9)  │  Android (Kotlin) │
└────────────┬───────────────────┬───────────────────────────────┘
             │ HTTPS + SignalR   │ HTTPS
             ▼                   ▼
┌────────────────────┐   ┌─────────────────────────────┐
│   server-core      │   │   server-worldbuilding      │
│   ASP.NET Core 10  │   │   Node.js + Express 5       │
│   Port 5288 (HTTPS)│   │   Port 3000 (HTTP)          │
│   Port 5287 (HTTP) │   │                             │
│                    │   │  /api/manuscripts           │
│  /api/tokens       │   │  /api/wiki                  │
│  /api/users        │   │  /api/graph                 │
│  /api/projects     │   │  /api/health                │
│  /hubs/voice       │   │  /api-docs  ← Swagger UI    │
│  /hubs/presence    │   └──────────────┬──────────────┘
│  /swagger  ← UI    │                  │
└────────┬───────────┘          ┌───────▼──────────┐
         │                      │   MongoDB        │
         │  RabbitMQ events     │   (manuscripts,  │
         │  (project.created)   │   wiki entries)  │
         ▼                      └──────────────────┘
┌────────────────┐              ┌───────────────────┐
│   SQL Server   │              │   Neo4j           │
│  (users,       │              │ (narrative graph) │
│   projects,    │              └───────────────────┘
│   roles)       │       ┌─────────────────────────────────┐
└────────────────┘       │     RabbitMQ                    │
                         │  Exchange: worldbuilding.events │
                         │  Routing key: project.created   │
                         └─────────────────────────────────┘
```

### Services

| Service | Tech | Port | Databases | Purpose |
|---|---|---|---|---|
| `server-core` | ASP.NET Core 10 | 5288/5287 | SQL Server | Auth, users, projects, roles, SignalR |
| `server-worldbuilding` | Node.js + Express 5 | 3000 | MongoDB, Neo4j | Manuscripts, wiki, narrative graph |

### Clients

| Client | Tech | Role |
|---|---|---|
| Desktop | WPF .NET 9 | Main writing workspace — editor, wiki, graph, voice |
| Web | Blazor .NET 9 | Public reader + admin panel |
| Android | Kotlin + Compose | Mobile companion — project list, voice PTT, wiki reference |

---

## API Documentation

| Service | URL |
|---|---|
| server-core (Swagger UI) | `https://localhost:5288/swagger` |
| server-worldbuilding (Swagger UI) | `http://localhost:3000/api-docs` |
| server-worldbuilding (OpenAPI JSON) | `http://localhost:3000/api-docs.json` |

---

## API Reference

### server-core  (`https://localhost:5288`)

#### Authentication

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/tokens` | — | Login — returns a JWT valid for 24 h |
| `POST` | `/api/users` | — | Register a new account |

#### Users

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/api/users` | Admin | List all users |
| `GET` | `/api/users/{id}` | User | Get user by ID |
| `PUT` | `/api/users/{id}` | Self / Admin | Update profile |
| `DELETE` | `/api/users/{id}` | Self / Admin | Delete account |
| `POST` | `/api/users/{id}/ban` | Admin | Ban user (locks account, invalidates sessions) |

#### Projects

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/projects` | User | Create project (caller becomes OWNER) |
| `GET` | `/api/projects` | User | List caller's projects |
| `GET` | `/api/projects/public` | — | List all public projects |
| `GET` | `/api/projects/all` | Admin | List every project in the system |
| `GET` | `/api/projects/{id}` | Member / Public | Get project by ID |
| `PUT` | `/api/projects/{id}` | OWNER | Update project metadata |
| `DELETE` | `/api/projects/{id}` | OWNER | Delete project |
| `POST` | `/api/projects/{id}/join` | User | Join a public project as READER |
| `POST` | `/api/projects/{id}/collaborators` | OWNER | Invite collaborator by email |
| `GET` | `/api/projects/{id}/collaborators` | Member | List collaborators |
| `DELETE` | `/api/projects/{id}/collaborators/{userId}` | OWNER | Remove collaborator |

#### Real-time Hubs (SignalR)

| Hub | Path | Description |
|---|---|---|
| Voice | `/hubs/voice` | Push-to-talk audio streaming |
| Presence | `/hubs/presence` | Online/offline presence tracking |

---

### server-worldbuilding  (`http://localhost:3000`)

#### Health

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/health` | Service health check — returns `OK` |

#### Manuscripts

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/manuscripts/{projectId}` | List all manuscripts (chapter index, no content) |
| `POST` | `/api/manuscripts/{projectId}` | Create a manuscript |
| `GET` | `/api/manuscripts/{projectId}/{manuscriptId}` | Get manuscript with chapter index |
| `PUT` | `/api/manuscripts/{projectId}/{manuscriptId}` | Rename or reorder manuscript |
| `DELETE` | `/api/manuscripts/{projectId}/{manuscriptId}` | Delete manuscript and all chapters |

#### Chapters

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/manuscripts/{projectId}/{manuscriptId}/chapters` | Create chapter |
| `GET` | `/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}` | Get chapter with full RTF content |
| `PUT` | `/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}` | Update chapter (Last-Write-Wins) |
| `DELETE` | `/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}` | Delete chapter |

#### Wiki

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/wiki/{projectId}` | List wiki entries |
| `POST` | `/api/wiki/{projectId}` | Create wiki entry |
| `GET` | `/api/wiki/{projectId}/{entryId}` | Get wiki entry |
| `PUT` | `/api/wiki/{projectId}/{entryId}` | Update wiki entry |
| `DELETE` | `/api/wiki/{projectId}/{entryId}` | Delete wiki entry |

#### Graph

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/graph/{projectId}` | Get narrative graph (nodes + edges) |
| `POST` | `/api/graph/{projectId}/nodes` | Create graph node |
| `POST` | `/api/graph/{projectId}/edges` | Create relationship between nodes |
| `DELETE` | `/api/graph/{projectId}/nodes/{nodeId}` | Delete node |
| `DELETE` | `/api/graph/{projectId}/edges/{edgeId}` | Delete edge |

---

## Project Roles

| Role | Permissions |
|---|---|
| `OWNER` | Full control — update, delete, manage collaborators |
| `EDITOR` | Read + write manuscripts and wiki |
| `READER` | Read-only access |

Roles are managed through the `ProjectRoles` constant class (`Layla.Core/Constants/ProjectRoles.cs`), which provides `IsValid(role)` and `Normalize(role)` methods for case-insensitive validation.

---

## Error Handling

All services and controllers use the typed `ErrorCode` enum (`Layla.Core/Common/ErrorCode.cs`) instead of magic strings. Controllers map errors to HTTP status codes automatically via `RespondWithError(ErrorCode?)`.

| ErrorCode Category | HTTP Status | Examples |
|---|---|---|
| Validation / Input | 400 | `InvalidInput` |
| Authentication | 401 | `InvalidCredentials`, `SessionExpired` |
| Authorization | 403 | `Forbidden` |
| Not found | 404 | `ProjectNotFound`, `UserNotFound` |
| Conflict | 409 | `DuplicateEmail` |
| Server errors | 500 | `InternalError` |

---

## Setup

### Prerequisites

- Docker Desktop (recommended) — or install services manually
- .NET 10 SDK
- Node.js 22 + pnpm 10
- Android Studio (for the mobile client)

### Docker (recommended)

```bash
cp .env.Development .env   # fill in secrets
docker compose up -d
```

Services start at:
- server-core: `https://localhost:5288`
- server-worldbuilding: `http://localhost:3000`
- RabbitMQ management: `http://localhost:15672`
- Neo4j browser: `http://localhost:7474`

### Manual

#### server-core

```bash
cd src/server-core
dotnet restore
dotnet run --project Layla.Api
```

#### server-worldbuilding

```bash
cd src/server-worldbuilding
pnpm install
pnpm run dev
```

#### Web client

```bash
cd src/client-web
dotnet run
```

#### Desktop client

Open `src/client-desktop/Layla.Desktop/Layla.Desktop.sln` in Visual Studio and run.

---

## Environment Variables

Copy `.env.Development` to `.env` and fill in all values before starting.

### SQL Server

| Variable | Description |
|---|---|
| `SQL_USERNAME` | SQL Server admin username |
| `SQL_PASSWORD` | SQL Server SA password |
| `SQL_DATABASE` | Database name |
| `SQL_PORT` | SQL Server port |
| `MSSQL_MEM_LIMIT` | SQL Server memory limit |

### MongoDB

| Variable | Description |
|---|---|
| `MONGO_INITDB_ROOT_USERNAME` | MongoDB root user |
| `MONGO_INITDB_ROOT_PASSWORD` | MongoDB root password |
| `MONGO_PORT` | MongoDB port |

### Neo4j

| Variable | Description |
|---|---|
| `NEO4J_USERNAME` | Neo4j username |
| `NEO4J_PASSWORD` | Neo4j password |
| `NEO4J_BROWSER_PORT` | Neo4j browser HTTP port |
| `NEO4J_BOLT_PORT` | Neo4j Bolt protocol port |

### Worldbuilding

| Variable | Description |
|---|---|
| `WORLDBUILDING_PORT` | HTTP port |
| `WORLDBUILDING_ALLOWED_ORIGINS` | Comma-separated CORS origins |

### Core API

| Variable | Description |
|---|---|
| `CORE_PORT_1` | HTTPS port |
| `CORE_PORT_2` | HTTP port |
| `CORE_ENVIRONMENT` | ASP.NET environment (`Development`, `Production`) |

### RabbitMQ

| Variable | Description |
|---|---|
| `RABBIT_HostName` | RabbitMQ hostname |
| `RABBIT_USER` | RabbitMQ username |
| `RABBIT_PASSWORD` | RabbitMQ password |
| `RABBIT_PORT` | AMQP port |
| `RABBIT_MANAGEMENT_PORT` | Management UI port |

### Security (JWT)

| Variable | Description |
|---|---|
| `JWT_SECRET` | Signing key (min 32 chars, validated at startup) |
| `JWT_SECRET_REFRESH` | Refresh token signing key (min 32 chars) |
| `JWT_ISSUER` | Token issuer claim |
| `JWT_AUDIENCE` | Token audience claim |
| `JWT_ACCESS_TOKEN_EXPIRY` | Access token lifetime |
| `JWT_REFRESH_TOKEN_EXPIRY` | Refresh token lifetime |

---

## Messaging

server-core publishes to the `worldbuilding.events` RabbitMQ Topic exchange.

| Routing key | Payload | Trigger |
|---|---|---|
| `project.created` | `{ projectId, ownerUserId, title, createdAt }` | New project created |

server-worldbuilding consumes this event to bootstrap Neo4j graph nodes and MongoDB manuscript documents.

> **Note**: Events are published **after** the database commit (outbox pattern) to prevent inconsistency between SQL Server state and downstream consumers.

---

## Internal Architecture

### server-core — Modular Bootstrap (`Layla.Api/Config/`)

`Program.cs` delegates configuration to four focused modules:

| Module | Responsibility |
|---|---|
| `Secrets.cs` | Fail-fast validation of critical secrets (JWT, DB, RabbitMQ) |
| `Builder.cs` | Controllers, Swagger, SignalR, infrastructure DI |
| `Services.cs` | Singleton services (VoiceRoomManager, PresenceTracker) |
| `Secure.cs` | CORS, JWT Bearer auth, rate limiting, token version validation |

### server-core — Middleware & Filters

| Component | Path | Purpose |
|---|---|---|
| `GlobalExceptionMiddleware` | `Middleware/` | Catches unhandled exceptions, logs and returns 500 |
| `TokenVersionValidator` | `Middleware/` | Validates JWT token version against DB (session invalidation) |
| `RequireUserIdFilter` | `Filters/` | Action filter ensuring user ID is present in claims |
| `ApiControllerBase` | `Controllers/` | Base controller with `RespondWithError(ErrorCode?)` helper |

### server-core — Clean Architecture Layers

```
Layla.Api          → Controllers, Hubs, Middleware, Config
Layla.Core         → Entities, Interfaces, Services, DTOs, ErrorCode, Constants
Layla.Infrastructure → EF Core repos, AuthService, PresenceTracker, RabbitMQ
```

### server-worldbuilding — TypeScript Path Aliases

The project uses `@/` path aliases mapped to `src/` via `tsconfig.json`. All imports use `@/config/env`, `@/db/mongoose`, etc.

The service implements graceful shutdown — `SIGTERM` and `SIGINT` handlers close HTTP, RabbitMQ, and Neo4j connections in order.

---

## Solution File

`Layla.Core.slnx` contains only the server-core projects:

```xml
<Solution>
  <Project Path="Layla.Api/Layla.Api.csproj" />
  <Project Path="Layla.Core/Layla.Core.csproj" />
  <Project Path="Layla.Infrastructure/Layla.Infrastructure.csproj" />
</Solution>
```

Client projects (`client-desktop`, `client-web`) have their own independent `.sln` files.

---

## Use Cases

| ID | Name | Actor | Backend | Status |
|---|---|---|---|---|
| CU-01 | Browse public catalog | Anyone | server-core | ✅ |
| CU-02 | Preview project synopsis | Anyone | server-core | ✅ |
| CU-03 | Login / Register | User | server-core | ✅ |
| CU-04 | Manage profile | User | server-core | ✅ |
| CU-05 | Create project | Writer | server-core | ✅ |
| CU-06 | Manage collaborators | Writer (OWNER) | server-core | ✅ |
| CU-07 | Configure privacy | Writer (OWNER) | server-core | ✅ |
| CU-08 | Edit manuscript | Editor / Writer | worldbuilding | ✅ |
| CU-09 | Manage wiki (nodes) | Editor / Writer | worldbuilding | ✅ |
| CU-10 | Visualize narrative graph | Reader / Editor | worldbuilding | ✅ |
| CU-11 | Voice session (speak) | Writer | server-core | ✅ |
| CU-12 | Join as listener | Reader | server-core | ✅ |
| CU-13 | Read full story | Reader | worldbuilding | ❌ |
| CU-14 | System reports | Admin | server-core | ❌ |
| CU-15 | Manage users (ban/roles) | Admin | server-core | ✅ |

✅ Implemented · 🔧 Partial · ❌ Not started
