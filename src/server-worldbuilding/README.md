# Layla — Worldbuilding Service

Node.js + Express 5 backend for manuscripts, wiki entries, and the narrative graph.

- **Manuscripts** — stored in MongoDB (multiple per project, each with ordered chapters)
- **Wiki entries** — stored in MongoDB (characters, locations, events, objects, concepts)
- **Narrative graph** — stored in Neo4j (nodes and relationships between wiki entities)

Authentication is delegated to **server-core**; this service validates the same JWT Bearer tokens.

---

## Quick Start

```bash
cd src/server-worldbuilding
pnpm install
pnpm run dev        # tsx watch src/index.ts (hot reload)
pnpm run build      # compile to dist/
pnpm run start      # run compiled dist/index.js
```

The server starts on `http://localhost:3000`.

---

## API Documentation

Swagger UI is available at `http://localhost:3000/api-docs` once the server is running.
The raw OpenAPI JSON spec is at `http://localhost:3000/api-docs.json`.

---

## API Reference

All endpoints require a valid `Authorization: Bearer <token>` header and that the caller
holds any role in the target project (validated via `requireProjectAccess` middleware).
Write operations additionally require OWNER or EDITOR role (via `requireWriteAccess`).

### Health

| Method | Path | Description |
|---|---|---|
| `GET` | `/health` | Service health check — returns `OK` |

### Manuscripts

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/manuscripts/:projectId` | List all manuscripts (chapter index, no content) |
| `POST` | `/api/manuscripts/:projectId` | Create a manuscript — body: `{ title, order? }` |
| `GET` | `/api/manuscripts/:projectId/:manuscriptId` | Get manuscript with chapter index |
| `PUT` | `/api/manuscripts/:projectId/:manuscriptId` | Rename or reorder — body: `{ title?, order? }` |
| `DELETE` | `/api/manuscripts/:projectId/:manuscriptId` | Delete manuscript and all its chapters |

### Chapters

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/manuscripts/:projectId/:manuscriptId/chapters` | Create chapter — body: `{ title, content?, order? }` |
| `GET` | `/api/manuscripts/:projectId/:manuscriptId/chapters/:chapterId` | Get chapter with full RTF content |
| `PUT` | `/api/manuscripts/:projectId/:manuscriptId/chapters/:chapterId` | Update chapter (Last-Write-Wins) |
| `DELETE` | `/api/manuscripts/:projectId/:manuscriptId/chapters/:chapterId` | Delete chapter |
| `GET` | `/api/manuscripts/:projectId/:manuscriptId/chapters/:chapterId/mentions` | Get wiki entity mentions detected in the chapter |
| `PUT` | `/api/manuscripts/:projectId/:manuscriptId/chapters/:chapterId/autosave` | Autosave with mentions and optional milestone flag |
| `GET` | `/api/manuscripts/:projectId/:manuscriptId/chapters/:chapterId/versions` | List version history |
| `GET` | `/api/manuscripts/:projectId/:manuscriptId/chapters/:chapterId/versions/:versionId` | Get a specific version |
| `PUT` | `/api/manuscripts/:projectId/:manuscriptId/chapters/:chapterId/versions/:versionId/restore` | Restore chapter to a specific version |

#### Last-Write-Wins (LWW) conflict detection

`PUT` on a chapter accepts an optional `clientTimestamp` (ISO-8601). If the provided
timestamp precedes the server's stored `updatedAt`, the request is rejected with
`409 Conflict` and the current server state is returned for client-side resolution.

### Wiki

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/wiki/:projectId/entries` | List all wiki entries (optional `?type=` filter) |
| `GET` | `/api/wiki/:projectId/detectable` | Get entities optimized for Aho-Corasick tokenizer |
| `POST` | `/api/wiki/:projectId/entries` | Create a wiki entry |
| `GET` | `/api/wiki/:projectId/entries/:entityId` | Get a wiki entry |
| `PUT` | `/api/wiki/:projectId/entries/:entityId` | Update a wiki entry |
| `DELETE` | `/api/wiki/:projectId/entries/:entityId` | Delete a wiki entry |
| `GET` | `/api/wiki/:projectId/entries/:entityId/appearances` | Get chapters where this entity appears |

Wiki entry types: `Character` · `Location` · `Event` · `Object` · `Concept`

### Graph

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/graph/:projectId` | Get full narrative graph — nodes + edges (optional `?type=` filter) |
| `POST` | `/api/graph/:projectId/relationships` | Create a directed relationship between entities |
| `DELETE` | `/api/graph/:projectId/relationships` | Delete relationships between entities (body-based) |

---

## Project Structure

```
src/
├── config/         Environment variable loading and validation
├── consumers/      RabbitMQ event consumers (projectCreated, collaborator)
├── controllers/    Route handlers
├── db/             MongoDB and Neo4j connection setup
├── docs/           OpenAPI specification (swagger.ts)
├── interfaces/     TypeScript interfaces (models + repositories)
├── middlewares/    JWT authentication, project access guard, rate limiter
├── models/         Mongoose schemas
├── repositories/   Data access layer
├── routes/         Express routers
├── services/       Business logic
├── utils/          Shared utilities (asyncHandler)
├── validation/     Zod schemas for request body validation
└── workers/        Background workers (Neo4j sync)
```

---

## Environment Variables

| Variable | Description |
|---|---|
| `PORT` | HTTP port (default `3000`) |
| `MONGODB_URI` | MongoDB connection string (auto-constructed from parts if not set) |
| `NEO4J_URI` | Bolt URI (e.g. `bolt://localhost:7687`) |
| `NEO4J_USERNAME` | Neo4j username |
| `NEO4J_PASSWORD` | Neo4j password |
| `RABBITMQ_URL` | AMQP connection string (auto-constructed from parts if not set) |
| `JWT_SECRET` | Must match server-core's signing key (min 32 chars) |
| `JWT_SECRET_REFRESH` | Must match server-core's refresh signing key (min 32 chars) |
| `JWT_ACCESS_TOKEN_EXPIRY` | Access token expiry (e.g. `1440` minutes) |
| `JWT_REFRESH_TOKEN_EXPIRY` | Refresh token expiry (e.g. `10080` minutes) |
| `ALLOWED_ORIGINS` | Comma-separated CORS origins |
| `CORE_API_URL` | server-core URL for access-control fallback (default `http://localhost:5287`) |
| `RABBITMQ_EXCHANGE` | RabbitMQ exchange name (default `worldbuilding.events`) |
| `RABBITMQ_QUEUE` | RabbitMQ queue name (default `worldbuilding.node.queue`) |

For local development, create a `.env.development` file (see `.env.development` for the template).

---

## TypeScript Path Aliases

The project uses `@/` path aliases mapped to `src/` via `tsconfig.json`. All imports use `@/config/env`, `@/db/mongoose`, etc.

The service implements graceful shutdown — `SIGTERM` and `SIGINT` handlers close HTTP, RabbitMQ, and Neo4j connections in order.

---

## Use Cases

| ID | Name | Status |
|---|---|---|
| CU-08 | Edit manuscript (Rich Text) | ✅ |
| CU-09 | Manage wiki (Nodes) | ✅ |
| CU-10 | Visualize narrative graph | ✅ |
| CU-13 | Read full story | ❌ |

✅ Implemented · 🔧 Partial · ❌ Not started
