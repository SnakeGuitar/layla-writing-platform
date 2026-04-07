import type { Request, Response } from "express";
import * as WikiService from "@/services/WikiEntry.service";
import * as GraphService from "@/services/Graph.service";
import {
  CreateWikiEntrySchema,
  UpdateWikiEntrySchema,
  entityTypeSchema,
  validate,
} from "@/validation";

/**
 * GET /api/wiki/:projectId/entries
 *
 * Lists all wiki entries for a project.
 * Accepts an optional `?type=` query parameter to filter by entity type.
 */
export const listEntries = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const typeParam = req.query["type"];
  const entityTypeParsed = entityTypeSchema.optional().safeParse(typeParam);
  if (!entityTypeParsed.success) {
    res.status(400).json({ error: "Invalid entityType filter" });
    return;
  }
  const entries = await WikiService.listEntries(
    req.params["projectId"] as string,
    entityTypeParsed.data,
  );
  res.json(entries);
};

/**
 * GET /api/wiki/:projectId/entries/:entityId
 *
 * Returns a single wiki entry by its `entityId`.
 */
export const getEntry = async (req: Request, res: Response): Promise<void> => {
  const entry = await WikiService.getEntry(
    req.params["entityId"] as string,
    req.params["projectId"] as string,
  );
  if (!entry) {
    res.status(404).json({ error: "Entity not found" });
    return;
  }
  res.json(entry);
};

/**
 * POST /api/wiki/:projectId/entries
 *
 * Creates a new wiki entry.
 */
export const createEntry = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const parsed = validate(CreateWikiEntrySchema, req.body);
  if (!parsed.success) {
    res.status(400).json({ error: parsed.error });
    return;
  }

  const entry = await WikiService.createEntry({
    projectId: req.params["projectId"] as string,
    ...parsed.data,
  });
  res.status(201).json(entry);
};

/**
 * PUT /api/wiki/:projectId/entries/:entityId
 *
 * Updates mutable fields of a wiki entry.
 */
export const updateEntry = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const parsed = validate(UpdateWikiEntrySchema, req.body);
  if (!parsed.success) {
    res.status(400).json({ error: parsed.error });
    return;
  }

  const entry = await WikiService.updateEntry(
    req.params["entityId"] as string,
    parsed.data,
    req.params["projectId"] as string,
  );
  if (!entry) {
    res.status(404).json({ error: "Entity not found" });
    return;
  }
  res.json(entry);
};

/**
 * DELETE /api/wiki/:projectId/entries/:entityId
 *
 * Deletes a wiki entry (and its Neo4j node). Returns **204 No Content** on success.
 */
export const deleteEntry = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const deleted = await WikiService.deleteEntry(
    req.params["entityId"] as string,
    req.params["projectId"] as string,
  );
  if (!deleted) {
    res.status(404).json({ error: "Entity not found" });
    return;
  }
  res.status(204).send();
};

/**
 * GET /api/wiki/:projectId/entries/:entityId/appearances
 *
 * Returns all chapters where the entity is mentioned (via APPEARS_IN edges in Neo4j).
 */
export const getEntityAppearances = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const appearances = await GraphService.getEntityAppearances(
    req.params["projectId"] as string,
    req.params["entityId"] as string,
  );
  res.json(appearances);
};
