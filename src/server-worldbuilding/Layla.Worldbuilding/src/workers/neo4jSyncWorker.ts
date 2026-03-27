import type { IWikiEntry } from "@/interfaces/wiki/IWikiEntry";
import { MongooseWikiEntryRepository } from "@/repositories/MongooseWikiEntryRepository";
import { Neo4jGraphRepository } from "@/repositories/Neo4jGraphRepository";

const wikiRepo = new MongooseWikiEntryRepository();
const graphRepo = new Neo4jGraphRepository();

/** Interval between sync attempts, in milliseconds. */
const SYNC_INTERVAL_MS = 60_000;

/**
 * Attempts to MERGE a single {@link IWikiEntry} into Neo4j and,
 * on success, marks it as synced in MongoDB.
 */
const retrySyncEntry = async (entry: IWikiEntry): Promise<void> => {
  try {
    await graphRepo.mergeEntity({
      entityId: entry.entityId,
      projectId: entry.projectId,
      name: entry.name,
      entityType: entry.entityType,
    });

    await wikiRepo.updateEntry(entry.entityId, { neo4jSynced: true });

    console.log(`[neo4jSyncWorker] Synced entry ${entry.entityId}`);
  } catch (err) {
    console.error(
      `[neo4jSyncWorker] Failed to sync entry ${entry.entityId}:`,
      err,
    );
  }
};

/**
 * Polls MongoDB every {@link SYNC_INTERVAL_MS} milliseconds for
 * {@link WikiEntry} documents where `neo4jSynced === false` and retries
 * the Neo4j MERGE for each one.
 */
export const startNeo4jSyncWorker = (): void => {
  const tick = async (): Promise<void> => {
    try {
      const unsyncedEntries = await wikiRepo.findEntriesToSync();

      if (unsyncedEntries.length === 0) return;

      console.log(
        `[neo4jSyncWorker] Retrying ${unsyncedEntries.length} unsynced entries...`,
      );

      await Promise.allSettled(unsyncedEntries.map(retrySyncEntry));
    } catch (err) {
      console.error("[neo4jSyncWorker] Tick error:", err);
    }
  };

  setInterval(() => void tick(), SYNC_INTERVAL_MS);
  console.log(
    `[neo4jSyncWorker] Started (interval: ${SYNC_INTERVAL_MS / 1000}s)`,
  );
};
