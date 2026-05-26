import swaggerJsdoc from "swagger-jsdoc";

/**
 * OpenAPI 3.0 specification for the Layla Worldbuilding Service.
 *
 * The definition is built programmatically here rather than via JSDoc annotations
 * so that it stays co-located with the route types and can be version-controlled
 * as a single source of truth.
 */
const definition: swaggerJsdoc.OAS3Definition = {
  openapi: "3.0.3",
  info: {
    title: "Layla — Worldbuilding Service API",
    version: "1.0.0",
    description:
      "REST API for the Layla worldbuilding service. Handles manuscripts, chapters, " +
      "wiki entries, and the narrative graph (Neo4j). Authentication is delegated to the " +
      "server-core service; this API validates the same JWT Bearer tokens.",
    contact: {
      name: "Layla Project",
    },
  },
  servers: [
    {
      url: "http://localhost:3000",
      description: "Local development server",
    },
  ],
  components: {
    securitySchemes: {
      BearerAuth: {
        type: "http",
        scheme: "bearer",
        bearerFormat: "JWT",
        description:
          "JWT token issued by the server-core `/api/tokens` endpoint. " +
          "Include in the `Authorization` header as `Bearer <token>`.",
      },
    },
    schemas: {
      Mention: {
        type: "object",
        description:
          "A reference from a chapter to a wiki entity, detected automatically from the text.",
        properties: {
          entityId: {
            type: "string",
            format: "uuid",
            description: "UUID of the referenced wiki entry.",
          },
          name: {
            type: "string",
            description: "Entity name at the time of detection.",
          },
          entityType: {
            type: "string",
            description: "Entity type (Character, Location, Event, Object).",
          },
        },
      },
      AppearanceRecord: {
        type: "object",
        description: "A chapter in which a wiki entity appears.",
        properties: {
          manuscriptId: { type: "string", format: "uuid" },
          manuscriptTitle: { type: "string" },
          chapterId: { type: "string", format: "uuid" },
          chapterTitle: { type: "string" },
        },
      },
      Chapter: {
        type: "object",
        properties: {
          chapterId: {
            type: "string",
            format: "uuid",
            description: "UUID identifying the chapter.",
          },
          title: {
            type: "string",
            maxLength: 200,
            description: "Display title shown in the navigation panel.",
          },
          content: {
            type: "string",
            description: "Full RTF content. Omitted in index responses.",
          },
          order: {
            type: "integer",
            minimum: 0,
            description: "Zero-based position within the manuscript.",
          },
          mentions: {
            type: "array",
            description: "Wiki entities referenced in this chapter.",
            items: { $ref: "#/components/schemas/Mention" },
          },
          createdAt: { type: "string", format: "date-time" },
          updatedAt: { type: "string", format: "date-time" },
        },
      },
      ChapterIndex: {
        type: "object",
        description:
          "Chapter metadata without content — used in manuscript index responses.",
        properties: {
          chapterId: { type: "string", format: "uuid" },
          title: { type: "string" },
          order: { type: "integer", minimum: 0 },
          createdAt: { type: "string", format: "date-time" },
          updatedAt: { type: "string", format: "date-time" },
        },
      },
      Manuscript: {
        type: "object",
        properties: {
          manuscriptId: {
            type: "string",
            format: "uuid",
            description: "UUID identifying the manuscript.",
          },
          projectId: {
            type: "string",
            format: "uuid",
            description: "UUID of the owning project (issued by server-core).",
          },
          title: {
            type: "string",
            maxLength: 200,
            description: "Human-readable title (e.g. 'Book 1', 'Draft 2').",
          },
          order: {
            type: "integer",
            minimum: 0,
            description:
              "Zero-based display order among the project's manuscripts.",
          },
          chapters: {
            type: "array",
            description: "Chapter index — content fields are omitted.",
            items: { $ref: "#/components/schemas/ChapterIndex" },
          },
          createdAt: { type: "string", format: "date-time" },
          updatedAt: { type: "string", format: "date-time" },
        },
      },
      CreateManuscriptBody: {
        type: "object",
        required: ["title"],
        properties: {
          title: { type: "string", maxLength: 200 },
          order: { type: "integer", minimum: 0 },
        },
      },
      UpdateManuscriptBody: {
        type: "object",
        properties: {
          title: { type: "string", maxLength: 200 },
          order: { type: "integer", minimum: 0 },
        },
      },
      CreateChapterBody: {
        type: "object",
        required: ["title"],
        properties: {
          title: { type: "string", maxLength: 200 },
          content: {
            type: "string",
            description: "Initial RTF content. Defaults to empty string.",
          },
          order: { type: "integer", minimum: 0 },
        },
      },
      UpdateChapterBody: {
        type: "object",
        properties: {
          title: { type: "string", maxLength: 200 },
          content: { type: "string" },
          order: { type: "integer", minimum: 0 },
          clientTimestamp: {
            type: "string",
            format: "date-time",
            description:
              "ISO-8601 timestamp of the client's last known server state. " +
              "When supplied and older than the server's `updatedAt`, the request is " +
              "rejected with **409 Conflict** (Last-Write-Wins guard).",
          },
        },
      },
      WikiEntry: {
        type: "object",
        properties: {
          entityId: { type: "string", format: "uuid" },
          projectId: { type: "string", format: "uuid" },
          name: { type: "string", maxLength: 200 },
          entityType: {
            type: "string",
            enum: ["Character", "Location", "Event", "Object", "Concept"],
            description: "Category of the wiki entry.",
          },
          description: { type: "string" },
          tags: { type: "array", items: { type: "string" } },
          createdAt: { type: "string", format: "date-time" },
          updatedAt: { type: "string", format: "date-time" },
        },
      },
      ErrorResponse: {
        type: "object",
        properties: {
          error: {
            type: "string",
            description: "Human-readable error message.",
          },
        },
      },
      ConflictResponse: {
        type: "object",
        properties: {
          error: {
            type: "string",
            example: "Version conflict (Last-Write-Wins)",
          },
          currentVersion: { $ref: "#/components/schemas/Chapter" },
        },
      },
      AutosaveChapterBody: {
        type: "object",
        description:
          "Payload for the debounced autosave endpoint. Includes content, " +
          "locally-detected wiki mentions, and an optional milestone flag.",
        properties: {
          content: {
            type: "string",
            description: "Full RTF content to persist.",
          },
          mentions: {
            type: "array",
            description: "Wiki entity mentions detected client-side.",
            items: { $ref: "#/components/schemas/Mention" },
          },
          isMilestone: {
            type: "boolean",
            description:
              "When true, this save is treated as a named version milestone.",
          },
        },
      },
      ChapterVersion: {
        type: "object",
        description: "Metadata for a chapter version snapshot.",
        properties: {
          versionId: { type: "string", format: "uuid" },
          chapterId: { type: "string", format: "uuid" },
          projectId: { type: "string", format: "uuid" },
          savedBy: {
            type: "string",
            description: "User ID who saved this version.",
          },
          isMilestone: { type: "boolean" },
          content: {
            type: "string",
            description: "Full RTF content. Only present when fetching a single version.",
          },
          createdAt: { type: "string", format: "date-time" },
        },
      },
      DetectableEntity: {
        type: "object",
        description:
          "Lightweight entity representation optimized for client-side Aho-Corasick tokenizer.",
        properties: {
          id: { type: "string", format: "uuid" },
          mainToken: {
            type: "string",
            description: "Primary name used for detection.",
          },
          aliases: {
            type: "array",
            items: { type: "string" },
            description: "Alternative names that also trigger detection.",
          },
          type: {
            type: "string",
            enum: ["Character", "Location", "Event", "Object", "Concept"],
          },
        },
      },
      CreateRelationshipBody: {
        type: "object",
        required: ["sourceEntityId", "targetEntityId", "type"],
        properties: {
          sourceEntityId: { type: "string", format: "uuid" },
          targetEntityId: { type: "string", format: "uuid" },
          type: {
            type: "string",
            description: "Relationship type label (e.g. 'KNOWS', 'LOCATED_IN').",
          },
        },
      },
      DeleteRelationshipBody: {
        type: "object",
        required: ["sourceEntityId", "targetEntityId"],
        properties: {
          sourceEntityId: { type: "string", format: "uuid" },
          targetEntityId: { type: "string", format: "uuid" },
        },
      },
      CreateWikiEntryBody: {
        type: "object",
        required: ["name", "entityType"],
        properties: {
          name: { type: "string", maxLength: 200 },
          entityType: {
            type: "string",
            enum: ["Character", "Location", "Event", "Object", "Concept"],
          },
          description: { type: "string" },
          tags: { type: "array", items: { type: "string" } },
          aliases: { type: "array", items: { type: "string" } },
        },
      },
    },
  },
  security: [{ BearerAuth: [] }],
  paths: {
    "/api/manuscripts/{projectId}": {
      get: {
        tags: ["Manuscripts"],
        summary: "List all manuscripts for a project",
        description:
          "Returns all manuscripts belonging to `projectId` as index objects. " +
          "Chapter content is omitted; only metadata is included.",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
            description: "UUID of the project.",
          },
        ],
        responses: {
          "200": {
            description: "Array of manuscripts ordered by `order` ascending.",
            content: {
              "application/json": {
                schema: {
                  type: "array",
                  items: { $ref: "#/components/schemas/Manuscript" },
                },
              },
            },
          },
          "401": {
            description: "Missing or invalid JWT.",
            content: {
              "application/json": {
                schema: { $ref: "#/components/schemas/ErrorResponse" },
              },
            },
          },
          "403": {
            description: "Caller is not a member of the project.",
            content: {
              "application/json": {
                schema: { $ref: "#/components/schemas/ErrorResponse" },
              },
            },
          },
        },
      },
      post: {
        tags: ["Manuscripts"],
        summary: "Create a new manuscript",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
        ],
        requestBody: {
          required: true,
          content: {
            "application/json": {
              schema: { $ref: "#/components/schemas/CreateManuscriptBody" },
            },
          },
        },
        responses: {
          "201": {
            description: "Manuscript created.",
            content: {
              "application/json": {
                schema: { $ref: "#/components/schemas/Manuscript" },
              },
            },
          },
          "400": {
            description: "`title` is missing.",
            content: {
              "application/json": {
                schema: { $ref: "#/components/schemas/ErrorResponse" },
              },
            },
          },
          "401": { description: "Missing or invalid JWT." },
          "403": { description: "Caller is not a member of the project." },
        },
      },
    },
    "/api/manuscripts/{projectId}/{manuscriptId}": {
      get: {
        tags: ["Manuscripts"],
        summary: "Get a single manuscript (with chapter index)",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
          {
            name: "manuscriptId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
        ],
        responses: {
          "200": {
            description: "Manuscript with chapter index.",
            content: {
              "application/json": {
                schema: { $ref: "#/components/schemas/Manuscript" },
              },
            },
          },
          "404": {
            description: "Manuscript not found.",
            content: {
              "application/json": {
                schema: { $ref: "#/components/schemas/ErrorResponse" },
              },
            },
          },
        },
      },
      put: {
        tags: ["Manuscripts"],
        summary: "Update manuscript title or order",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
          {
            name: "manuscriptId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
        ],
        requestBody: {
          required: true,
          content: {
            "application/json": {
              schema: { $ref: "#/components/schemas/UpdateManuscriptBody" },
            },
          },
        },
        responses: {
          "200": {
            description: "Updated manuscript.",
            content: {
              "application/json": {
                schema: { $ref: "#/components/schemas/Manuscript" },
              },
            },
          },
          "404": { description: "Manuscript not found." },
        },
      },
      delete: {
        tags: ["Manuscripts"],
        summary: "Delete a manuscript and all its chapters",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
          {
            name: "manuscriptId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
        ],
        responses: {
          "204": { description: "Manuscript deleted." },
          "404": { description: "Manuscript not found." },
        },
      },
    },
    "/api/manuscripts/{projectId}/{manuscriptId}/chapters": {
      post: {
        tags: ["Chapters"],
        summary: "Create a new chapter",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
          {
            name: "manuscriptId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
        ],
        requestBody: {
          required: true,
          content: {
            "application/json": {
              schema: { $ref: "#/components/schemas/CreateChapterBody" },
            },
          },
        },
        responses: {
          "201": {
            description: "Chapter created.",
            content: {
              "application/json": {
                schema: { $ref: "#/components/schemas/Chapter" },
              },
            },
          },
          "400": { description: "`title` is missing." },
          "404": { description: "Manuscript not found." },
        },
      },
    },
    "/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}": {
      get: {
        tags: ["Chapters"],
        summary: "Get full chapter content",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
          {
            name: "manuscriptId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
          {
            name: "chapterId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
        ],
        responses: {
          "200": {
            description: "Chapter with full RTF content.",
            content: {
              "application/json": {
                schema: { $ref: "#/components/schemas/Chapter" },
              },
            },
          },
          "404": { description: "Chapter not found." },
        },
      },
      put: {
        tags: ["Chapters"],
        summary: "Update a chapter (Last-Write-Wins)",
        description:
          "Updates `title`, `content`, and/or `order`. " +
          "If `clientTimestamp` is supplied and is older than the server's `updatedAt`, " +
          "the request is rejected with **409 Conflict** so the client can surface a merge UI.",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
          {
            name: "manuscriptId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
          {
            name: "chapterId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
        ],
        requestBody: {
          required: true,
          content: {
            "application/json": {
              schema: { $ref: "#/components/schemas/UpdateChapterBody" },
            },
          },
        },
        responses: {
          "200": {
            description: "Updated chapter.",
            content: {
              "application/json": {
                schema: { $ref: "#/components/schemas/Chapter" },
              },
            },
          },
          "404": { description: "Chapter not found." },
          "409": {
            description:
              "Write rejected — client state is stale (LWW conflict).",
            content: {
              "application/json": {
                schema: { $ref: "#/components/schemas/ConflictResponse" },
              },
            },
          },
        },
      },
      delete: {
        tags: ["Chapters"],
        summary: "Delete a chapter",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
          {
            name: "manuscriptId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
          {
            name: "chapterId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
        ],
        responses: {
          "204": { description: "Chapter deleted." },
          "404": { description: "Chapter not found." },
        },
      },
    },
    "/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}/mentions":
      {
        get: {
          tags: ["Chapters"],
          summary: "Get wiki mentions for a chapter",
          description:
            "Returns the list of wiki entities detected in the chapter's text.",
          parameters: [
            {
              name: "projectId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
            {
              name: "manuscriptId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
            {
              name: "chapterId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
          ],
          responses: {
            "200": {
              description: "Array of mentions.",
              content: {
                "application/json": {
                  schema: {
                    type: "array",
                    items: { $ref: "#/components/schemas/Mention" },
                  },
                },
              },
            },
            "404": { description: "Chapter not found." },
          },
        },
      },
    "/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}/autosave":
      {
        put: {
          tags: ["Chapters"],
          summary: "Autosave chapter content with mentions",
          description:
            "Handles client debounced autosaves. Persists content and locally-detected " +
            "wiki entity mentions. When `isMilestone` is true, a named version snapshot is created.",
          parameters: [
            {
              name: "projectId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
            {
              name: "manuscriptId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
            {
              name: "chapterId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
          ],
          requestBody: {
            required: true,
            content: {
              "application/json": {
                schema: { $ref: "#/components/schemas/AutosaveChapterBody" },
              },
            },
          },
          responses: {
            "200": { description: "Autosave persisted." },
            "401": { description: "Missing or invalid JWT." },
            "403": { description: "Caller does not have write access." },
            "404": { description: "Chapter not found." },
          },
        },
      },
    "/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}/versions":
      {
        get: {
          tags: ["Chapters"],
          summary: "List chapter version history",
          description:
            "Returns version history metadata for a chapter, ordered by creation date descending.",
          parameters: [
            {
              name: "projectId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
            {
              name: "manuscriptId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
            {
              name: "chapterId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
          ],
          responses: {
            "200": {
              description: "Array of version metadata.",
              content: {
                "application/json": {
                  schema: {
                    type: "array",
                    items: { $ref: "#/components/schemas/ChapterVersion" },
                  },
                },
              },
            },
          },
        },
      },
    "/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}/versions/{versionId}":
      {
        get: {
          tags: ["Chapters"],
          summary: "Get a specific chapter version",
          description: "Returns a single version snapshot including full content.",
          parameters: [
            {
              name: "projectId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
            {
              name: "manuscriptId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
            {
              name: "chapterId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
            {
              name: "versionId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
          ],
          responses: {
            "200": {
              description: "Version with full content.",
              content: {
                "application/json": {
                  schema: { $ref: "#/components/schemas/ChapterVersion" },
                },
              },
            },
            "404": { description: "Version not found." },
          },
        },
      },
    "/api/manuscripts/{projectId}/{manuscriptId}/chapters/{chapterId}/versions/{versionId}/restore":
      {
        put: {
          tags: ["Chapters"],
          summary: "Restore chapter to a specific version",
          description:
            "Replaces the current chapter content with the content from the specified version. " +
            "Creates a new version snapshot before restoring.",
          parameters: [
            {
              name: "projectId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
            {
              name: "manuscriptId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
            {
              name: "chapterId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
            {
              name: "versionId",
              in: "path",
              required: true,
              schema: { type: "string", format: "uuid" },
            },
          ],
          responses: {
            "200": {
              description: "Restored chapter.",
              content: {
                "application/json": {
                  schema: { $ref: "#/components/schemas/Chapter" },
                },
              },
            },
            "401": { description: "Missing or invalid JWT." },
            "403": { description: "Caller does not have write access." },
            "404": { description: "Version or chapter not found." },
          },
        },
      },
    "/api/wiki/{projectId}/entries": {
      get: {
        tags: ["Wiki"],
        summary: "List all wiki entries for a project",
        description:
          "Returns all wiki entries. Accepts an optional `?type=` query parameter to filter by entity type.",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
          {
            name: "type",
            in: "query",
            required: false,
            schema: {
              type: "string",
              enum: ["Character", "Location", "Event", "Object", "Concept"],
            },
            description: "Filter entries by entity type.",
          },
        ],
        responses: {
          "200": {
            description: "Array of wiki entries.",
            content: {
              "application/json": {
                schema: {
                  type: "array",
                  items: { $ref: "#/components/schemas/WikiEntry" },
                },
              },
            },
          },
        },
      },
      post: {
        tags: ["Wiki"],
        summary: "Create a wiki entry",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
        ],
        requestBody: {
          required: true,
          content: {
            "application/json": {
              schema: { $ref: "#/components/schemas/WikiEntry" },
            },
          },
        },
        responses: {
          "201": {
            description: "Entry created.",
            content: {
              "application/json": {
                schema: { $ref: "#/components/schemas/WikiEntry" },
              },
            },
          },
          "400": { description: "Validation error." },
        },
      },
    },
    "/api/wiki/{projectId}/entries/{entityId}": {
      get: {
        tags: ["Wiki"],
        summary: "Get a wiki entry",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
          {
            name: "entityId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
        ],
        responses: {
          "200": {
            description: "Wiki entry.",
            content: {
              "application/json": {
                schema: { $ref: "#/components/schemas/WikiEntry" },
              },
            },
          },
          "404": { description: "Entry not found." },
        },
      },
      put: {
        tags: ["Wiki"],
        summary: "Update a wiki entry",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
          {
            name: "entityId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
        ],
        requestBody: {
          required: true,
          content: {
            "application/json": {
              schema: { $ref: "#/components/schemas/WikiEntry" },
            },
          },
        },
        responses: {
          "200": { description: "Updated entry." },
          "404": { description: "Entry not found." },
        },
      },
      delete: {
        tags: ["Wiki"],
        summary: "Delete a wiki entry",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
          {
            name: "entityId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
        ],
        responses: {
          "204": { description: "Entry deleted." },
          "404": { description: "Entry not found." },
        },
      },
    },
    "/api/wiki/{projectId}/entries/{entityId}/appearances": {
      get: {
        tags: ["Wiki"],
        summary: "Get chapters where an entity appears",
        description:
          "Returns all chapters that mention this wiki entity, based on APPEARS_IN " +
          "edges in the narrative graph. Useful for building character arc views.",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
          {
            name: "entityId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
        ],
        responses: {
          "200": {
            description: "Array of appearance records.",
            content: {
              "application/json": {
                schema: {
                  type: "array",
                  items: { $ref: "#/components/schemas/AppearanceRecord" },
                },
              },
            },
          },
        },
      },
    },
    "/api/wiki/{projectId}/detectable": {
      get: {
        tags: ["Wiki"],
        summary: "Get detectable entities for client-side tokenizer",
        description:
          "Returns all wiki entities in a format optimized for the client-side " +
          "Aho-Corasick tokenizer. Each entry includes the main name and aliases.",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
        ],
        responses: {
          "200": {
            description: "Array of detectable entities.",
            content: {
              "application/json": {
                schema: {
                  type: "array",
                  items: { $ref: "#/components/schemas/DetectableEntity" },
                },
              },
            },
          },
        },
      },
    },
    "/api/graph/{projectId}": {
      get: {
        tags: ["Graph"],
        summary: "Get full narrative graph",
        description:
          "Returns the full entity graph for a project: nodes and directed edges. " +
          "Accepts an optional `?type=` query parameter to filter nodes by entity type.",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
          {
            name: "type",
            in: "query",
            required: false,
            schema: {
              type: "string",
              enum: ["Character", "Location", "Event", "Object", "Concept"],
            },
            description: "Filter graph nodes by entity type.",
          },
        ],
        responses: {
          "200": {
            description: "Graph with nodes and edges.",
            content: {
              "application/json": {
                schema: {
                  type: "object",
                  properties: {
                    nodes: { type: "array", items: { $ref: "#/components/schemas/WikiEntry" } },
                    edges: { type: "array", items: { type: "object" } },
                  },
                },
              },
            },
          },
          "400": { description: "Invalid entity type filter." },
        },
      },
    },
    "/api/graph/{projectId}/relationships": {
      post: {
        tags: ["Graph"],
        summary: "Create a relationship between entities",
        description:
          "Creates a directed relationship between two wiki entities in the narrative graph.",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
        ],
        requestBody: {
          required: true,
          content: {
            "application/json": {
              schema: { $ref: "#/components/schemas/CreateRelationshipBody" },
            },
          },
        },
        responses: {
          "201": { description: "Relationship created." },
          "400": { description: "Validation error." },
          "404": { description: "One or both entities do not exist in this project." },
        },
      },
      delete: {
        tags: ["Graph"],
        summary: "Delete relationships between entities",
        description:
          "Deletes all directed relationships between two wiki entities.",
        parameters: [
          {
            name: "projectId",
            in: "path",
            required: true,
            schema: { type: "string", format: "uuid" },
          },
        ],
        requestBody: {
          required: true,
          content: {
            "application/json": {
              schema: { $ref: "#/components/schemas/DeleteRelationshipBody" },
            },
          },
        },
        responses: {
          "204": { description: "Relationships deleted." },
          "400": { description: "Validation error." },
        },
      },
    },
  },
};

export const swaggerSpec = swaggerJsdoc({ definition, apis: [] });
