<<<<<<< HEAD
# Layla — Worldbuilding Service

Node.js + Express 5 backend for manuscripts, wiki entries, and the narrative graph.

- **Manuscripts** — stored in MongoDB (multiple per project, each with chapters)
- **Wiki entries** — stored in MongoDB (characters, locations, events, objects, concepts)
- **Narrative graph** — stored in Neo4j (nodes and relationships between wiki entities)

Authentication is delegated to **server-core**; this service validates the same JWT Bearer tokens.

---

## Running

```bash
pnpm install
pnpm run dev        # ts-node-dev with hot reload
pnpm run build      # compile to dist/
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

#### Last-Write-Wins (LWW) conflict detection

`PUT` on a chapter accepts an optional `clientTimestamp` (ISO-8601). If the provided
timestamp precedes the server's stored `updatedAt`, the request is rejected with
`409 Conflict` and the current server state is returned for client-side resolution.

### Wiki

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/wiki/:projectId` | List all wiki entries for a project |
| `POST` | `/api/wiki/:projectId` | Create a wiki entry |
| `GET` | `/api/wiki/:projectId/:entryId` | Get a wiki entry |
| `PUT` | `/api/wiki/:projectId/:entryId` | Update a wiki entry |
| `DELETE` | `/api/wiki/:projectId/:entryId` | Delete a wiki entry |

Wiki entry types: `CHARACTER` · `LOCATION` · `EVENT` · `OBJECT` · `CONCEPT`

### Graph

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/graph/:projectId` | Get full narrative graph (nodes + edges) |
| `POST` | `/api/graph/:projectId/nodes` | Create a graph node |
| `POST` | `/api/graph/:projectId/edges` | Create a relationship between nodes |
| `DELETE` | `/api/graph/:projectId/nodes/:nodeId` | Delete a node |
| `DELETE` | `/api/graph/:projectId/edges/:edgeId` | Delete an edge |

---

## Project Structure

```
src/
├── config/         Environment variable config
├── consumers/      RabbitMQ event consumers
├── controllers/    Route handlers
├── db/             MongoDB and Neo4j connection setup
├── docs/           OpenAPI specification (swagger.ts)
├── interfaces/     TypeScript interfaces (models + repositories)
├── middlewares/    JWT authentication, project access guard
├── models/         Mongoose schemas
├── repositories/   Data access layer
├── routes/         Express routers
├── services/       Business logic
├── utils/          Shared utilities (asyncHandler)
└── workers/        Background workers (Neo4j sync)
```

---

## Environment Variables

| Variable | Description |
|---|---|
| `PORT` | HTTP port (default `3000`) |
| `MONGO_URI` | MongoDB connection string |
| `NEO4J_URI` | Bolt URI (e.g. `bolt://localhost:7687`) |
| `NEO4J_USER` | Neo4j username |
| `NEO4J_PASSWORD` | Neo4j password |
| `RABBITMQ_URL` | AMQP connection string |
| `JWT_SECRET` | Must match server-core's signing key |
| `JWT_ISSUER` | Must match server-core's issuer claim |
| `JWT_AUDIENCE` | Must match server-core's audience claim |

---

## Use Cases

| ID | Name | Status |
|---|---|---|
| CU-08 | Edit manuscript (Rich Text) | ✅ |
| CU-09 | Manage wiki (Nodes) | 🔧 |
| CU-10 | Visualize narrative graph | 🔧 |
| CU-13 | Read full story | ❌ |

✅ Implemented · 🔧 Partial · ❌ Not started
=======
Para ejecución del backend
npx ts-node-dev src/index.ts

#Express - Typescript
--------------------------------------------------------------------------------------------------
| CU-## | Nombre del caso de uso            | Actor principal   | Módulo                | Estado |
|-------|-----------------------------------|------------------ | --------------------- | ------ |
| CU-06	| Gestionar Colaboradores           | Escritor          | Gestión               |   ❌  |
| CU-07	| Configurar Privacidad	            | Escritor          | Gestión               |   ❌  |
| CU-08	| Editar Manuscrito (Texto Rico)    | Editor / Escritor | Escritura (MongoDB)   |   ❌  |
| CU-09	| Gestionar Wiki (Nodos)            | Editor / Escritor	| Worldbuilding (Neo4j) |   ❌  |
| CU-10	| Visualizar Relaciones (Grafo)	    | Lector / Editor   | Worldbuilding (Neo4j) |   ❌  |
| CU-15 | Gestionar Usuarios (Ban/Roles)    | Administrador     | Admin                 |   ❌  |

## CU-06 Gestionar Colaboradores
### Descripción
El Escritor invita a otros usuarios a participar en la obra, asignándoles roles específicos (Editor) para permitir la co-autoría o corrección.
### Precondiciones
El usuario debe ser el Propietario del proyecto (Role Owner).
### Postcondiciones
POS-1: El usuario invitado ahora puede ver y editar el proyecto en su propio Dashboard.
### Flujo normal
1.	El Sistema muestra la lista actual de colaboradores.
2.	El Escritor ingresa el correo electrónico del usuario a invitar.
3.	El Sistema busca al usuario en la base de datos de identidad (SQL Server).
4.	El Sistema valida que el usuario exista (FA-01).
5.	El Escritor selecciona el rol a asignar (ej. "Editor").
6.	El Sistema registra el permiso en la tabla de Roles del proyecto.
7.	El Sistema envía una notificación al usuario invitado.
8.	Termina CU.

>>>>>>> 82f7fa1 (Adding express-ts backend)
