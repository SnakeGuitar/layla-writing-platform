import type {
  IWikiEntry,
  IWikiEntryNoDescription,
  WikiEntityType,
} from "@/interfaces/wiki/IWikiEntry";

export interface IWikiEntryRepository {
  listEntries(
    projectId: string,
    entityType?: WikiEntityType,
  ): Promise<IWikiEntryNoDescription[]>;

  getEntry(entityId: string): Promise<IWikiEntry | null>;

  createEntry(data: Partial<IWikiEntry>): Promise<IWikiEntry>;

  updateEntry(
    entityId: string,
    data: Partial<IWikiEntry>,
  ): Promise<IWikiEntry | null>;

  deleteEntry(entityId: string): Promise<boolean>;

  findEntriesToSync(): Promise<IWikiEntry[]>;
}
