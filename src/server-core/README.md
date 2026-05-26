# Layla — Server Core

ASP.NET Core 10 backend for authentication, user management, project management, and real-time collaboration hubs.

- **Authentication** — JWT Bearer tokens (HS512, 24-hour expiry), email verification via PIN
- **Users** — registration, profile management, admin bans
- **Projects** — CRUD, collaborator invitations, role management, public/private visibility
- **Real-time** — SignalR hubs for voice streaming, user presence, and manuscript collaboration

---

## Quick Start

```bash
cd src/server-core
dotnet restore
dotnet run --project Layla.Api
```

The server starts on `https://localhost:5288` (HTTPS) and `http://localhost:5287` (HTTP).

Swagger UI is available at `https://localhost:5288/swagger` in Development mode.

---

## Architecture

Clean Architecture with three projects:

```
Layla.Api            → Controllers, Hubs, Middleware, Config, Filters, Workers
Layla.Core           → Entities, Interfaces, Services, DTOs, ErrorCode, Constants
Layla.Infrastructure → EF Core repos, AuthService, PresenceTracker, RabbitMQ publisher
```

Solution file: `Layla.Core.slnx`

### Modular Bootstrap (`Layla.Api/Config/`)

`Program.cs` delegates configuration to four focused modules:

| Module | Responsibility |
|---|---|
| `Secrets.cs` | Fail-fast validation of critical secrets (JWT, DB, RabbitMQ) — production only |
| `Builder.cs` | Controllers, Swagger, SignalR, Kestrel port binding, infrastructure DI |
| `Services.cs` | Singleton services (VoiceRoomManager, PresenceTracker) |
| `Secure.cs` | CORS, JWT Bearer auth, rate limiting, token version validation |

---

## Controllers

| Controller | Route | Auth | Purpose |
|---|---|---|---|
| `TokensController` | `api/tokens` | Anonymous | Login — issues JWT Bearer tokens |
| `UsersController` | `api/users` | Mixed | Registration (anon), email verification (anon), profile CRUD (auth), admin ban |
| `ProjectsController` | `api/projects` | Authorized | Project CRUD, collaborator management, public catalog |

### Endpoints

#### Authentication (`/api/tokens`)

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/tokens` | — | Login with email + password. Returns JWT (24h). Rate-limited. |

#### Users (`/api/users`)

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/users` | — | Register a new account |
| `POST` | `/api/users/verify-email` | — | Verify email with PIN code |
| `GET` | `/api/users` | Admin | List all users |
| `GET` | `/api/users/{id}` | Self / Admin | Get user by ID |
| `PUT` | `/api/users/{id}` | Self / Admin | Update profile |
| `DELETE` | `/api/users/{id}` | Self / Admin | Delete account |
| `POST` | `/api/users/{id}/ban` | Admin | Ban user (locks account, invalidates sessions) |

#### Projects (`/api/projects`)

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/projects` | User | Create project (caller becomes OWNER) |
| `GET` | `/api/projects` | User | List caller's projects |
| `GET` | `/api/projects/public` | — | List public catalog |
| `GET` | `/api/projects/all` | Admin | List every project |
| `GET` | `/api/projects/{id}` | Member / Public | Get project by ID |
| `PUT` | `/api/projects/{id}` | OWNER | Update project metadata |
| `DELETE` | `/api/projects/{id}` | OWNER | Delete project |
| `POST` | `/api/projects/{id}/join` | User | Join a public project as READER |
| `POST` | `/api/projects/{id}/collaborators` | OWNER | Invite collaborator by email |
| `GET` | `/api/projects/{id}/collaborators` | Member | List collaborators |
| `PATCH` | `/api/projects/{id}/collaborators/{userId}/role` | OWNER | Change collaborator role |
| `DELETE` | `/api/projects/{id}/collaborators/{userId}` | OWNER | Remove collaborator |

---

## SignalR Hubs

| Hub | Path | Auth | Purpose |
|---|---|---|---|
| `VoiceHub` | `/hubs/voice` | JWT | Push-to-talk audio streaming with role-based speak permissions |
| `PresenceHub` | `/hubs/presence` | JWT | Online/offline presence tracking, author heartbeat |
| `ManuscriptHub` | `/hubs/manuscript` | JWT | Real-time cursor sync, text broadcasting, chapter save notifications |

---

## Middleware & Filters

| Component | Path | Purpose |
|---|---|---|
| `GlobalExceptionMiddleware` | `Middleware/` | Catches unhandled exceptions, logs and returns 500 |
| `TokenVersionValidator` | `Middleware/` | Validates JWT token version against DB (session invalidation) |
| `RequireUserIdFilter` | `Filters/` | Action filter ensuring user ID is present in claims |
| `ApiControllerBase` | `Controllers/` | Base controller with `RespondWithError(ErrorCode?)` helper |

---

## Error Handling

All controllers use the typed `ErrorCode` enum (`Layla.Core/Common/ErrorCode.cs`) instead of magic strings. `RespondWithError(ErrorCode?)` maps errors to HTTP status codes automatically.

| ErrorCode Category | HTTP Status | Examples |
|---|---|---|
| Validation / Input | 400 | `InvalidInput` |
| Authentication | 401 | `InvalidCredentials`, `SessionExpired` |
| Authorization | 403 | `Forbidden` |
| Not found | 404 | `ProjectNotFound`, `UserNotFound` |
| Conflict | 409 | `DuplicateEmail` |
| Locked | 423 | `AccountLocked` |
| Server errors | 500 | `InternalError` |

---

## Configuration

The server reads configuration from `appsettings.Development.json` (local) or environment variables (Docker). Key settings:

| Setting Path | Purpose |
|---|---|
| `Ports:HTTPS` / `Ports:HTTP` | Kestrel binding ports (default 5288 / 5287) |
| `JwtSettings:Secret` | HS512 signing key (min 32 chars) |
| `JwtSettings:Issuer` / `Audience` | Token claims |
| `DatabaseConfigs:SQL:ConnectionString` | SQL Server connection |
| `RabbitMQ:*` | RabbitMQ connection for event publishing |
| `EmailConfigs:*` | SMTP settings for email verification |

---

## Docker

The Dockerfile is at `Layla.Api/Dockerfile`. When running via docker-compose from `src/`, the service is available as `server-core` on the internal Docker network.
