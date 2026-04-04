import { getNeo4jDriver } from "@/db/neo4j";
import type {
  IGraphResult,
  GraphNode,
  GraphEdge,
} from "@/interfaces/graph/IGraphResult";
import type {
  IGraphRepository,
  IAppearanceRecord,
} from "@/interfaces/repositories/IGraphRepository";

/**
 * Extract props from node secure
 */
const nodeProps = (
  node: { properties: unknown } | null,
): Record<string, unknown> =>
  node && typeof node.properties === "object" && node.properties !== null
    ? (node.properties as Record<string, unknown>)
    : {};

const str = (v: unknown): string =>
  typeof v === "string" ? v : String(v ?? "");

export class Neo4jGraphRepository implements IGraphRepository {
  private pendingAppearances = new Map<
    string,
    {
      meta: Omit<
        Parameters<Neo4jGraphRepository["syncAppearances"]>[0],
        "entityIds"
      >;
      entityIds: Set<string>;
    }
  >();

  async getGraph(
    projectId: string,
    entityType?: string,
  ): Promise<IGraphResult> {
    const session = getNeo4jDriver().session();

    try {
      const result = await session.run(
        `MATCH (e:Entity { projectId: $projectId })
         WHERE $entityType IS NULL OR e.entityType = $entityType
         OPTIONAL MATCH (e)-[r]->(t:Entity { projectId: $projectId })
         RETURN e, r, t`,
        { projectId, entityType: entityType ?? null },
      );

      const nodesMap = new Map<string, GraphNode>();
      const edges: GraphEdge[] = [];

      for (const record of result.records) {
        const eProps = nodeProps(record.get("e"));
        const tProps = nodeProps(record.get("t"));
        const rRel = record.get("r") as {
          type: string;
          properties: Record<string, unknown>;
        } | null;

        if (eProps["entityId"] && !nodesMap.has(str(eProps["entityId"]))) {
          nodesMap.set(str(eProps["entityId"]), {
            entityId: str(eProps["entityId"]),
            name: str(eProps["name"]),
            entityType: str(eProps["entityType"]),
          });
        }

        if (tProps["entityId"] && !nodesMap.has(str(tProps["entityId"]))) {
          nodesMap.set(str(tProps["entityId"]), {
            entityId: str(tProps["entityId"]),
            name: str(tProps["name"]),
            entityType: str(tProps["entityType"]),
          });
        }

        if (rRel && eProps["entityId"] && tProps["entityId"]) {
          const rProps =
            typeof rRel.properties === "object" && rRel.properties !== null
              ? rRel.properties
              : {};

          edges.push({
            sourceId: str(eProps["entityId"]),
            targetId: str(tProps["entityId"]),
            type: rRel.type,
            ...(rProps["label"] !== undefined && {
              label: str(rProps["label"]),
            }),
          });
        }
      }

      return { nodes: Array.from(nodesMap.values()), edges };
    } finally {
      await session.close();
    }
  }

  async mergeEntity(data: {
    entityId: string;
    projectId: string;
    name: string;
    entityType: string;
  }): Promise<void> {
    const session = getNeo4jDriver().session();
    try {
      await session.run(
        `MERGE (e:Entity { entityId: $entityId })
         ON CREATE SET e.projectId = $projectId, e.name = $name, e.entityType = $entityType
         ON MATCH  SET e.name = $name, e.entityType = $entityType`,
        data,
      );
    } finally {
      await session.close();
    }
  }

  async deleteEntity(entityId: string): Promise<void> {
    const session = getNeo4jDriver().session();
    try {
      await session.run(
        "MATCH (e:Entity { entityId: $entityId }) DETACH DELETE e",
        { entityId },
      );
    } finally {
      await session.close();
    }
  }

  async createRelationship(data: {
    projectId: string;
    sourceEntityId: string;
    targetEntityId: string;
    type: string;
    label?: string;
  }): Promise<void> {
    const session = getNeo4jDriver().session();

    try {
      // `data.type` is validated against `relationshipTypeSchema` at the API
      // boundary (controller + Zod), so it is guaranteed to be a safe,
      // known relationship type by the time it reaches here.
      await session.run(
        `MATCH (a:Entity { entityId: $sourceId, projectId: $projectId })
         MATCH (b:Entity { entityId: $targetId, projectId: $projectId })
         MERGE (a)-[r:${data.type}]->(b)
         ON CREATE SET r.label = $label
         ON MATCH  SET r.label = $label`,
        {
          sourceId: data.sourceEntityId,
          targetId: data.targetEntityId,
          projectId: data.projectId,
          label: data.label ?? data.type,
        },
      );
    } finally {
      await session.close();
    }
  }

  async deleteRelationship(data: {
    projectId: string;
    sourceEntityId: string;
    targetEntityId: string;
  }): Promise<void> {
    const session = getNeo4jDriver().session();

    try {
      await session.run(
        `MATCH (a:Entity { entityId: $sourceId, projectId: $projectId })
          -[r]->
          (b:Entity { entityId: $targetId, projectId: $projectId })
         DELETE r`,
        {
          sourceId: data.sourceEntityId,
          targetId: data.targetEntityId,
          projectId: data.projectId,
        },
      );
    } finally {
      await session.close();
    }
  }
  async syncAppearances(data: {
    projectId: string;
    manuscriptId: string;
    manuscriptTitle: string;
    chapterId: string;
    chapterTitle: string;
    entityIds: string[];
  }): Promise<void> {
    const session = getNeo4jDriver().session();

    try {
      await session.executeWrite(async (tx) => {
        // 1. Limpiar edges previos.
        await tx.run(
          `MATCH (e:Entity)-[r:APPEARS_IN]->(ch:Chapter { chapterId: $chapterId, projectId: $projectId })
           DELETE r`,
          { chapterId: data.chapterId, projectId: data.projectId },
        );

        if (data.entityIds.length === 0) return;

        await tx.run(
          `MERGE (ch:Chapter { chapterId: $chapterId, projectId: $projectId })
           ON CREATE SET ch.manuscriptId     = $manuscriptId,
                         ch.manuscriptTitle  = $manuscriptTitle,
                         ch.chapterTitle     = $chapterTitle
           ON MATCH  SET ch.manuscriptTitle  = $manuscriptTitle,
                         ch.chapterTitle     = $chapterTitle
           WITH ch
           UNWIND $entityIds AS entityId
           MATCH (e:Entity { entityId: entityId, projectId: $projectId })
           MERGE (e)-[:APPEARS_IN]->(ch)`,
          {
            projectId: data.projectId,
            manuscriptId: data.manuscriptId,
            manuscriptTitle: data.manuscriptTitle,
            chapterId: data.chapterId,
            chapterTitle: data.chapterTitle,
            entityIds: data.entityIds,
          },
        );
      });
    } finally {
      await session.close();
    }
  }

  async mergeAppearancesBatch(data: {
    projectId: string;
    manuscriptId: string;
    manuscriptTitle: string;
    chapterId: string;
    chapterTitle: string;
    entityIds: string[];
  }): Promise<void> {
    if (data.entityIds.length === 0) return;

    const driver = getNeo4jDriver();
    const session = driver.session();

    try {
      await session.run(
        `MERGE (ch:Chapter { chapterId: $chapterId, projectId: $projectId })
         ON CREATE SET ch.manuscriptId = $manuscriptId, ch.manuscriptTitle = $manuscriptTitle, ch.chapterTitle = $chapterTitle
         ON MATCH  SET ch.manuscriptTitle = $manuscriptTitle, ch.chapterTitle = $chapterTitle
         WITH ch
         UNWIND $entityIds AS eid
         MATCH (e:Entity { entityId: eid, projectId: $projectId })
         MERGE (e)-[:APPEARS_IN]->(ch)`,
        {
          projectId: data.projectId,
          manuscriptId: data.manuscriptId,
          manuscriptTitle: data.manuscriptTitle,
          chapterId: data.chapterId,
          chapterTitle: data.chapterTitle,
          entityIds: data.entityIds,
        }
      );
    } finally {
      await session.close();
    }
  }

  async clearChapterAppearances(data: {
    projectId: string;
    chapterId: string;
  }): Promise<void> {
    const session = getNeo4jDriver().session();
    try {
      await session.run(
        `MATCH (e:Entity)-[r:APPEARS_IN]->(ch:Chapter { chapterId: $chapterId, projectId: $projectId })
         DELETE r`,
        { chapterId: data.chapterId, projectId: data.projectId },
      );
    } finally {
      await session.close();
    }
  }

  async getEntityAppearances(data: {
    projectId: string;
    entityId: string;
  }): Promise<IAppearanceRecord[]> {
    const session = getNeo4jDriver().session();

    try {
      const result = await session.run(
        `MATCH (e:Entity { entityId: $entityId, projectId: $projectId })-[:APPEARS_IN]->(ch:Chapter { projectId: $projectId })
         RETURN ch.manuscriptId    AS manuscriptId,
                ch.manuscriptTitle AS manuscriptTitle,
                ch.chapterId       AS chapterId,
                ch.chapterTitle    AS chapterTitle
         ORDER BY ch.manuscriptId, ch.chapterId`,
        { entityId: data.entityId, projectId: data.projectId },
      );

      return result.records.map((r) => ({
        manuscriptId: r.get("manuscriptId") as string,
        manuscriptTitle: r.get("manuscriptTitle") as string,
        chapterId: r.get("chapterId") as string,
        chapterTitle: r.get("chapterTitle") as string,
      }));
    } finally {
      await session.close();
    }
  }

  bufferAppearance(data: {
    projectId: string;
    entityId: string;
    manuscriptId: string;
    manuscriptTitle: string;
    chapterId: string;
    chapterTitle: string;
  }): void {
    // síncrono, sin I/O
    const key = `${data.projectId}::${data.chapterId}`;
    if (!this.pendingAppearances.has(key)) {
      this.pendingAppearances.set(key, {
        meta: {
          projectId: data.projectId,
          manuscriptId: data.manuscriptId,
          manuscriptTitle: data.manuscriptTitle,
          chapterId: data.chapterId,
          chapterTitle: data.chapterTitle,
        },
        entityIds: new Set(),
      });
    }
    this.pendingAppearances.get(key)!.entityIds.add(data.entityId);
  }

  async flushAppearances(): Promise<void> {
    if (this.pendingAppearances.size === 0) return;

    // Un syncAppearances por chapterId — cada uno ya usa UNWIND internamente
    await Promise.all(
      [...this.pendingAppearances.values()].map(({ meta, entityIds }) =>
        this.syncAppearances({ ...meta, entityIds: [...entityIds] }),
      ),
    );

    this.pendingAppearances.clear();
  }
}
