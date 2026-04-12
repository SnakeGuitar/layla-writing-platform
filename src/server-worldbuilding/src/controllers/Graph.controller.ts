import type { Request, Response } from "express";
import * as GraphService from "@/services/Graph.service";
import {
	CreateRelationshipSchema,
	DeleteRelationshipSchema,
	entityTypeSchema,
	validate,
} from "@/validation";

/**
 * GET /api/graph/:projectId
 *
 * Returns the full entity graph for a project: nodes and directed edges.
 * Accepts an optional `?type=` query parameter to filter nodes by entity type.
 * Returns 400 when `type` is present but is not a known entity type.
 */
export const getGraph = async (req: Request, res: Response): Promise<void> => {
	const rawType = req.query["type"];
	let entityType: string | undefined;

	if (rawType !== undefined) {
		const parsed = entityTypeSchema.safeParse(rawType);
		if (!parsed.success) {
			res.status(400).json({
				error: `Invalid entity type. Allowed: ${entityTypeSchema.options.join(", ")}`,
			});
			return;
		}
		entityType = parsed.data;
	}

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
// TODO-Desarrollo: Definir cuerpos de req y res
export const createRelationship = async (
	req: Request,
	res: Response,
): Promise<void> => {
	try {
		const parsed = validate(CreateRelationshipSchema, req.body);
		if (!parsed.success) {
			res.status(400).json({ error: parsed.error });
			return;
		}

		const created = await GraphService.createRelationship({
			projectId: req.params["projectId"] as string,
			...parsed.data,
		});

		if (!created) {
			res.status(404).json({
				error: "One or both entities do not exist in this project",
			});
			return;
		}

		res.status(201).json({ message: "Relationship created" });
	} catch {
		res.status(500).json({ error: "Internal server error" });
	}
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
