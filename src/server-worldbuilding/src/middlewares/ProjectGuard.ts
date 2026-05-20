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
      // Check membership via :MEMBER_OF edge OR ownership via ownerId
      // property so the guard works even before the full role-sync pipeline
      // is in place.
      const result = await session.run(
        `MATCH (p:Project { projectId: $projectId })
         OPTIONAL MATCH (u:User { id: $userId })-[r:MEMBER_OF]->(p)
         WITH p, r
         WHERE p.ownerId = $userId OR r IS NOT NULL
         RETURN p.ownerId = $userId AS isOwner, r.role AS role LIMIT 1`,
        { projectId, userId: req.user.id },
      );

      if (result.records.length === 0) {
        res.status(403).json({ error: "Access denied to this project" });
        return;
      }

      const record = result.records[0];
      const isOwner = record.get("isOwner") as boolean;
      const role = record.get("role") as string | null;

      req.projectRole = isOwner ? "OWNER" : (role || "READER");

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

/**
 * Middleware that blocks write requests (POST, PUT, DELETE) if the user's role
 * on the project is READER.
 */
export const requireWriteAccess = () => {
  return (
    req: InterfaceAuthRequest,
    res: Response,
    next: NextFunction,
  ): void => {
    if (!req.projectRole || req.projectRole === "READER") {
      res.status(403).json({ error: "Forbidden: Write access required" });
      return;
    }
    next();
  };
};
