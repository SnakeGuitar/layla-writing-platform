export type WikiEntityType = "Character" | "Location" | "Event" | "Object" | "Concept";

export interface IWikiEntry {
  projectId: string;
  entityId: string;
  name: string;
  entityType: WikiEntityType;
  description: string;
  tags: string[];
  neo4jSynced: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export type IWikiEntryNoDescription = Omit<IWikiEntry, "description">;
