# Plan: Corregir uso de HttpClient y mejorar arquitectura de Services

## Contexto

El proyecto tiene problemas con la configuración de `HttpClient`:
1. El `named client` "Backend" no se usa correctamente - `ApiClient` recibe un `HttpClient` sin configurar
2. La retry policy de Polly nunca se aplica al `ApiClient`
3. `PresenceService` usa `IHttpClientFactory.CreateClient("ServerCore")` pero ese cliente no está configurado
4. Servicios como `AuthService` y `ProjectService` no están registrados en DI

## Cambios a Realizar

### 1. `Config/HttpClientConfig.cs` - Corregir registro de HttpClient

**Problema**: Usa `AddHttpClient<ApiClient>("Backend", ...)` con named client, pero el nombre no se aplica.

**Solución**: Usar typed client sin nombre:

```csharp
services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiUrls:BackendURL"]!);
})
.AddPolicyHandler(retryPolicy);
```

**Eliminar**: `services.AddHttpClient();` genérico (redundante).

### 2. `Config/Services.cs` - Registrar servicios faltantes

**Problema**: `AuthService` y `ProjectService` no están en DI pero se inyectan en componentes Blazor.

**Solución**: Agregar registros:
```csharp
services.AddScoped<AuthService>();
services.AddScoped<ProjectService>();
```

### 3. `PresenceService` - Usar ApiClient en lugar de HttpClient directo

**Problema**: Usa `IHttpClientFactory.CreateClient("ServerCore")` que no existe.

**Solución**: Inyectar `ApiClient` y usar su método `RequestAsync`.

### 4. `Services/Auth/AuthService.cs` - Corregir tipo de retorno

**Problema**: `RequestAsync<LoginRequest>` debería ser `RequestAsync<LoginResponse>`.

**Solución**: Corregir el tipo genérico.

### 5. `Services/Http/RequestHttp.cs` - Agregar `required`

**Problema**: `Endpoint` y `Method` son críticos pero no tienen `required`.

**Solución**: Agregar `required` a propiedades esenciales.

### 6. `Services/Http/ApiClient.cs` - Mejorar manejo de errores

**Problema**: Pierde información del error original en algunos casos.

**Solución**: Preservar inner exception y datos de respuesta consistentemente.

## Archivos a Modificar

1. `Config/HttpClientConfig.cs` - Corregir configuración de HttpClient
2. `Config/Services.cs` - Registrar AuthService y ProjectService
3. `Services/PresenceService.cs` - Usar ApiClient
4. `Services/Auth/AuthService.cs` - Corregir tipo de retorno
5. `Services/Http/RequestHttp.cs` - Agregar required keywords
6. `Services/Http/ApiClient.cs` - Mejoras menores de error handling

## Verificación

1. Build del proyecto sin errores
2. Verificar que DI resuelve `ApiClient` con retry policy (revisar logs)
3. Testear login y llamada a projects API
4. Verificar que PresenceService conecta correctamente
