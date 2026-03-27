import type { IWikiEntry, WikiEntityType } from "../wiki/IWikiEntry";

export interface IWikiEntryRepository {
  listEntries(
    projectId: string,
    entityType?: WikiEntityType,
  ): Promise<IWikiEntry[]>;
  getEntry(entityId: string): Promise<IWikiEntry | null>;
  createEntry(data: Partial<IWikiEntry>): Promise<IWikiEntry>;
  updateEntry(
    entityId: string,
    data: Partial<IWikiEntry>,
  ): Promise<IWikiEntry | null>;
  deleteEntry(entityId: string): Promise<boolean>;
  findEntriesToSync(): Promise<any[]>; // For the sync worker
}
