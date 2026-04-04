import { WikiEntryModel } from "@/models/WikiEntry.model";
import type { IWikiEntry, WikiEntityType } from "@/interfaces/wiki/IWikiEntry";
import type { IWikiEntryRepository } from "@/interfaces/repositories/IWikiEntryRepository";

export class MongooseWikiEntryRepository implements IWikiEntryRepository {
  async listEntries(
    projectId: string,
    entityType?: WikiEntityType,
  ): Promise<Omit<IWikiEntry, "description">[]> {
    const filter: Record<string, string | number> = { projectId };
    if (entityType) filter.entityType = entityType;
    return WikiEntryModel.find(filter)
      .select("-description -__v")
      .lean() as unknown as IWikiEntry[];
  }

  async getEntry(entityId: string): Promise<IWikiEntry | null> {
    return WikiEntryModel.findOne({
      entityId,
    }).lean() as unknown as IWikiEntry | null;
  }

  async createEntry(data: Partial<IWikiEntry>): Promise<IWikiEntry> {
    const entry = await WikiEntryModel.create(data);
    return entry.toObject() as unknown as IWikiEntry;
  }

  async updateEntry(
    entityId: string,
    data: Partial<IWikiEntry>,
  ): Promise<IWikiEntry | null> {
    return WikiEntryModel.findOneAndUpdate(
      { entityId },
      { $set: data },
      { new: true },
    ).lean() as unknown as IWikiEntry | null;
  }

  async deleteEntry(entityId: string): Promise<boolean> {
    const result = await WikiEntryModel.deleteOne({ entityId });
    return result.deletedCount > 0;
  }

  async findEntriesToSync(): Promise<IWikiEntry[]> {
    return WikiEntryModel.find({ neo4jSynced: false })
      .limit(50)
      .lean() as unknown as IWikiEntry[];
  }
}
