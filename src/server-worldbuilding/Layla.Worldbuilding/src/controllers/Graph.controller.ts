import type { Request, Response } from "express";
import * as GraphService from "@/services/Graph.service";

/**
 * GET /api/graph/:projectId
 *
 * Returns the full entity graph for a project: nodes and directed edges.
 * Accepts an optional `?type=` query parameter to filter nodes by entity type.
 */
export const getGraph = async (req: Request, res: Response): Promise<void> => {
  const entityType = req.query["type"] as string | undefined;
  const graph = await GraphService.getGraph(
    req.params["projectId"] as string,
    entityType,
  );
  res.json(graph);
};

/**
 * POST /api/graph/:projectId/relationships
 *
 * Creates a directed relationship between two entities.
 * Requires `sourceEntityId`, `targetEntityId`, and `type` in the request body.
 *
 * Note: all relationships are stored as `:RELATED_TO` in Neo4j;
 * the `type` and optional `label` fields are stored as properties on the edge.
 */
export const createRelationship = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const { sourceEntityId, targetEntityId, type, label } = req.body as {
    sourceEntityId: string;
    targetEntityId: string;
    type: string;
    label?: string;
  };

  if (!sourceEntityId || !targetEntityId || !type) {
    res
      .status(400)
      .json({ error: "sourceEntityId, targetEntityId, and type are required" });
    return;
  }

  await GraphService.createRelationship({
    projectId: req.params["projectId"] as string,
    sourceEntityId,
    targetEntityId,
    type,
    label,
  });
  res.status(201).json({ message: "Relationship created" });
};

/**
 * DELETE /api/graph/:projectId/relationships
 *
 * Deletes all directed relationships between two entities.
 * Requires `sourceEntityId` and `targetEntityId` in the request body.
 */
export const deleteRelationship = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const { sourceEntityId, targetEntityId } = req.body as {
    sourceEntityId: string;
    targetEntityId: string;
  };

  if (!sourceEntityId || !targetEntityId) {
    res
      .status(400)
      .json({ error: "sourceEntityId and targetEntityId are required" });
    return;
  }

  await GraphService.deleteRelationship({
    projectId: req.params["projectId"] as string,
    sourceEntityId,
    targetEntityId,
  });
  res.status(204).send();
};
