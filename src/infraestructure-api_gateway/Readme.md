# Layla — API Gateway

YARP (Yet Another Reverse Proxy) gateway that acts as the unified entry point for all Layla services. Routes HTTP, WebSocket, and gRPC traffic to the appropriate backend microservice.

---

## Why YARP

| Feature | YARP | Ocelot | Envoy |
|---|---|---|---|
| Ecosystem | .NET native | .NET native | Agnostic |
| HTTP/2 (gRPC) | ✅ Native | ⚠️ Limited | ✅ |
| WebSockets (SignalR) | ✅ Native | ✅ | ✅ |
| REST | ✅ | ✅ | ✅ |
| Maintained by | Microsoft | Community | CNCF |
| Performance | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| Dynamic config | ✅ | ✅ | ✅ |

---

## Quick Start

```bash
cd src/infraestructure-api_gateway
dotnet restore
dotnet run
```

The gateway starts on `http://localhost:5000`.

---

## Route Table

All routes are configured in `appsettings.json` under `ReverseProxy`. Traffic is forwarded to two backend clusters:

### core-cluster → `http://server-core:5287`

| Route | Match Pattern | Target |
|---|---|---|
| `tokens-route` | `/api/tokens/{**catch-all}` | Authentication |
| `users-route` | `/api/users/{**catch-all}` | User management |
| `projects-route` | `/api/projects/{**catch-all}` | Project management |
| `voice-route` | `/hubs/voice/{**catch-all}` | Voice SignalR hub |
| `presence-route` | `/hubs/presence/{**catch-all}` | Presence SignalR hub |
| `manuscript-hub-route` | `/hubs/manuscript/{**catch-all}` | Manuscript SignalR hub |
| `grpc-route` | `/grpc/{**catch-all}` | gRPC services |

### worldbuilding-cluster → `http://layla-worldbuilding:3000`

| Route | Match Pattern | Target |
|---|---|---|
| `manuscripts-route` | `/api/manuscripts/{**catch-all}` | Manuscript CRUD |
| `wiki-route` | `/api/wiki/{**catch-all}` | Wiki entries |
| `graph-route` | `/api/graph/{**catch-all}` | Narrative graph |

---

## Health Checks

- **Gateway health endpoint**: `GET /health` → returns `Healthy`
- **Active health checks**: Every 15 seconds, YARP pings `/health` on each backend cluster
- **Passive health checks**: Tracks request failures and marks destinations unhealthy; reactivation period is 30 seconds
- **Health check policy**: Custom `MinReplicas` (active + passive) defined in `Policies/`

---

## Middleware

| Component | Purpose |
|---|---|
| `CorrelationIdTransform` | Adds a correlation ID header to proxied requests |
| `X-Gateway-Timestamp` | Injects Unix timestamp header on every proxied request |
| Rate Limiter | Fixed-window rate limiter: 100 requests/minute, queue limit 10 |
| OPTIONS handler | Short-circuits CORS preflight requests with 204 |

---

## Configuration

The gateway reads its port from `Ports:HTTP` in configuration or from the `ASPNETCORE_URLS` environment variable.

| Setting | Default | Purpose |
|---|---|---|
| `Ports:HTTP` | `5000` | HTTP listening port |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Runtime environment |

---

## Docker

The Dockerfile builds a self-contained image. In docker-compose, the service is named `layla-api-gateway` and exposes port `5000`.

> **Note**: Authentication at the gateway level is disabled (see the commented-out JWT block in `Program.cs`). Authentication is handled by each backend service individually.