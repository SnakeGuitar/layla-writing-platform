# Web Client (Blazor) — Refactor Summary

**Date:** 2026-05-04
**Scope:** `src/client-web/`
**Goal:** Bring the Blazor web client up to feature-parity with the WPF desktop client, which is the most complete reference implementation in the codebase.

---

## 1. The problem

Before this pass, the web client was the most fragile of the three clients:

| Symptom | Root cause |
|---|---|
| Login submitted credentials and **always** navigated to `/home`, even on failure | `Login.razor` discarded the `LoginResponse` and never inspected status |
| **No token persistence** — every component that needed to call the API had to receive a `string token` parameter, but no producer ever filled it | `IProjectService.GetUserProjectsAsync(string token)` etc.; `Home.razor` called `PresenceService.ConnectAsync(_token)` with `_token = ""` |
| `/projects/my-projects` showed **6 hardcoded fake projects** | `MyProjects.razor` had a static demo list inside `OnInitialized` |
| `Register.razor` was an empty page (`<p>Register</p>`) | Never implemented |
| Server returned 401 on every authenticated call once login was bypassed | No `Authorization: Bearer` header anywhere in the pipeline |
| No `[Authorize]` gating | `Routes.razor` used `RouteView` instead of `AuthorizeRouteView` |
| Login form crashed when typed input was an empty `Email`/`Password` | `_email`/`_password` had no client-side validation, only a vague "Please fill in all fields." |
| API errors were swallowed | Empty `catch { /* TODO */ }` blocks |

The desktop client (`src/client-desktop/`) already had all the right primitives — `SessionManager`, `IAuthService` returning `AuthResult`, `ValidationService`, `IProjectApiService` reading the token from session. The fix is to mirror those patterns in Blazor's idioms (scoped services, `AuthenticationStateProvider`, `AuthorizeRouteView`).

---

## 2. What changed (file by file)

### New: cross-cutting validation primitives (mirrors desktop)

```
src/client-web/Helpers/Validation/
    ValidationResult.cs    ← per-field error accumulator
    ValidationService.cs   ← IsValidEmail / IsStrongPassword / IsRequired
```

Same shape as `Layla.Desktop.Models.Validation.ValidationResult` and `Layla.Desktop.Services.Validation.ValidationService`. The two clients now reject identical inputs before any HTTP round-trip.

`LoginRequest` and `RegisterRequest` (in `Application/Schemas/Auth/`) gained a `Validate()` method that returns this `ValidationResult`. Because `System.ComponentModel.DataAnnotations.ValidationResult` is in scope thanks to `[Required]`, the schemas use `using ValidationResult = client_web.Helpers.Validation.ValidationResult;` to disambiguate.

### New: `AuthResult` wrapper

```
src/client-web/Models/Authentication/AuthResult.cs
```

Mirrors `Layla.Desktop.Models.Authentication.AuthResult`. Carries either the decoded `LoginResponse`, or a fallback message + per-field validation errors. Callers now branch on `result.IsSuccess` instead of catching `APIException` everywhere.

### Updated: `IAuthService` / `AuthService`

```diff
- Task<LoginResponse> LoginAsync(LoginRequest request);
+ Task<AuthResult> LoginAsync(LoginRequest request);
+ Task<AuthResult> RegisterAsync(RegisterRequest request);
```

`AuthService` now:

- Runs `request.Validate()` and short-circuits with `AuthResult.ValidationError(...)` on failure.
- Maps `401 → "Invalid email or password."`, `409 → "Email is already registered."`, `400 → ProblemDetails-aware validation errors`.
- Logs network/API failures via `ILogger<AuthService>` instead of swallowing them.
- Adds `RegisterAsync(RegisterRequest)` that posts to `/api/users` and returns the same `AuthResult` shape.

### New: per-circuit `SessionManager`

```
src/client-web/Application/Services/Session/
    ISessionManager.cs
    SessionManager.cs
```

Mirrors `Layla.Desktop.Services.SessionManager`, with two adjustments for Blazor Server:

- Implementation is registered **`Scoped`** (one instance per SignalR circuit) instead of `static`. Two browser tabs no longer share state on the server, and concurrent users cannot collide.
- `InitializeAsync()` and `SaveAsync()` are async because browser-storage hydration requires JS interop. Persistence uses `ProtectedSessionStorage` (encrypted, scoped to the browser tab) so a page reload keeps the user signed in for the lifetime of the tab.
- A `SessionChanged` event lets the auth-state provider re-broadcast the identity on every login / logout / refresh.

### New: `LaylaAuthenticationStateProvider`

```
src/client-web/Application/Services/Auth/LaylaAuthenticationStateProvider.cs
```

Bridges the session into Blazor's authorisation pipeline:

- On every `GetAuthenticationStateAsync()` it reads `Session.CurrentToken`, parses it with `JwtSecurityTokenHandler.ReadJwtToken` (no validation — server-core has already signed it) and projects the JWT claims into a `ClaimsPrincipal`.
- When the session changes, it calls `NotifyAuthenticationStateChanged(...)`, so every `<AuthorizeView>` / `[Authorize]` consumer re-renders without manual plumbing.

### Updated: DI wiring (`Config/Services.cs`)

```csharp
services.AddScoped<ProtectedSessionStorage>();
services.AddScoped<ISessionManager, SessionManager>();
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<AuthenticationStateProvider, LaylaAuthenticationStateProvider>();
services.AddAuthorizationCore();
services.AddCascadingAuthenticationState();
```

`client-web.csproj` gained:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authorization" Version="9.0.3" />
<PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="9.0.3" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.5.0" />
```

### Updated: `IProjectService` / `ProjectService`

The token is no longer a parameter on every call:

```diff
- Task<IEnumerable<ProjectResponse>> GetUserProjectsAsync(string token);
- Task<IEnumerable<ProjectResponse>> GetAllProjectsAsync(string token);
+ Task<IEnumerable<Project>>  GetMyProjectsAsync();
+ Task<IEnumerable<Project>>  GetAllProjectsAsync();
+ Task<List<PublicProjectDto>> GetPublicProjectsAsync();
+ Task<Project?> GetProjectByIdAsync(Guid id);
+ Task<Project?> CreateProjectAsync(CreateProjectRequest request);
+ Task<Project?> UpdateProjectAsync(Guid id, UpdateProjectRequest request);
+ Task<bool>     DeleteProjectAsync(Guid id);
```

The implementation pulls the JWT from `ISessionManager` per call. The full surface (`Get`, `Create`, `Update`, `Delete`, plus `Public` and `All`) mirrors `Layla.Desktop.Services.ProjectApiService`. Errors are funnelled through `ILogger<ProjectService>` and degraded to empty collections / `null` instead of leaking to the UI.

### Updated: shared models (`Models/`)

```
Models/Project.cs                   ← removed `required` modifiers + `Roles` nav, added IsPublic / IsAuthorActive / UserRole
Models/CreateProjectRequest.cs      ← new (mirrors desktop)
Models/UpdateProjectRequest.cs      ← new (mirrors desktop)
```

The `required` modifier on a deserialisation target is a deserialiser footgun — `System.Text.Json` ignored it but EF/Newtonsoft would have thrown. The shape now matches `Layla.Core.Contracts.Project.ProjectResponseDto` exactly.

### Updated: `appsettings.Development.json`

Added a separate URL for the Node.js worldbuilding service so future calls (manuscripts / wiki / graph) don't have to reuse the server-core base address by accident:

```json
"ApiUrls": {
    "BackendURL":      "https://localhost:5288",
    "WorldbuildingURL": "http://localhost:3000",
    ...
}
```

### Updated: routing (`UI/Routes.razor`)

```diff
- <RouteView RouteData="routeData" DefaultLayout="..." />
+ <AuthorizeRouteView RouteData="routeData" DefaultLayout="...">
+     <NotAuthorized>
+         <RedirectToLogin />
+     </NotAuthorized>
+ </AuthorizeRouteView>
+ <NotFound>
+     <LayoutView Layout="...">
+         <p class="text-stone-300">Page not found.</p>
+     </LayoutView>
+ </NotFound>
```

`<CascadingAuthenticationState>` is registered globally via `AddCascadingAuthenticationState()` (Blazor 9 idiom), so wrapping the router is no longer necessary.

The new `UI/Components/RedirectToLogin.razor` preserves the original destination as a `?returnUrl=` query parameter so the user lands back where they started after signing in.

### Updated: `UI/_Imports.razor`

Adds `Microsoft.AspNetCore.Authorization` + `Microsoft.AspNetCore.Components.Authorization` so every component can use `[Authorize]` and `<AuthorizeView>` without a per-file `@using`.

### Updated: pages

- **`Auth/Login.razor`** — shows per-field errors from `AuthResult.ValidationErrors`, surfaces backend messages from `AuthResult.ErrorMessage`, calls `Session.SaveAsync(...)` on success, then `Navigation.NavigateTo("/home")`. Pressing **Enter** in the password field submits.
- **`Auth/Register.razor`** — implemented from scratch, mirrors the Login layout, validates client-side via `RegisterRequest.Validate()`, calls `AuthService.RegisterAsync(...)`, persists the session and redirects to `/home`.
- **`Projects/MyProjects.razor`** — `[Authorize]`-gated, removes the 6 fake projects, calls `ProjectService.GetMyProjectsAsync()`, hydrates the session in `OnAfterRenderAsync(firstRender)` so a page reload still works, and navigates on click.
- **`Home.razor`** — reads the JWT from `Session.CurrentToken` instead of `_token = ""`, hydrates the session in `OnAfterRenderAsync(firstRender)`, and only connects to the presence hub once.

---

## 3. Architectural notes

### Why scoped, not static

The desktop client's `SessionManager` is a `static` class — fine for a process that hosts exactly one user at a time. In Blazor Server, the process hosts every user simultaneously over SignalR circuits. A static would leak Alice's token into Bob's circuit. **Every per-user piece of state in this refactor is `Scoped`** — `ApiClient`, `ISessionManager`, `IAuthService`, `IProjectService`, the `AuthenticationStateProvider`. The voice / presence stack stays `Singleton` because the SignalR client itself is process-wide.

### Why `ProtectedSessionStorage`, not localStorage

`ProtectedSessionStorage` encrypts payloads with the data-protection keys held by the server, so the JWT never sits in plaintext in the browser's storage panels. It's also tab-scoped — closing the tab discards the session, matching the security expectation for a serious authoring app.

The trade-off: it requires JS interop, which is unavailable during prerender. The standard pattern is to call `InitializeAsync()` from `OnAfterRenderAsync(firstRender)` and re-render once it succeeds. Both `Home.razor` and `MyProjects.razor` follow that pattern.

### Why `JwtSecurityTokenHandler.ReadJwtToken` (not `Validate`)

The web client doesn't re-validate signatures: server-core already signed the token, every protected API call sends it back, and the *server* will reject it if it's tampered with. Decoding locally is purely so Blazor can apply `[Authorize]` and `<AuthorizeView Roles="...">` based on the claim set. Treating the token as opaque on the client is a choice, not a security gap.

### What happens when the server rotates the token

Currently, when the JWT expires, the next call returns 401 and we surface a degraded state (empty list, null project). A polished follow-up is to subscribe to `APIException(401)` in a global `DelegatingHandler`, call `Session.ClearAsync()` and re-render — `<AuthorizeRouteView>` will then send the user to `/auth/login` automatically. The plumbing is in place; the handler is the missing piece.

---

## 4. Compilation status

All three components compile cleanly after the refactor:

| Component | Command | Result |
|---|---|---|
| `server-core` | `dotnet build Layla.Core.slnx` | **0 errors**, 2 warnings (NU1510 on a stale package reference, unrelated) |
| `client-web` | `dotnet build` | **0 errors, 0 warnings** |
| `server-worldbuilding` | `npx tsc --noEmit` | **0 errors** |

---

## 5. What this unblocks

| Use case | Previously | Now |
|---|---|---|
| CU-03 (Login / Register) | Login navigated to `/home` even on bad credentials; register page was empty | Both flows work end-to-end, with field-level validation, server-error mapping, and session persistence |
| CU-01 (Public catalog feed) | Worked but presence hub connected anonymously | Connects with the real JWT when the user is signed in |
| CU-04 / CU-05 (My projects, Create project) | `/projects/my-projects` showed 6 fakes | Renders the real list from server-core; `[Authorize]` redirects anonymous users to login |
| Anything `[Authorize]`-gated | No infrastructure | `<AuthorizeRouteView>`, `LaylaAuthenticationStateProvider`, `RedirectToLogin` are all in place |

---

## 6. Pending work (out of scope for this pass)

- **Auto-logout on 401**: a `DelegatingHandler` that observes 401s and calls `Session.ClearAsync()`. The session and auth-state plumbing is ready; only the handler is missing.
- **Refresh-token flow**: server-core issues `JWT_REFRESH_TOKEN_EXPIRY` but no client currently refreshes. Same handler pattern applies.
- **Worldbuilding endpoints from the web**: `appsettings.Development.json` already has `ApiUrls:WorldbuildingURL` and the `Project` model has `IsAuthorActive`. The next step is a second typed `ApiClient` (`WorldbuildingClient`) bound to the worldbuilding URL and a `ManuscriptService` mirroring the desktop's `ManuscriptApiService`.
- **Logout button**: the layout currently has none. Wire it up against `Session.ClearAsync()` and let `LaylaAuthenticationStateProvider` notify the cascade.
- **CU-15 (admin dashboard)**: still placeholder content; needs the same data-binding pass once the patterns above are exercised on a real screen.
