import { v4 as uuidv4 } from "uuid";
import type { IWikiEntry, WikiEntityType } from "@/interfaces/wiki/IWikiEntry";
import { container } from "./container";

/**
 * Lists wiki entries for a project, optionally filtered by entity type.
 */
export const listEntries = async (
  projectId: string,
  entityType?: WikiEntityType,
  repo = container.wikiRepo,
) => {
  return repo.listEntries(projectId, entityType);
};

/**
 * Returns a single wiki entry by its `entityId`, scoped to `projectId` to
 * prevent cross-project access (returns `null` if the entry belongs to a
 * different project).
 */
export const getEntry = async (
  entityId: string,
  projectId?: string,
  repo = container.wikiRepo,
) => {
  return repo.getEntry(entityId, projectId);
};

/**
 * Creates a new wiki entry in MongoDB and attempts an immediate Neo4j sync.
 */
export const createEntry = async (
  data: {
    projectId: string;
    name: string;
    entityType: WikiEntityType;
    description?: string;
    tags?: string[];
  },
  repo = container,
) => {
  const entryData: Partial<IWikiEntry> = {
    projectId: data.projectId,
    entityId: uuidv4(),
    name: data.name,
    entityType: data.entityType,
    description: data.description ?? "",
    tags: data.tags ?? [],
    neo4jSynced: false,
  };

  const entry = await repo.wikiRepo.createEntry(entryData);

  try {
    await repo.graphRepo.mergeEntity({
      entityId: entry.entityId,
      projectId: entry.projectId,
      name: entry.name,
      entityType: entry.entityType,
    });

    await repo.wikiRepo.updateEntry(entry.entityId, { neo4jSynced: true });
    entry.neo4jSynced = true;
  } catch (err) {
    console.warn(
      `[WikiEntry.service] Neo4j sync failed for entry ${entry.entityId}; will retry.`,
      err,
    );
  }

  return entry;
};

/**
 * Updates mutable fields of an existing wiki entry in MongoDB and re-syncs
 * to Neo4j if the entry exists. Pass `projectId` to scope the update to a
 * specific project — prevents cross-project mutations.
 */
export const updateEntry = async (
  entityId: string,
  data: Partial<
    Pick<IWikiEntry, "name" | "entityType" | "description" | "tags">
  >,
  projectId?: string,
  repo = container,
) => {
  const entry = await repo.wikiRepo.updateEntry(entityId, data, projectId);

  if (entry) {
    try {
      await repo.graphRepo.mergeEntity({
        entityId: entry.entityId,
        projectId: entry.projectId,
        name: entry.name,
        entityType: entry.entityType,
      });

      if (!entry.neo4jSynced) {
        // Internal call — no projectId filter needed (entry already resolved)
        await repo.wikiRepo.updateEntry(entityId, { neo4jSynced: true });
        entry.neo4jSynced = true;
      }
    } catch (err) {
      console.warn(
        `[WikiEntry.service] Neo4j sync failed on update for ${entityId}.`,
        err,
      );
    }
  }

  return entry;
};

/**
 * Deletes a wiki entry from MongoDB and removes its Neo4j node with retry.
 *
 * Because MongoDB and Neo4j are not in a shared transaction, we retry the
 * Neo4j deletion up to {@link NEO4J_DELETE_RETRIES} times with exponential
 * back-off to minimize the risk of orphaned graph nodes.
 */
const NEO4J_DELETE_RETRIES = 3;

export const deleteEntry = async (
  entityId: string,
  projectId?: string,
  repo = container,
): Promise<boolean> => {
  const deleted = await repo.wikiRepo.deleteEntry(entityId, projectId);
  if (!deleted) return false;

  for (let attempt = 1; attempt <= NEO4J_DELETE_RETRIES; attempt++) {
    try {
      await repo.graphRepo.deleteEntity(entityId);
      return true;
    } catch (err) {
      console.error(
        `[WikiEntry.service] Neo4j delete attempt ${attempt}/${NEO4J_DELETE_RETRIES} failed for entity ${entityId}.`,
        err,
      );
      if (attempt < NEO4J_DELETE_RETRIES) {
        await new Promise((r) => setTimeout(r, 500 * attempt));
      }
    }
  }

  console.error(
    `[WikiEntry.service] All Neo4j delete retries exhausted for entity ${entityId}. Orphaned node may remain.`,
  );
  return true;
};
