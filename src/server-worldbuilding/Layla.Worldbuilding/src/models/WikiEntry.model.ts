import { Schema, model } from "mongoose";
import type { IWikiEntry } from "@/interfaces/wiki/IWikiEntry";

/**
 * Mongoose schema for wiki entries.
 *
 * - `entityId` is a UUID shared with the corresponding Neo4j `:Entity` node.
 * - `neo4jSynced` tracks eventual-consistency state; `false` means the Neo4j
 *   write failed and the entry is pending retry by {@link neo4jSyncWorker}.
 * - A compound index on `{ projectId, entityType }` matches the primary
 *   list-entries query pattern.
 */
const WikiEntrySchema = new Schema<IWikiEntry>(
  {
    projectId: { type: String, required: true, index: true },
    entityId: { type: String, required: true, unique: true, index: true },
    name: { type: String, required: true, maxlength: 200 },
    entityType: {
      type: String,
      enum: ["Character", "Location", "Event", "Object"],
      required: true,
    },
    description: { type: String, default: "" },
    tags: [{ type: String }],
    neo4jSynced: { type: Boolean, default: false },
  },
  { timestamps: true },
);

WikiEntrySchema.index({ projectId: 1, entityType: 1 });

export const WikiEntryModel = model<IWikiEntry>("WikiEntry", WikiEntrySchema);
