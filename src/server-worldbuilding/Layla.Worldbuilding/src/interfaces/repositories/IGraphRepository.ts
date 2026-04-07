import type { IGraphResult } from "../graph/IGraphResult";

/** Describes a chapter-to-entity appearance link stored in Neo4j. */
export interface IAppearanceRecord {
  manuscriptId: string;
  manuscriptTitle: string;
  chapterId: string;
  chapterTitle: string;
}

export interface IGraphRepository {
  getGraph(projectId: string, entityType?: string): Promise<IGraphResult>;
  mergeEntity(data: {
    entityId: string;
    projectId: string;
    name: string;
    entityType: string;
  }): Promise<void>;
  deleteEntity(entityId: string): Promise<void>;
  /**
   * Creates a relationship between two entities.
   * Returns `false` if either entity does not exist in the project.
   */
  createRelationship(data: {
    projectId: string;
    sourceEntityId: string;
    targetEntityId: string;
    type: string;
    label?: string;
  }): Promise<boolean>;
  deleteRelationship(data: {
    projectId: string;
    sourceEntityId: string;
    targetEntityId: string;
  }): Promise<void>;

  /** Creates or updates an APPEARS_IN edge between an entity and a chapter node. */
  syncAppearances(data: {
    projectId: string;
    manuscriptId: string;
    manuscriptTitle: string;
    chapterId: string;
    chapterTitle: string;
    entityIds: string[];
  }): Promise<void>;

  /** Creates or updates APPEARS_IN edges for multiple entities in a single query. */
  mergeAppearancesBatch(data: {
    projectId: string;
    manuscriptId: string;
    manuscriptTitle: string;
    chapterId: string;
    chapterTitle: string;
    entityIds: string[];
  }): Promise<void>;

  /** Removes all APPEARS_IN edges for a given chapter (used before re-syncing mentions). */
  clearChapterAppearances(data: {
    projectId: string;
    chapterId: string;
  }): Promise<void>;

  /** Returns all chapters in which a given entity appears. */
  getEntityAppearances(data: {
    projectId: string;
    entityId: string;
  }): Promise<IAppearanceRecord[]>;
}
