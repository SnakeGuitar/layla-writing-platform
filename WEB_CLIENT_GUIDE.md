# Web Client — Configuration & Architecture Guide

This document explains **how the Blazor web client is wired together**, so a new contributor can read it once and know where everything lives. It covers the bootstrap flow, the DI graph, the authentication pipeline, the HTTP layer, and the routing model.

> **TL;DR:** The web client is a **Blazor Server** app (interactive over SignalR) that talks to **server-core** for identity/projects and to **server-worldbuilding** for manuscripts/wiki/graph. State is **per-circuit** (`Scoped`), the JWT lives in **`ProtectedSessionStorage`**, and `[Authorize]` works because we install Blazor's standard auth pipeline on top of a custom `AuthenticationStateProvider`.

---

## 1. Project layout

```
src/client-web/
├── Program.cs                       Entry point — three Configure() calls and Run()
├── client-web.csproj                Target net9.0, packages (SignalR, Polly, JWT, Auth)
├── appsettings.Development.json     ApiUrls + SignalR hub paths
│
├── Config/                          Composition root — split by concern
│   ├── Builder.cs                   Razor components, kestrel options, etc.
│   ├── HttpClientConfig.cs          Typed HttpClient + Polly retry policy
│   └── Services.cs                  DI registrations (this is the big one)
│
├── Application/                     "Backend" code — services, DTOs, infrastructure
│   ├── Config/
│   │   ├── Http/                    ApiClient, APIRequest, APIException
│   │   └── SignalR/                 SignalRClient (used by voice + presence)
│   ├── Schemas/                     Wire DTOs (Auth, Project)
│   │   └── Auth/
│   │       ├── LoginRequest.cs      .Validate() ↦ ValidationResult
│   │       ├── LoginResponse.cs     Mirrors server-core AuthResponseDto
│   │       └── RegisterRequest.cs   .Validate() ↦ ValidationResult
│   └── Services/
│       ├── Auth/                    IAuthService + LaylaAuthenticationStateProvider
│       ├── Session/                 ISessionManager + SessionManager
│       ├── Projects/                IProjectService + ProjectService
│       ├── ActiveStatusAuthor/      PresenceService (SignalR)
│       └── Voice/                   IVoiceService + sub-interfaces
│
├── Models/                          Domain models surfaced to the UI
│   ├── Authentication/AuthResult.cs Wrapped login/register outcome
│   ├── Project.cs, CreateProjectRequest.cs, UpdateProjectRequest.cs
│   └── …
│
├── Helpers/Validation/              Shared with the desktop — same rules
│   ├── ValidationResult.cs
│   └── ValidationService.cs
│
└── UI/                              Blazor components live here
    ├── _Imports.razor               Global @using (auth + forms + virtualization)
    ├── App.razor                    HTML shell + <Routes/> mount
    ├── Routes.razor                 AuthorizeRouteView + NotFound + RedirectToLogin
    ├── Layout/
    │   ├── MainLayout.razor         Default authenticated layout
    │   └── LayoutEmpty.razor        Login / Register layout (no chrome)
    ├── Components/
    │   ├── ProjectCard.razor, ProjectCard2.razor
    │   └── RedirectToLogin.razor    Used by Routes.razor's NotAuthorized
    └── Pages/
        ├── Home.razor               "/" + "/home" — public catalog
        ├── Auth/Login.razor, Register.razor
        ├── Projects/MyProjects.razor (uses [Authorize])
        └── Errors/Error.razor
```

The **`Application/` vs `UI/` vs `Models/` vs `Helpers/`** split is intentional:
- `Application/` is plain C# — could be moved to a class library tomorrow.
- `UI/` is Blazor-specific (`.razor`, `@inject`, `@page`).
- `Models/` is the public surface that crosses the `Application/UI` boundary.
- `Helpers/` is leaf code with no project-internal dependencies.

---

## 2. Bootstrap flow

`Program.cs` is intentionally tiny:

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

HttpClientConfig.Configure(builder.Services, builder);   // (1) HTTP layer
Builder.Configure(builder.Services, builder);            // (2) Razor / Kestrel
Services.Configure(builder.Services);                    // (3) DI graph

var app = builder.Build();
// pipeline: Hsts → Health → HttpsRedirect → StaticFiles → Antiforgery → MapRazorComponents
app.Run();
```

Each `Configure(...)` is a `static` method in the `Config/` folder, so the wiring is greppable and the entry point stays a one-screen file.

### 2.1 `HttpClientConfig.Configure`

Registers a **typed** `HttpClient` for `ApiClient` (the wrapper used to hit server-core), with a **Polly retry policy**:

- Retries on transient failures: `HttpRequestException`, `TaskCanceledException`, 5xx, 408, 429.
- 3 attempts, exponential backoff (200 ms → 400 ms → 800 ms).

The base address comes from `ApiUrls:BackendURL` (`https://localhost:5288` in Development). A second URL — `ApiUrls:WorldbuildingURL` — is also defined in `appsettings.Development.json` for the future Node.js client.

### 2.2 `Builder.Configure`

Razor-component / Kestrel / antiforgery wiring. Boring but necessary.

### 2.3 `Services.Configure` — the DI graph

This is where the application's behaviour is composed. Reading top-to-bottom, in dependency order:

```csharp
// HTTP
services.AddScoped<ApiClient>();

// Session + Auth
services.AddScoped<ProtectedSessionStorage>();
services.AddScoped<ISessionManager, SessionManager>();
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<AuthenticationStateProvider, LaylaAuthenticationStateProvider>();
services.AddAuthorizationCore();
services.AddCascadingAuthenticationState();

// Domain
services.AddScoped<PresenceService>();
services.AddScoped<IProjectService, ProjectService>();

// Voice (single SignalR client process-wide)
services.AddSingleton<ISignalRClient, SignalRClient>();
services.AddSingleton<IVoiceService, VoiceService>();
services.AddSingleton<IConnectionService>(sp => sp.GetRequiredService<IVoiceService>());
services.AddSingleton<IRoomService>(sp => sp.GetRequiredService<IVoiceService>());
services.AddSingleton<IAudioService>(sp => sp.GetRequiredService<IVoiceService>());
```

**Lifetimes — and why:**

| Service | Lifetime | Why |
|---|---|---|
| `ApiClient` | `Scoped` | Holds a request-bound `HttpClient`; one per circuit avoids cross-user header bleed. |
| `ProtectedSessionStorage` | `Scoped` | Bound to the current user's circuit. |
| `ISessionManager` | `Scoped` | **Per-user state.** A `static` would leak Alice's token into Bob's circuit. |
| `IAuthService` | `Scoped` | Reads/writes through `ApiClient` + `ISessionManager`. |
| `AuthenticationStateProvider` | `Scoped` | Built into Blazor's auth model; identity is per-circuit. |
| `IProjectService` | `Scoped` | Pulls token from `ISessionManager`. |
| `PresenceService` | `Scoped` | Holds a SignalR connection bound to the user's token. |
| `ISignalRClient`, `IVoiceService` | `Singleton` | Voice room state is process-wide (multiple users join the same room). |

**Rule of thumb:** anything that *holds the user's identity* is `Scoped`. Anything that *holds room/process-wide state* is `Singleton`.

`AddCascadingAuthenticationState()` is the Blazor 9 way to make the current `AuthenticationState` available to every component without wrapping `<Routes>` in `<CascadingAuthenticationState>` manually.

---

## 3. Authentication pipeline (end-to-end)

### 3.1 What signs the user in

```
[Login.razor]  user types email + password
      │
      ▼  HandleLogin()
[IAuthService.LoginAsync(LoginRequest)]
      │ 1. request.Validate()   ← client-side; bails early on bad input
      │ 2. ApiClient.SendAsync<LoginResponse>("/api/tokens", POST, body)
      │ 3. wrap into AuthResult.Success/Fail/ValidationError
      ▼
[Login.razor]  if (result.IsSuccess)
      │
      ▼
[ISessionManager.SaveAsync(LoginResponse)]
      │ 1. write Token/UserId/Email/DisplayName/ExpiresAt to in-memory fields
      │ 2. write StoredSession JSON to ProtectedSessionStorage["layla.session"]
      │ 3. raise SessionChanged
      ▼
[LaylaAuthenticationStateProvider.OnSessionChanged]
      │ NotifyAuthenticationStateChanged(new AuthenticationState(...))
      ▼
Every <AuthorizeView>, [Authorize] page, and cascading consumer re-renders.
      │
      ▼
NavigationManager.NavigateTo("/home")
```

### 3.2 What carries the identity on subsequent navigations

`<CascadingAuthenticationState>` cascades the current `AuthenticationState`. Every protected page just adds `@attribute [Authorize]` and the framework calls our `LaylaAuthenticationStateProvider.GetAuthenticationStateAsync()`, which:

1. Reads `Session.CurrentToken` from the in-memory cache.
2. Decodes the JWT with `JwtSecurityTokenHandler.ReadJwtToken`.
3. Builds a `ClaimsIdentity` with `nameType: "name"` and `roleType: "role"` (matching the claim names server-core uses).
4. Returns a `ClaimsPrincipal`.

If the token is missing, expired, or malformed, the provider returns the static `Anonymous` state — which `AuthorizeRouteView` then routes to `<RedirectToLogin />`.

### 3.3 What survives a page reload

`SessionManager` persists the snapshot to `ProtectedSessionStorage`, which is:
- **Encrypted** with the server's data-protection keys (the cookie payload is opaque).
- **Tab-scoped** — closing the tab discards it.

But `ProtectedSessionStorage` requires JS interop, which is **unavailable during prerender**. So the pattern in every page that needs the token is:

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (!firstRender) return;
    await Session.InitializeAsync();   // hydrates from ProtectedSessionStorage
    StateHasChanged();                 // re-render with the restored identity
}
```

`Home.razor` and `MyProjects.razor` already follow this — replicate it on any page that needs the token before the second render.

### 3.4 What happens on logout (when wired)

Pending. The plumbing is ready: call `Session.ClearAsync()` and the cascade does the rest. A logout button in `MainLayout.razor` is the only missing piece.

---

## 4. HTTP layer

`Application/Config/Http/` is a thin wrapper around `HttpClient` that:

1. Forces camelCase JSON to match ASP.NET Core defaults.
2. Treats HTTP status as the success/error signal (no `{ data, error }` envelope — server-core returns DTOs directly).
3. Throws a single typed `APIException(message, status, raw)` on failure, with best-effort message extraction from `ProblemDetails`-style bodies.
4. Lets callers attach a `Bearer` token per request via `APIRequest.Token`.

The flow on every API call:

```
[ProjectService.GetMyProjectsAsync]
      │ Token = _session.IsAuthenticated ? _session.CurrentToken : null
      ▼
[ApiClient.SendAsync<IEnumerable<Project>>(APIRequest)]
      │ BuildHttpRequest:
      │   - method + endpoint
      │   - Authorization: Bearer <token>
      │   - body serialized with camelCase
      │ SendAsync via HttpClient
      │   ↳ wrapped with Polly retry (3 attempts, exponential backoff)
      │ on success: deserialize into T
      │ on failure: throw APIException
      ▼
Caller catches APIException → log → degrade (return [], null, false).
```

**Why a typed `ApiClient`** rather than calling `HttpClient` directly:
- One place to enforce JSON conventions.
- One place to map errors.
- One place to attach the retry policy (it's bound to the typed client at registration, so every call gets it for free).

---

## 5. Routing & layouts

### 5.1 `Routes.razor`

```razor
<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="routeData"
                            DefaultLayout="typeof(Layout.MainLayout)">
            <NotAuthorized>
                <RedirectToLogin />
            </NotAuthorized>
        </AuthorizeRouteView>
    </Found>
    <NotFound>
        <LayoutView Layout="typeof(Layout.MainLayout)">
            <p>Page not found.</p>
        </LayoutView>
    </NotFound>
</Router>
```

- **`AuthorizeRouteView`** (instead of plain `RouteView`) honours `[Authorize]` on `@page` components.
- **`NotAuthorized` ↦ `RedirectToLogin`** captures the original URL into a `?returnUrl=` query parameter so the user lands back where they started after signing in.
- **`NotFound`** catches unmapped routes and renders inside `MainLayout`.

### 5.2 Layouts

| Layout | When | What it includes |
|---|---|---|
| `MainLayout` | Default for every authenticated page | Header, sidebar, theme |
| `LayoutEmpty` | Login + Register | No chrome — just the centred card on the gradient background |

Per-page selection: `@layout client_web.UI.Layout.LayoutEmpty` at the top of the file.

### 5.3 Render mode

Every interactive page declares:

```razor
@rendermode InteractiveServer
```

Pages that are pure read-only static markup (like an "About" page if it existed) wouldn't need this. Anything that calls a service or reacts to user input does.

---

## 6. Configuration files

### `appsettings.Development.json`

```json
{
  "ApiUrls": {
    "BackendURL":      "https://localhost:5288",
    "WorldbuildingURL": "http://localhost:3000",
    "SignalRHubURL": {
      "VoiceServiceHub":    "/hubs/voice",
      "PresenceServiceHub": "/hubs/presence"
    }
  }
}
```

- `BackendURL` — the .NET server-core API. **Used as the base address of `ApiClient`.**
- `WorldbuildingURL` — the Node.js worldbuilding API. Reserved for the manuscripts/wiki/graph client (not yet wired in).
- `SignalRHubURL.*` — relative paths appended to `BackendURL` by `PresenceService` and `VoiceService`.

### `client-web.csproj`

The packages that matter:

| Package | Role |
|---|---|
| `Microsoft.AspNetCore.Components.Authorization` | `<AuthorizeRouteView>`, `<AuthorizeView>`, `AuthenticationStateProvider` |
| `Microsoft.AspNetCore.Authorization` | `[Authorize]` attribute |
| `Microsoft.AspNetCore.SignalR.Client` | Presence + voice hubs |
| `Microsoft.Extensions.Http.Polly` + `Polly` | Retry policy on the HTTP client |
| `System.IdentityModel.Tokens.Jwt` | JWT decoding inside `LaylaAuthenticationStateProvider` |

---

## 7. End-to-end: how a request to "list my projects" flows

```
User clicks "/projects/my-projects"
        │
        ▼
[Router]
        │ matches @page "/projects/my-projects" on MyProjects.razor
        │ AuthorizeRouteView checks [Authorize]
        │   ↳ asks AuthenticationStateProvider.GetAuthenticationStateAsync()
        │   ↳ reads Session.CurrentToken → decodes JWT → ClaimsPrincipal
        │   ↳ if anonymous, renders <RedirectToLogin/> instead
        ▼
[MyProjects.razor]
        │ OnInitializedAsync → LoadProjectsAsync()
        │ OnAfterRenderAsync(firstRender) → Session.InitializeAsync()
        │   (in case the circuit just woke up and only browser storage has the token)
        ▼
[IProjectService.GetMyProjectsAsync()]
        │ Token = Session.IsAuthenticated ? Session.CurrentToken : null
        ▼
[ApiClient.SendAsync<IEnumerable<Project>>]
        │ GET /api/projects + Bearer <token>
        │ Polly retries on transient failures
        ▼
[server-core: ProjectsController]
        │ JWT validated, project list pulled from EF Core
        │ returns IEnumerable<ProjectResponseDto> (camelCase JSON)
        ▼
[ApiClient]
        │ deserialize → IEnumerable<Project>
        ▼
[MyProjects.razor]
        │ renders <ProjectCard2 .../> for each project
```

Every link in that chain is replaceable in isolation: swap `ApiClient` for a fake to unit-test the service, swap `ISessionManager` for a stub to unit-test the auth state provider, etc.

---

## 8. Conventions worth knowing

- **Per-field validation errors** flow `ValidationService.IsXxx` → `LoginRequest.Validate` → `AuthResult.ValidationErrors` → `_fieldErrors[propertyName]` in the page → `<p class="text-red-600">…</p>` next to the input.
- **Errors never bubble to the user as exceptions.** Services catch `APIException` and either return a wrapped `AuthResult.Fail(...)` or degrade to `null` / `[]`. The page reads the explicit failure and renders an inline message.
- **Logging** uses `ILogger<T>` everywhere — no `Console.WriteLine`, no `Debug.WriteLine`. Logs will end up in whatever sink the host configures.
- **camelCase JSON** at the wire (matching server-core's defaults). PascalCase C# properties bind correctly because `PropertyNameCaseInsensitive = true`.
- **Razor binding mode**: `@bind="_email"` (write-on-change) is fine for non-trivial forms because `InteractiveServer` debounces over the SignalR circuit. For per-keystroke validation, switch to `@bind-Value:event="oninput"`.

---

## 9. Where to add things

| You want to… | Touch this |
|---|---|
| Add a new authenticated page | New `.razor` under `UI/Pages/`, decorate with `@page` + `@attribute [Authorize]` + `@rendermode InteractiveServer` |
| Add a new server-core endpoint call | New method on `IProjectService` (or a new service alongside it); call `_client.SendAsync<T>(new APIRequest{...})` |
| Add a new field to the user session | Extend `LoginResponse` + `StoredSession` (in `SessionManager`) + `ISessionManager` |
| Add a global validation rule | Extend `ValidationService` (mirror it on the desktop too) |
| Add a worldbuilding API call | First, create a second typed `HttpClient` (`AddHttpClient<WorldbuildingClient>`) bound to `ApiUrls:WorldbuildingURL`, then build a service like `ProjectService` against it |
| Add a logout button | Inject `ISessionManager` into `MainLayout.razor`, call `Session.ClearAsync()` on click — `AuthorizeRouteView` redirects to login automatically |
| Add a 401 auto-logout | New `DelegatingHandler` on the typed `ApiClient` registration: on 401, call `Session.ClearAsync()` and bubble the failure |
| Inspect what the user sees | `Session.CurrentEmail`, `.CurrentDisplayName`, `.CurrentUserId`, `.CurrentToken` (don't log the token) |

---

## 10. Quick reference: services at a glance

| Service | Lives in | Lifetime | What it does |
|---|---|---|---|
| `ApiClient` | `Application/Config/Http` | Scoped | Typed HTTP wrapper with retry + JSON conventions |
| `ISessionManager` | `Application/Services/Session` | Scoped | In-memory + ProtectedSessionStorage session cache |
| `IAuthService` | `Application/Services/Auth` | Scoped | `LoginAsync` / `RegisterAsync` ↦ `AuthResult` |
| `AuthenticationStateProvider` | (built-in slot) | Scoped | `LaylaAuthenticationStateProvider` decodes the JWT into a `ClaimsPrincipal` |
| `IProjectService` | `Application/Services/Projects` | Scoped | Project CRUD against server-core |
| `PresenceService` | `Application/Services/ActiveStatusAuthor` | Scoped | SignalR presence hub client |
| `IVoiceService` | `Application/Services/Voice` | Singleton | SignalR voice hub client (process-wide) |
| `ISignalRClient` | `Application/Config/SignalR` | Singleton | Low-level SignalR plumbing shared by Voice + Presence |

---

## 11. Things that would surprise you

1. **`SessionManager` is `Scoped`, not static.** The desktop `SessionManager` is `static` because the desktop process hosts exactly one user. Blazor Server hosts every user simultaneously over SignalR circuits — a static would leak Alice's token into Bob's circuit.
2. **The web client doesn't validate JWT signatures.** It calls `JwtSecurityTokenHandler.ReadJwtToken` (decode only). The server is the only authority — every protected API call sends the token back, and server-core rejects tampered tokens. Decoding locally is purely so Blazor can apply `[Authorize]` and `<AuthorizeView Roles="...">`.
3. **`appsettings.json` is gitignored** but `appsettings.Development.json` is committed (see `src/.gitignore`). Real secrets go in `appsettings.json` or User Secrets — never in the committed dev file.
4. **`@bind` on a Razor input commits on blur, not on every keystroke.** This is fine here, but if you wire keystroke-level validation, switch to `@bind:event="oninput"`.
5. **`OnAfterRenderAsync(firstRender)` is the only place** where you can call into `ProtectedSessionStorage` reliably. Calling it from `OnInitializedAsync` works on subsequent navigations but throws during the very first prerender pass.

---

If something in this document drifts from the code, **the code wins** — patch the doc when the wiring changes.
