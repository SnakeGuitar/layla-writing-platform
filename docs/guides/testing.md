# Testing guide

## Overview

| Suite                       | Tool                |        Tests | Status         |
| --------------------------- | ------------------- | -----------: | -------------- |
| Unit — server-core          | xUnit + NSubstitute |          187 | green          |
| Unit — server-worldbuilding | Vitest              |          128 | green          |
| Load — REST + SignalR       | k6                  |    5 scripts | —              |
| Performance — JWT           | BenchmarkDotNet     | 5 benchmarks | —              |
| **Total unit**              |                     |      **315** | **0 failures** |

---

## How to run

```bash
# server-core unit tests
cd src/server-core
dotnet test Layla.Core.Tests/Layla.Core.Tests.csproj

# server-worldbuilding unit tests
cd src/server-worldbuilding
pnpm test              # run once
pnpm test:watch        # watch mode
pnpm test:coverage     # with V8 coverage report

# Load tests (requires a running stack)
cd tests/load
k6 run auth.js
k6 run projects.js
k6 run manuscripts.js
k6 run signalr.js
k6 run scenarios.js                     # mixed workload
k6 run -e TARGET_VUS=100 scenarios.js  # override VU count

# Performance benchmarks (Release build required)
cd src/server-core
dotnet run --project Layla.Core.Benchmarks -c Release
```

---

## Unit tests — server-core (C#)

### Design conventions

- One `[Fact]` = one assertion (atomic tests).
- Each test class arranges and acts in its constructor (synchronous via `.GetAwaiter().GetResult()`).
- Shared setup lives in `file static class *SutFactory` — invisible outside the file.
- Interfaces are mocked with **NSubstitute 5.3**; `UserManager<T>` and `SignInManager<T>` are mocked via full constructor injection.

### Test files

#### `TokenServiceTests.cs` — 17 tests

`Layla.Core/Services/TokenService`

| Class                         | What it verifies                                 |
| ----------------------------- | ------------------------------------------------ |
| `_SubClaim`                   | `sub` claim equals user ID                       |
| `_EmailClaim`                 | `email` claim present                            |
| `_TokenVersionClaim`          | `token_version` claim present                    |
| `_WithMultipleRoles`          | all roles included                               |
| `_WithNoRoles`                | zero-role token is valid                         |
| `_Expiry`                     | expiry matches `JwtSettings.ExpirationInMinutes` |
| `_Algorithm`                  | header alg is `HS512`                            |
| `_JtiUniqueness`              | two tokens have different `jti`                  |
| `_SignatureValidity`          | token validates with the same secret             |
| `_Constructor_WithNullSecret` | throws `ArgumentException` on empty secret       |

---

#### `AuthServiceTests.cs` — 30 tests

`Layla.Infrastructure/Services/AuthService`

| Class                                       | What it verifies                                       |
| ------------------------------------------- | ------------------------------------------------------ |
| `LoginAsync_WhenUserDoesNotExist`           | `InvalidCredentials`                                   |
| `LoginAsync_WhenAccountIsLockedOut`         | `AccountLocked`                                        |
| `LoginAsync_WhenPasswordIsWrong`            | `InvalidCredentials`                                   |
| `LoginAsync_WhenCredentialsAreValid`        | token, email, displayName, expiry                      |
| `LoginAsync_TokenVersionBehavior`           | `UpdateAsync` called with version + 1                  |
| `LoginAsync_WhenTokenVersionUpdateFails`    | `InternalError`                                        |
| `RegisterAsync_WhenEmailAlreadyRegistered`  | `DuplicateEmail`                                       |
| `RegisterAsync_WhenIdentityRejectsPassword` | `ValidationFailed`                                     |
| `RegisterAsync_WhenSuccessful`              | success, empty token (pending email)                   |
| `RegisterAsync_EmailVerification`           | email sent once, to correct address, with Identity PIN |
| `RegisterAsync_WhenDisplayNameOmitted`      | defaults to email local part                           |

---

#### `ProjectServiceTests.cs` — 36 tests

`Layla.Core/Services/ProjectService`

| Scenario group                                    | What it verifies                                           |
| ------------------------------------------------- | ---------------------------------------------------------- |
| `CreateProjectAsync_WhenSucceeds`                 | title/visibility persisted, event published                |
| `TransactionBehavior`                             | operation runs inside `ExecuteInTransactionAsync`          |
| `WhenDatabaseFails`                               | `DatabaseError` propagated                                 |
| `UpdateProjectAsync_WhenCallerIsNotOwner`         | `Unauthorized`                                             |
| `UpdateProjectAsync_WhenProjectDoesNotExist`      | `ProjectNotFound`                                          |
| `UpdateProjectAsync_WhenOwnerUpdates`             | success                                                    |
| `DeleteProjectAsync_WhenCallerIsNotOwner`         | `Unauthorized`                                             |
| `DeleteProjectAsync_WhenOwnerDeletes`             | success                                                    |
| `GetUserProjectsAsync_WhenUserHasProjects`        | list returned                                              |
| `InviteCollaboratorAsync_*`                       | duplicate invite, invalid role, owner self-invite, success |
| `JoinPublicProjectAsync_*`                        | private project, already member, success                   |
| `RemoveCollaboratorAsync_WhenTryingToRemoveOwner` | `Unauthorized`                                             |

---

#### `PresenceTrackerTests.cs` — 25 tests

`Layla.Infrastructure/Services/PresenceTracker`

| Scenario group          | What it verifies                                                                                                                                                                                                 |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `MarkActive`            | first owner activates project; second join returns false; READER does not activate; single entry for two connections; role upgrade (EDITOR→OWNER); no downgrade (OWNER stays OWNER); avatarUrl preserved/updated |
| `MarkInactive`          | unknown connectionId; last participant leaves (project deactivates); other participants remain; two connections → one removed but user still present                                                             |
| `IsProjectActive`       | unknown project; only READER; EDITOR present; presence-hub "Author" role                                                                                                                                         |
| `GetActiveParticipants` | unknown project; DTO fields (userId, displayName, role, avatarUrl); count                                                                                                                                        |
| `GetUserConnection`     | unknown user; connected; after disconnect                                                                                                                                                                        |

---

#### `VoiceRoomManagerTests.cs` — 27 tests

`Layla.Infrastructure/Services/VoiceRoomManager`

| Scenario group         | What it verifies                                                                              |
| ---------------------- | --------------------------------------------------------------------------------------------- |
| `AddParticipant`       | DTO fields (userId, displayName, isSpeaking=false, role); room created; rejoin replaces state |
| `RemoveParticipant`    | project not found; user not in room; removal returns true + room emptied                      |
| `RemoveByConnectionId` | found (projectId, userId, participant removed); not found (both null)                         |
| `SetSpeaking`          | project/user not found; sets true; sets false                                                 |
| `GetParticipants`      | project not found → []; correct count                                                         |
| `GetParticipant`       | project/user not found → null; correct userId                                                 |
| `TryConsumeAudioSlot`  | project/user not found; first call succeeds; immediate retry throttled (20 ms)                |

---

#### `OutboxProcessorWorkerTests.cs` — 13 tests

`Layla.Infrastructure/Workers/OutboxProcessorWorker`

`ProcessOutboxMessagesAsync` invoked via reflection (private method).

| Scenario              | What it verifies                                                                                                                                       |
| --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| No messages           | `Publish` never called; `SaveChanges` never called                                                                                                     |
| ClientEvicted message | `Publish` called once; routing key `"client.evicted"`; correct projectId and userId forwarded; message marked `Processed = true`; `SaveChanges` called |
| Unknown event type    | `Publish` not called; message still marked processed                                                                                                   |
| Publisher throws      | message stays unprocessed; remaining batch members still processed                                                                                     |
| Multiple messages     | `SaveChanges` called exactly once                                                                                                                      |

---

#### `TokenVersionValidatorTests.cs` — 8 tests

`Layla.Api/Middleware/TokenVersionValidator`

Uses real `TokenValidatedContext` (ASP.NET Core) with `DefaultHttpContext`.

| Scenario                       | What it verifies              |
| ------------------------------ | ----------------------------- |
| Principal is null              | `ctx.Result.Failure` set      |
| `sub` claim missing            | failure set                   |
| `token_version` claim missing  | failure set                   |
| User not found in DB           | failure set                   |
| TokenVersion mismatch          | failure set                   |
| Valid token, version matches   | `ctx.Result?.Failure` is null |
| NameIdentifier absent          | claim added after validation  |
| NameIdentifier already present | not duplicated                |

---

#### `AppUserServiceTests.cs` — 14 tests

`Layla.Core/Services/AppUserService`

| Scenario                         | What it verifies                     |
| -------------------------------- | ------------------------------------ |
| `GetAllAppUsersAsync` success    | `IsSuccess`, one DTO per user        |
| `GetAllAppUsersAsync` repo fails | `IsNotSuccess`, error code forwarded |
| `GetAppUserByIdAsync` found      | success, email + displayName mapped  |
| `GetAppUserByIdAsync` not found  | `UserNotFound`                       |
| `UpdateAppUserAsync` success     | success, updated displayName         |
| `UpdateAppUserAsync` not found   | `UserNotFound`                       |
| `DeleteAppUserAsync` success     | success, data = true                 |
| `BanAppUserAsync` success        | success, data = true                 |

---

#### `RequireUserIdFilterTests.cs` — 4 tests

`Layla.Api/Filters/RequireUserIdFilter`

| Scenario                                  | What it verifies                                     |
| ----------------------------------------- | ---------------------------------------------------- |
| Endpoint has `[AllowAnonymous]`           | result not set (filter bypassed)                     |
| No userId claim on authenticated endpoint | `UnauthorizedObjectResult`                           |
| Valid `sub` claim                         | result not set; userId stored in `HttpContext.Items` |

---

#### `GlobalExceptionMiddlewareTests.cs` — 6 tests

`Layla.Api/Middleware/GlobalExceptionMiddleware`

| Scenario                  | What it verifies                                                |
| ------------------------- | --------------------------------------------------------------- |
| `next` completes normally | status code remains 200                                         |
| `next` throws             | status 500; Content-Type `application/json`                     |
| Response body             | `StatusCode = 500`; non-empty `Error` string; `TraceId` present |

---

## Unit tests — server-worldbuilding (TypeScript)

### Design conventions

- One `it` = one assertion (atomic).
- `beforeAll` + shared `let` variables: arrange and act once per `describe`, assert in separate `it` blocks.
- `vi.mock("@/services/container", ...)` at the top of every file that imports services — prevents `config/env.ts` JWT secret validation on module load.
- `vi.useFakeTimers()` / `vi.runAllTimersAsync()` for retry-with-backoff scenarios.
- Service functions that accept `repo = container` are tested by passing a custom repo directly (no DI container needed).

### Test files

#### `Auth.test.ts` — 27 tests

`middlewares/Auth.ts`

| Scenario group           | What it verifies                                                                                                                                       |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `MiddlewareAuthenticate` | no header → 401; Basic scheme → 401; expired → 401 + message; invalid signature → 401; unknown error → 401; valid token → next called + `req.user` set |
| `MiddlewareOptionalAuth` | no header → next (no block); valid token → `req.user` set; invalid token → next (never blocks)                                                         |
| `MiddlewareRequireRole`  | no user → 401; wrong role → 403; matching role → next; matching one of multiple → next                                                                 |

---

#### `ProjectGuard.test.ts` — 14 tests

`middlewares/ProjectGuard.ts`

| Scenario group         | What it verifies                                                                                                                                             |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `requireWriteAccess`   | READER → 403; undefined role → 403; EDITOR → next; OWNER → next                                                                                              |
| `requireProjectAccess` | no projectId → next; no `req.user` → 401; Neo4j confirms OWNER → next + role set; Neo4j confirms EDITOR → next + role set; no Neo4j record + no bearer → 403 |

---

#### `Graph.service.test.ts` — 18 tests

`services/Graph.service.ts`

| Scenario group         | What it verifies                                                                                                    |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------- |
| `getGraph`             | repo called; projectId forwarded; entityType filter forwarded; result returned unchanged; correct node count and id |
| `createRelationship`   | returns true/false from repo; all fields forwarded                                                                  |
| `deleteRelationship`   | repo called; resolves without throw; repo throws → propagated                                                       |
| `getEntityAppearances` | empty → []; correct record count and fields; projectId + entityId forwarded                                         |

---

#### `Mention.service.test.ts` — 28 tests

`services/Mention.service.ts`

| Scenario group        | What it verifies                                                                                                                                                                                                                     |
| --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `stripRtf`            | empty string; plain text passthrough; control words removed; hex escape `\'e9` → `é`; unicode escape `\u233` → `é`; consecutive spaces collapsed                                                                                     |
| `extractMentions`     | empty entries → []; no match; exact match (entityId, name, entityType); case-insensitive; word-boundary (no partial match); same entity deduplicated; empty name skipped; name > 200 chars skipped; multiple entities → all found    |
| `syncChapterMentions` | empty entries → []; content matches → mention found + `syncAppearances` called; no content match → `syncAppearances` not called; entity deleted between extract and sync → filtered out; `syncAppearances` throws → error propagated |

---

#### `WikiEntry.service.test.ts` — 17 tests

`services/WikiEntry.service.ts`

| Scenario group | What it verifies                                                                                                                                                                                                      |
| -------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `createEntry`  | Neo4j sync succeeds → entry returned + `mergeEntity` called + `neo4jSynced = true`; Neo4j sync fails → entry still returned (tolerant), `neo4jSynced = false`                                                         |
| `updateEntry`  | entry not found → null; entry found + Neo4j succeeds → returned + `mergeEntity` called; Neo4j fails → entry still returned                                                                                            |
| `deleteEntry`  | MongoDB not found → false + no Neo4j call; first attempt succeeds → true + called once; all 3 retries fail → true (MongoDB deleted, orphaned node acceptable) + called 3×; succeeds on 2nd attempt → true + called 2× |

---

#### `Manuscript.service.test.ts` — 16 tests

`services/Manuscript.service.ts`

| Scenario group        | What it verifies                                                                                                                                                                                                               |
| --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `createManuscript`    | auto-order = existing count; explicit order used; returned index has correct title; `content` not exposed in index                                                                                                             |
| `updateChapter` (LWW) | chapter not found → `{ conflict: false }`; clientTimestamp older than `updatedAt` → `{ conflict: true, chapter }`; clientTimestamp newer → `{ conflict: false }` + repo called; no timestamp → no conflict check + repo called |
| `createChapter`       | auto-order = chapter count; explicit order used; new chapterId assigned (non-empty); content and title set from args                                                                                                           |

---

#### `ManageJWT.test.ts` — 8 tests

`utils/ManageJWT.ts`

Uses real `jwt.sign` / `jwt.verify` (HS512) — no JWT mocking.

| Scenario                          | What it verifies                                         |
| --------------------------------- | -------------------------------------------------------- |
| Token with `sub` claim            | `id` normalized from `sub`; `email` and `role` preserved |
| Token with explicit `id` claim    | `id` takes priority over `sub`                           |
| Token with neither `id` nor `sub` | `id` defaults to `""`                                    |
| Expired token                     | throws `TokenExpiredError`                               |
| Wrong secret                      | throws `JsonWebTokenError`                               |
| Wrong algorithm (HS256)           | throws `JsonWebTokenError`                               |

---

## Load tests (k6)

Located in `tests/load/`. Require a running stack (`docker compose up -d` or Vagrant).

| Script           |          VUs | Thresholds     | What it exercises                                                           |
| ---------------- | -----------: | -------------- | --------------------------------------------------------------------------- |
| `auth.js`        |           50 | p(95) < 500 ms | POST `/api/tokens` (login) + POST `/api/users` (register)                   |
| `projects.js`    |           30 | p(95) < 800 ms | Full CRUD: create → list → get → update → delete                            |
| `manuscripts.js` |           20 | p(95) < 1 s    | Concurrent chapter read + write                                             |
| `signalr.js`     |           25 | p(95) < 2 s    | Negotiate → WebSocket → `JoinChapterGroupAsync` → `SendTextChangedAsync`    |
| `scenarios.js`   | configurable | —              | Mixed workload: 40 % catalog, 30 % projects, 20 % manuscripts, 10 % SignalR |

```bash
# Override VU count
k6 run -e TARGET_VUS=200 tests/load/scenarios.js
```

---

## Performance benchmarks (BenchmarkDotNet)

Located in `src/server-core/Layla.Core.Benchmarks/`. **Must run in Release mode.**

| Benchmark                      | Baseline | What it measures                        |
| ------------------------------ | :------: | --------------------------------------- |
| `GenerateToken_SingleRole`     |    ✓     | JWT generation — one role               |
| `GenerateToken_MultipleRoles`  |          | JWT generation — five roles             |
| `ValidateToken`                |          | `TokenHandler.ValidateToken` round-trip |
| `ParseTokenWithoutValidation`  |          | `JwtSecurityTokenHandler.ReadJwtToken`  |
| `GenerateAndValidateRoundtrip` |          | End-to-end generate + validate          |

Decorators: `[MemoryDiagnoser]` (allocations), `[SimpleJob(warmupCount: 3, iterationCount: 10)]`.

---

## Coverage map

### Testable modules with unit tests

| Module                    | Service              |   Tests |
| ------------------------- | -------------------- | ------: |
| TokenService              | server-core          |      17 |
| AuthService               | server-core          |      30 |
| ProjectService            | server-core          |      36 |
| AppUserService            | server-core          |      14 |
| PresenceTracker           | server-core          |      25 |
| VoiceRoomManager          | server-core          |      27 |
| OutboxProcessorWorker     | server-core          |      13 |
| TokenVersionValidator     | server-core          |       8 |
| GlobalExceptionMiddleware | server-core          |       6 |
| RequireUserIdFilter       | server-core          |       4 |
| Auth middleware           | server-worldbuilding |      27 |
| ProjectGuard middleware   | server-worldbuilding |      14 |
| Graph.service             | server-worldbuilding |      18 |
| Mention.service           | server-worldbuilding |      28 |
| WikiEntry.service         | server-worldbuilding |      17 |
| Manuscript.service        | server-worldbuilding |      16 |
| ManageJWT                 | server-worldbuilding |       8 |
| **Total**                 |                      | **315** |

### Not covered — low value (trivial code)

| Module                      | Why                                            |
| --------------------------- | ---------------------------------------------- |
| `IdentityErrorFormatter`    | Single `string.Join`                           |
| `ClaimsPrincipalExtensions` | Two `FindFirstValue` calls                     |
| `Result<T>`                 | Generic factory wrapper, no branching          |
| `asyncHandler.ts`           | Single `.catch(next)` line                     |
| `validation/index.ts`       | Zod schemas — better covered by contract tests |
| `RateLimiter.ts`            | Delegates entirely to `express-rate-limit`     |

### Not covered — integration territory

These require real infrastructure (SQL Server, MongoDB, Neo4j, RabbitMQ, SignalR) and are better served by integration tests or end-to-end tests.

**server-core:** `AppUserRepository`, `ProjectRepository`, `OutboxRepository`, `Publisher`, `Consumer`, `EventBusAdapter`, `EmailService`, `ManuscriptHub`, `PresenceHub`, `VoiceHub`, controllers × 3.

**server-worldbuilding:** `MongooseManuscriptRepository`, `MongooseWikiEntryRepository`, `Neo4jGraphRepository`, consumers × 2, controllers × 3, `neo4jSyncWorker`.
