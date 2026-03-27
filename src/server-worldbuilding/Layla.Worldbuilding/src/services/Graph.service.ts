import type { IGraphResult } from "@/interfaces/graph/IGraphResult";
import type { IAppearanceRecord } from "@/interfaces/repositories/IGraphRepository";
import { Neo4jGraphRepository } from "@/repositories/Neo4jGraphRepository";

const graphRepo = new Neo4jGraphRepository();

/**
 * Returns the entity graph for a project as a list of nodes and directed edges.
 */
export const getGraph = async (
  projectId: string,
  entityType?: string,
): Promise<IGraphResult> => {
  return graphRepo.getGraph(projectId, entityType);
};

/**
 * Creates a directed `:RELATED_TO` relationship between two entity nodes.
 */
export const createRelationship = async (data: {
  projectId: string;
  sourceEntityId: string;
  targetEntityId: string;
  type: string;
  label?: string;
}): Promise<void> => {
  return graphRepo.createRelationship(data);
};

/**
 * Deletes all directed relationships between two entity nodes within a project.
 */
export const deleteRelationship = async (data: {
  projectId: string;
  sourceEntityId: string;
  targetEntityId: string;
}): Promise<void> => {
  return graphRepo.deleteRelationship(data);
};

/**
 * Returns all chapters where a given entity appears via APPEARS_IN edges.
 */
export const getEntityAppearances = async (
  projectId: string,
  entityId: string,
): Promise<IAppearanceRecord[]> => {
  return graphRepo.getEntityAppearances({ projectId, entityId });
};
