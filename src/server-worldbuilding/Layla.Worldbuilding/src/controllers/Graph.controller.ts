import type { Request, Response } from "express";
import * as GraphService from "@/services/Graph.service";
import {
  CreateRelationshipSchema,
  DeleteRelationshipSchema,
  validate,
} from "@/validation";

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
 */
export const createRelationship = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const parsed = validate(CreateRelationshipSchema, req.body);
  if (!parsed.success) {
    res.status(400).json({ error: parsed.error });
    return;
  }

  await GraphService.createRelationship({
    projectId: req.params["projectId"] as string,
    ...parsed.data,
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
  const parsed = validate(DeleteRelationshipSchema, req.body);
  if (!parsed.success) {
    res.status(400).json({ error: parsed.error });
    return;
  }

  await GraphService.deleteRelationship({
    projectId: req.params["projectId"] as string,
    ...parsed.data,
  });
  res.status(204).send();
};
