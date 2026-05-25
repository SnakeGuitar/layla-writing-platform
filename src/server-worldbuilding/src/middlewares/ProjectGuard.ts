import type { Response, NextFunction } from "express";
import type InterfaceAuthRequest from "@/interfaces/auth/AuthRequest";
import { getNeo4jDriver } from "@/db/neo4j";
import { config } from "@/config/env";

// ── Neo4j lazy-sync helpers ──────────────────────────────────────────────────

/**
 * Creates or updates a `MEMBER_OF` relationship in Neo4j for the given user
 * and project. Called lazily when the fallback path confirms membership via
 * server-core so that subsequent requests hit Neo4j directly.
 */
async function upsertMemberOfEdge(
  projectId: string,
  userId: string,
  role: string,
): Promise<void> {
  const driver = getNeo4jDriver();
  const session = driver.session();
  try {
    await session.executeWrite(async (tx) => {
      await tx.run(`MERGE (p:Project { projectId: $projectId })`, { projectId });
      await tx.run(
        `MERGE (u:User { id: $userId })
         WITH u
         MATCH (p:Project { projectId: $projectId })
         MERGE (u)-[r:MEMBER_OF]->(p)
         SET r.role = $role`,
        { userId, projectId, role: role.toUpperCase() },
      );
    });
  } catch (err) {
    // Best-effort — failure here only affects the cache, not the current request.
    console.warn("[ProjectGuard] Failed to upsert MEMBER_OF edge:", err);
  } finally {
    await session.close();
  }
}

/**
 * Calls the server-core collaborator endpoint to resolve the user's real role
 * for the given project. Returns `null` if the call fails or the user has no
 * role in the project.
 *
 * Used as a fallback when Neo4j has no record (e.g. after a cold restart that
 * wiped Neo4j state while SQL Server retained the authoritative data).
 */
async function fetchRoleFromCore(
  projectId: string,
  userId: string,
  bearerToken: string,
): Promise<string | null> {
  if (!config.coreApiUrl) return null;

  try {
    const url = `${config.coreApiUrl}/api/projects/${projectId}/collaborators`;
    const resp = await fetch(url, {
      headers: { Authorization: `Bearer ${bearerToken}` },
      signal: AbortSignal.timeout(4000),
    });

    if (!resp.ok) return null;

    const collaborators = (await resp.json()) as Array<{
      userId: string;
      role: string;
    }>;

    const mine = collaborators.find(
      (c) => c.userId.toLowerCase() === userId.toLowerCase(),
    );
    return mine?.role ?? null;
  } catch (err) {
    console.warn("[ProjectGuard] Core API fallback failed:", err);
    return null;
  }
}

// ── Middleware factory ────────────────────────────────────────────────────────

/**
 * Middleware factory that enforces per-project access control.
 *
 * Primary check: queries the Neo4j `:Project` node for a `MEMBER_OF` edge or
 * `ownerId` match.
 *
 * Fallback (Neo4j has no record): calls the server-core collaborator API using
 * the request's Bearer token. On success the edge is upserted in Neo4j so
 * subsequent requests are served from the graph cache.
 *
 * Returns **403 Forbidden** if neither check confirms access.
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
    let projectId = (req.params as { projectId?: string }).projectId;

    if (!projectId) {
      const fullPath = req.originalUrl || req.url || "";
      const match = fullPath.match(/\/(manuscripts|wiki|graph)\/([^/]+)/);
      if (match && match[2]) {
        projectId = decodeURIComponent(match[2].split("?")[0]);
      }
    }

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

    let neo4jRole: string | null = null;
    let neo4jFound = false;

    try {
      // Check membership via :MEMBER_OF edge OR ownership via ownerId property.
      const result = await session.run(
        `MATCH (p:Project { projectId: $projectId })
         OPTIONAL MATCH (u:User { id: $userId })-[r:MEMBER_OF]->(p)
         WITH p, r
         WHERE p.ownerId = $userId OR r IS NOT NULL
         RETURN p.ownerId = $userId AS isOwner, r.role AS role LIMIT 1`,
        { projectId, userId: req.user.id },
      );

      if (result.records.length > 0) {
        neo4jFound = true;
        const record = result.records[0];
        const isOwner = record.get("isOwner") as boolean;
        neo4jRole = isOwner ? "OWNER" : ((record.get("role") as string | null) ?? "READER");
      }
    } catch (err) {
      console.error("[ProjectGuard] Neo4j query failed:", err);
      // Don't return 503 immediately — attempt the server-core fallback first.
    } finally {
      await session.close();
    }

    // Trust Neo4j immediately for OWNER and EDITOR.
    // For READER we fall through to the server-core verification: the role may
    // have been upgraded to EDITOR in SQL Server while Neo4j still holds the
    // old value (event dropped during cold-start or container restart race).
    if (neo4jFound && neo4jRole && neo4jRole !== "READER") {
      req.projectRole = neo4jRole;
      next();
      return;
    }

    // ── Fallback: Neo4j has no record OR shows READER — verify with server-core ──
    const authHeader = req.headers.authorization ?? "";
    const bearerToken = authHeader.startsWith("Bearer ") ? authHeader.slice(7) : "";

    if (!bearerToken) {
      res.status(403).json({ error: "Access denied to this project" });
      return;
    }

    const coreRole = await fetchRoleFromCore(projectId, req.user.id, bearerToken);

    if (!coreRole) {
      res.status(403).json({ error: "Access denied to this project" });
      return;
    }

    // Upsert Neo4j lazily so the next request is served from the graph cache.
    void upsertMemberOfEdge(projectId, req.user.id, coreRole);

    req.projectRole = coreRole.toUpperCase();
    next();
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
