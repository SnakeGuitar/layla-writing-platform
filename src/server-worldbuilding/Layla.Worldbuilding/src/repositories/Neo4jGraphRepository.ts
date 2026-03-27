import { getNeo4jDriver } from "../db/neo4j";
import type {
  IGraphResult,
  GraphNode,
  GraphEdge,
} from "../interfaces/graph/IGraphResult";
import type {
  IGraphRepository,
  IAppearanceRecord,
} from "../interfaces/repositories/IGraphRepository";

export class Neo4jGraphRepository implements IGraphRepository {
  async getGraph(
    projectId: string,
    entityType?: string,
  ): Promise<IGraphResult> {
    const driver = getNeo4jDriver();
    const session = driver.session();

    try {
      const query = entityType
        ? `MATCH (e:Entity { projectId: $projectId, entityType: $entityType })
           OPTIONAL MATCH (e)-[r]->(t:Entity { projectId: $projectId })
           RETURN e, r, t`
        : `MATCH (e:Entity { projectId: $projectId })
           OPTIONAL MATCH (e)-[r]->(t:Entity { projectId: $projectId })
           RETURN e, r, t`;

      const result = await session.run(query, { projectId, entityType });

      const nodesMap = new Map<string, GraphNode>();
      const edges: GraphEdge[] = [];

      for (const record of result.records) {
        const eNode = record.get("e");
        const rRel = record.get("r");
        const tNode = record.get("t");

        if (eNode) {
          const props = eNode.properties as Record<string, string>;
          if (props["entityId"] && !nodesMap.has(props["entityId"])) {
            nodesMap.set(props["entityId"], {
              entityId: props["entityId"],
              name: props["name"] ?? "",
              entityType: props["entityType"] ?? "",
            });
          }
        }

        if (tNode) {
          const props = tNode.properties as Record<string, string>;
          if (props["entityId"] && !nodesMap.has(props["entityId"])) {
            nodesMap.set(props["entityId"], {
              entityId: props["entityId"],
              name: props["name"] ?? "",
              entityType: props["entityType"] ?? "",
            });
          }
        }

        if (rRel && eNode && tNode) {
          const eProps = eNode.properties as Record<string, string>;
          const tProps = tNode.properties as Record<string, string>;
          const rProps = rRel.properties as Record<string, string>;

          if (eProps["entityId"] && tProps["entityId"]) {
            const edge: GraphEdge = {
              sourceId: eProps["entityId"],
              targetId: tProps["entityId"],
              type: rRel.type as string,
              ...(rProps["label"] !== undefined && { label: rProps["label"] }),
            };
            edges.push(edge);
          }
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
    const driver = getNeo4jDriver();
    const session = driver.session();
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
    const driver = getNeo4jDriver();
    const session = driver.session();
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
    const driver = getNeo4jDriver();
    const session = driver.session();

    try {
      await session.run(
        `MATCH (a:Entity { entityId: $sourceId, projectId: $projectId })
         MATCH (b:Entity { entityId: $targetId, projectId: $projectId })
         MERGE (a)-[r:RELATED_TO { label: $label }]->(b)`,
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
    const driver = getNeo4jDriver();
    const session = driver.session();

    try {
      await session.run(
        `MATCH (a:Entity { entityId: $sourceId, projectId: $projectId })-[r]->(b:Entity { entityId: $targetId, projectId: $projectId })
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

  async mergeAppearance(data: {
    projectId: string;
    entityId: string;
    manuscriptId: string;
    manuscriptTitle: string;
    chapterId: string;
    chapterTitle: string;
  }): Promise<void> {
    const driver = getNeo4jDriver();
    const session = driver.session();

    try {
      await session.run(
        `MERGE (ch:Chapter { chapterId: $chapterId, projectId: $projectId })
         ON CREATE SET ch.manuscriptId = $manuscriptId, ch.manuscriptTitle = $manuscriptTitle, ch.chapterTitle = $chapterTitle
         ON MATCH  SET ch.manuscriptTitle = $manuscriptTitle, ch.chapterTitle = $chapterTitle
         WITH ch
         MATCH (e:Entity { entityId: $entityId, projectId: $projectId })
         MERGE (e)-[:APPEARS_IN]->(ch)`,
        {
          projectId: data.projectId,
          entityId: data.entityId,
          manuscriptId: data.manuscriptId,
          manuscriptTitle: data.manuscriptTitle,
          chapterId: data.chapterId,
          chapterTitle: data.chapterTitle,
        },
      );
    } finally {
      await session.close();
    }
  }

  async clearChapterAppearances(data: {
    projectId: string;
    chapterId: string;
  }): Promise<void> {
    const driver = getNeo4jDriver();
    const session = driver.session();

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
    const driver = getNeo4jDriver();
    const session = driver.session();

    try {
      const result = await session.run(
        `MATCH (e:Entity { entityId: $entityId, projectId: $projectId })-[:APPEARS_IN]->(ch:Chapter { projectId: $projectId })
         RETURN ch.manuscriptId AS manuscriptId, ch.manuscriptTitle AS manuscriptTitle,
                ch.chapterId AS chapterId, ch.chapterTitle AS chapterTitle
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
}
