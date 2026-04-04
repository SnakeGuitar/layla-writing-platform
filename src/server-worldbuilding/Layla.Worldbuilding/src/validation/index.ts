import { z } from "zod";

// ─── Shared primitives ────────────────────────────────────────────────────────

const uuidParam = z.string().uuid();

export const entityTypeSchema = z.enum([
  "Character",
  "Location",
  "Event",
  "Object",
  "Concept",
]);

// ─── Manuscript ───────────────────────────────────────────────────────────────

export const CreateManuscriptSchema = z.object({
  title: z.string().min(1).max(200),
  order: z.number().int().min(0).optional(),
});

export const UpdateManuscriptSchema = z.object({
  title: z.string().min(1).max(200).optional(),
  order: z.number().int().min(0).optional(),
});

export const CreateChapterSchema = z.object({
  title: z.string().min(1).max(200),
  content: z.string().optional(),
  order: z.number().int().min(0).optional(),
});

export const UpdateChapterSchema = z.object({
  title: z.string().min(1).max(200).optional(),
  content: z.string().optional(),
  order: z.number().int().min(0).optional(),
  clientTimestamp: z.string().datetime({ offset: true }).optional(),
});

// ─── Wiki ─────────────────────────────────────────────────────────────────────

export const CreateWikiEntrySchema = z.object({
  name: z.string().min(1).max(200),
  entityType: entityTypeSchema,
  description: z.string().optional(),
  tags: z.array(z.string()).optional(),
});

export const UpdateWikiEntrySchema = z.object({
  name: z.string().min(1).max(200).optional(),
  entityType: entityTypeSchema.optional(),
  description: z.string().optional(),
  tags: z.array(z.string()).optional(),
});

// ─── Graph ────────────────────────────────────────────────────────────────────

/** Allowed Neo4j relationship types — single source of truth for the API and repository. */
export const relationshipTypeSchema = z.enum([
  "RELATED_TO",
  "ALLY_OF",
  "ENEMY_OF",
  "MEMBER_OF",
  "LOCATED_IN",
]);

export const CreateRelationshipSchema = z.object({
  sourceEntityId: uuidParam,
  targetEntityId: uuidParam,
  type: relationshipTypeSchema,
  label: z.string().optional(),
});

export const DeleteRelationshipSchema = z.object({
  sourceEntityId: uuidParam,
  targetEntityId: uuidParam,
});

// ─── Helper ───────────────────────────────────────────────────────────────────

/**
 * Parses `data` against `schema`.
 * Returns the typed result on success, or a formatted error string on failure.
 */
export function validate<T>(
  schema: z.ZodType<T>,
  data: unknown,
): { success: true; data: T } | { success: false; error: string } {
  const result = schema.safeParse(data);
  if (result.success) return { success: true, data: result.data };

  const message = result.error.issues
    .map((i) => `${i.path.join(".")}: ${i.message}`)
    .join("; ");
  return { success: false, error: message };
}
