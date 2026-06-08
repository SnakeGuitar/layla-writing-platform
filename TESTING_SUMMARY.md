# Resumen de pruebas y compilacion

Este documento resume las suites de pruebas encontradas en Layla, como compilarlas y como ejecutarlas. El estado fue verificado localmente en `C:\Users\snake\Desktop\Layla`.

## Resumen actual

| Suite | Tecnologia | Ubicacion | Tests | Estado |
|---|---|---|---:|---|
| server-core | xUnit + NSubstitute, .NET 10 | `src/server-core/Layla.Core.Tests` | 187 | Verde |
| server-worldbuilding | Vitest, TypeScript | `src/server-worldbuilding/src/__tests__` | 131 | Verde en Vitest |
| client-web.Tests | xUnit, .NET 9 | `src/client-web.Tests` | 5 | No compila actualmente |
| Load tests | k6 | `tests/load` | 5 scripts | Requiere stack corriendo |
| Benchmarks | BenchmarkDotNet | `src/server-core/Layla.Core.Benchmarks` | 5 benchmarks | Ejecutar en Release |
| Android | JUnit/Espresso/Compose deps | `src/client-android` | 0 archivos reales | `NO-SOURCE` |

Total unitario descubierto: 323 tests. Actualmente pasan 318; los 5 de `client-web.Tests` estan bloqueados por error de compilacion.

## server-core

Proyecto de pruebas:

```powershell
C:\Users\snake\Desktop\Layla\src\server-core\Layla.Core.Tests\Layla.Core.Tests.csproj
```

Compilar:

```powershell
dotnet build C:\Users\snake\Desktop\Layla\src\server-core\Layla.Core.Tests\Layla.Core.Tests.csproj
```

Ejecutar:

```powershell
dotnet test C:\Users\snake\Desktop\Layla\src\server-core\Layla.Core.Tests\Layla.Core.Tests.csproj
```

Estado verificado: `187/187` tests pasaron.

Cobertura principal:

| Archivo | Tests |
|---|---:|
| `AppUserServiceTests.cs` | 17 |
| `AuthServiceTests.cs` | 25 |
| `GlobalExceptionMiddlewareTests.cs` | 6 |
| `OutboxProcessorWorkerTests.cs` | 13 |
| `PresenceTrackerTests.cs` | 27 |
| `ProjectServiceTests.cs` | 42 |
| `RequireUserIdFilterTests.cs` | 4 |
| `TokenServiceTests.cs` | 16 |
| `TokenVersionValidatorTests.cs` | 8 |
| `VoiceRoomManagerTests.cs` | 29 |

Durante la compilacion aparecen advertencias de vulnerabilidades conocidas en paquetes transitivos (`SharpCompress`, `Snappier`) y una advertencia `NU1510` sobre `Microsoft.Extensions.Diagnostics.HealthChecks`, pero no bloquean la suite.

## server-worldbuilding

Directorio:

```powershell
C:\Users\snake\Desktop\Layla\src\server-worldbuilding
```

Instalar dependencias:

```powershell
cd C:\Users\snake\Desktop\Layla\src\server-worldbuilding
corepack pnpm install --frozen-lockfile
```

Ejecutar tests:

```powershell
corepack pnpm test
```

Modo watch:

```powershell
corepack pnpm test:watch
```

Coverage:

```powershell
corepack pnpm test:coverage
```

Compilar/typecheck:

```powershell
corepack pnpm run build
```

Estado verificado: `131/131` tests pasaron con Vitest.

Desglose:

| Archivo | Tests |
|---|---:|
| `Auth.test.ts` | 27 |
| `Graph.service.test.ts` | 18 |
| `ManageJWT.test.ts` | 8 |
| `Manuscript.service.test.ts` | 19 |
| `Mention.service.test.ts` | 28 |
| `ProjectGuard.test.ts` | 14 |
| `WikiEntry.service.test.ts` | 17 |

Nota importante: `corepack pnpm run build` falla actualmente por errores de tipos en `src/__tests__/Graph.service.test.ts`. El test usa propiedades `id` y `label` en un `GraphNode`, pero la interfaz `GraphNode` define `entityId`, `name` y `entityType`.

## client-web.Tests

Proyecto:

```powershell
C:\Users\snake\Desktop\Layla\src\client-web.Tests\client-web.Tests.csproj
```

Compilar:

```powershell
dotnet build C:\Users\snake\Desktop\Layla\src\client-web.Tests\client-web.Tests.csproj
```

Ejecutar:

```powershell
dotnet test C:\Users\snake\Desktop\Layla\src\client-web.Tests\client-web.Tests.csproj
```

Estado actual: no compila.

Error principal:

```text
FakeSessionManager no implementa:
- ISessionManager.UpdateProfileAsync(string?, string?, string?)
- ISessionManager.CurrentAvatarUrl
- ISessionManager.CurrentBio
```

La causa es que `ISessionManager` crecio y el fake usado por la suite no fue actualizado. Cuando se corrija `src/client-web.Tests/Fakes/FakeSessionManager.cs`, deberian ejecutarse 5 tests:

| Archivo | Tests |
|---|---:|
| `ApiClientTests.cs` | 3 |
| `AuthStateProviderTests.cs` | 2 |

## Load tests

Directorio:

```powershell
C:\Users\snake\Desktop\Layla\tests\load
```

Estos scripts no se compilan. Requieren un stack corriendo, por ejemplo con Docker Compose o Vagrant.

Ejecutar:

```powershell
cd C:\Users\snake\Desktop\Layla\tests\load
k6 run auth.js
k6 run projects.js
k6 run manuscripts.js
k6 run signalr.js
k6 run scenarios.js
```

Sobrescribir usuarios virtuales en el workload mixto:

```powershell
k6 run -e TARGET_VUS=100 scenarios.js
```

Scripts:

| Script | Que prueba |
|---|---|
| `auth.js` | Login y registro contra `/api/tokens` y `/api/users` |
| `projects.js` | CRUD de proyectos |
| `manuscripts.js` | Lectura/escritura concurrente de capitulos |
| `signalr.js` | Negotiate, WebSocket y mensajes SignalR |
| `scenarios.js` | Carga mixta |

Nota local: `k6` no estaba disponible en `PATH` durante la verificacion.

## Benchmarks

Proyecto:

```powershell
C:\Users\snake\Desktop\Layla\src\server-core\Layla.Core.Benchmarks\Layla.Core.Benchmarks.csproj
```

Ejecutar en Release:

```powershell
cd C:\Users\snake\Desktop\Layla\src\server-core
dotnet run --project Layla.Core.Benchmarks -c Release
```

Miden operaciones relacionadas con JWT: generacion con uno o varios roles, validacion, parseo sin validacion y round-trip generar-validar.

## Android

Directorio:

```powershell
C:\Users\snake\Desktop\Layla\src\client-android
```

Compilar y ejecutar unit tests debug:

```powershell
C:\Users\snake\Desktop\Layla\src\client-android\gradlew.bat -p C:\Users\snake\Desktop\Layla\src\client-android testDebugUnitTest --no-daemon
```

Estado verificado: build correcto, pero `testDebugUnitTest` aparece como `NO-SOURCE` porque no hay archivos de prueba reales bajo `app/src/test` ni `app/src/androidTest`.

## Comando rapido por suite

```powershell
# server-core
dotnet test C:\Users\snake\Desktop\Layla\src\server-core\Layla.Core.Tests\Layla.Core.Tests.csproj

# server-worldbuilding
cd C:\Users\snake\Desktop\Layla\src\server-worldbuilding
corepack pnpm install --frozen-lockfile
corepack pnpm test

# client-web.Tests
dotnet test C:\Users\snake\Desktop\Layla\src\client-web.Tests\client-web.Tests.csproj

# load tests
cd C:\Users\snake\Desktop\Layla\tests\load
k6 run scenarios.js

# benchmarks
cd C:\Users\snake\Desktop\Layla\src\server-core
dotnet run --project Layla.Core.Benchmarks -c Release
```

