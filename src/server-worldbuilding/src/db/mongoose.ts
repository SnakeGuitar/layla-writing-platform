import mongoose from "mongoose";
import { config } from "@/config/env";
import { ManuscriptModel } from "@/models/Manuscript.model";
import { WikiEntryModel } from "@/models/WikiEntry.model";

const MAX_RETRIES = 5;
const RETRY_DELAY_MS = 3000;

/**
 * Drops indexes that are no longer declared in the schema and creates any
 * missing ones. Required because Mongoose does not remove stale indexes
 * automatically — earlier versions of the Manuscript schema declared
 * `projectId` as a stand-alone UNIQUE index, which conflicts with the current
 * compound `{projectId, manuscriptId}` unique index and prevents creating
 * more than one manuscript per project.
 *
 * Errors are logged but never thrown — index sync failures should not bring
 * the service down at startup.
 */
const syncSchemaIndexes = async (): Promise<void> => {
	const models: Array<{ name: string; sync: () => Promise<unknown> }> = [
		{ name: "Manuscript", sync: () => ManuscriptModel.syncIndexes() },
		{ name: "WikiEntry", sync: () => WikiEntryModel.syncIndexes() },
	];

	for (const { name, sync } of models) {
		try {
			await sync();
			console.log(`[MongoDB] ${name} indexes in sync.`);
		} catch (err) {
			console.error(`[MongoDB] Failed to sync ${name} indexes:`, err);
		}
	}
};

/**
 * Connects to MongoDB using the URI from the application config.
 *
 * Retries up to {@link MAX_RETRIES} times with a {@link RETRY_DELAY_MS} ms
 * backoff between attempts. Throws if all attempts fail.
 *
 * After a successful connection, reconciles collection indexes with the
 * current Mongoose schemas so stale indexes from older versions are dropped.
 */
export const connectMongoDB = async (): Promise<void> => {
	for (let attempt = 1; attempt <= MAX_RETRIES; attempt++) {
		try {
			await mongoose.connect(config.mongo.uri, {
				serverSelectionTimeoutMS: 5000,
			});
			console.log("MongoDB connected.");
			await syncSchemaIndexes();
			return;
		} catch (error) {
			console.error(
				`MongoDB: attempt ${attempt}/${MAX_RETRIES} failed.\n`,
				error,
			);
			if (attempt === MAX_RETRIES) throw error;
			await new Promise((resolve) => setTimeout(resolve, RETRY_DELAY_MS));
		}
	}
};
