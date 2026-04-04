import type { Response, NextFunction } from "express";
import type InterfaceAuthRequest from "@/interfaces/auth/AuthRequest";
import { getNeo4jDriver } from "@/db/neo4j";

/**
 * Middleware factory that enforces per-project access control.
 *
 * Queries the Neo4j `:Project` node for access.
 * Returns **403 Forbidden** if the project does not exist or access is denied.
 *
 * Must be used **after** {@link MiddlewareAuthenticate} so that
 * `req.user` is already populated.
 *
 * @example
 * router.get("/:projectId", MiddlewareAuthenticate, requireProjectAccess(), handler);
 */
export const requireProjectAccess = () => {
  return async (
    req: InterfaceAuthRequest,
    res: Response,
    next: NextFunction,
  ): Promise<void> => {
    const { projectId } = req.params as { projectId?: string };

    if (!projectId) {
      next();
      return;
    }

    if (!req.user) {
      res.status(401).json({ error: "Unauthorized" });
      return;
    }

    const driver = getNeo4jDriver();
    const session = driver.session();

    try {
      // TODO: Alignment with SQL ProjectRole system. 
      // Current check only verifies ownership in Neo4j.
      const result = await session.run(
        `MATCH (u:User { id: $userId })-[:MEMBER_OF]->(p:Project { projectId: $projectId }) RETURN p LIMIT 1`,
        { projectId, userId: req.user.id },
      );

      if (result.records.length === 0) {
        res.status(403).json({ error: "Access denied to this project" });
        return;
      }

      next();
    } catch (err) {
      console.error("[ProjectGuard] Neo4j query failed:", err);
      res.status(503).json({ error: "Service temporarily unavailable" });
      return;
    } finally {
      await session.close();
    }
  };
};
