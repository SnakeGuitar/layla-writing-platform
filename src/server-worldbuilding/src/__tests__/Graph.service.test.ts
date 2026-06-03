import { describe, it, expect, vi, beforeAll } from "vitest";
import type { IGraphRepository, IAppearanceRecord } from "@/interfaces/repositories/IGraphRepository";
import type { IGraphResult } from "@/interfaces/graph/IGraphResult";

vi.mock("@/services/container", () => ({
  container: { graphRepo: {}, manuscriptRepo: {}, wikiRepo: {} },
  setContainer: vi.fn(),
}));

import { getGraph, createRelationship, deleteRelationship, getEntityAppearances } from "@/services/Graph.service";

// ── helpers ───────────────────────────────────────────────────────────────────

function makeRepo(overrides: Partial<IGraphRepository> = {}): IGraphRepository {
  return {
    getGraph: vi.fn(),
    mergeEntity: vi.fn(),
    deleteEntity: vi.fn(),
    createRelationship: vi.fn(),
    deleteRelationship: vi.fn(),
    syncAppearances: vi.fn(),
    mergeAppearancesBatch: vi.fn(),
    clearChapterAppearances: vi.fn(),
    getEntityAppearances: vi.fn(),
    ...overrides,
  } as IGraphRepository;
}

const emptyGraph: IGraphResult = { nodes: [], edges: [] };

// ── getGraph — delegates to repo ─────────────────────────────────────────────

describe("getGraph", () => {
  describe("when called with a projectId only", () => {
    let result: IGraphResult;
    let repoMock: IGraphRepository;

    beforeAll(async () => {
      repoMock = makeRepo({ getGraph: vi.fn().mockResolvedValue(emptyGraph) });
      result = await getGraph("project-1", undefined, repoMock);
    });

    it("calls repo.getGraph", () => expect(repoMock.getGraph).toHaveBeenCalled());
    it("passes the projectId to the repo", () => expect(repoMock.getGraph).toHaveBeenCalledWith("project-1", undefined));
    it("returns the repo result unchanged", () => expect(result).toBe(emptyGraph));
  });

  describe("when called with an entityType filter", () => {
    let repoMock: IGraphRepository;

    beforeAll(async () => {
      repoMock = makeRepo({ getGraph: vi.fn().mockResolvedValue(emptyGraph) });
      await getGraph("p1", "Character", repoMock);
    });

    it("passes the entityType filter to the repo", () =>
      expect(repoMock.getGraph).toHaveBeenCalledWith("p1", "Character"));
  });

  describe("when the project has nodes", () => {
    let result: IGraphResult;

    beforeAll(async () => {
      const graph: IGraphResult = {
        nodes: [{ id: "n1", label: "Hero", entityType: "Character", projectId: "p1" }],
        edges: [],
      };
      const repo = makeRepo({ getGraph: vi.fn().mockResolvedValue(graph) });
      result = await getGraph("p1", undefined, repo);
    });

    it("returns the correct node count", () => expect(result.nodes).toHaveLength(1));
    it("returns the correct node id", () => expect(result.nodes[0]?.id).toBe("n1"));
  });
});

// ── createRelationship ────────────────────────────────────────────────────────

describe("createRelationship", () => {
  describe("when both entities exist", () => {
    let result: boolean;
    let repoMock: IGraphRepository;
    const data = { projectId: "p2", sourceEntityId: "s1", targetEntityId: "t1", type: "ENEMY_OF", label: "Nemesis" };

    beforeAll(async () => {
      repoMock = makeRepo({ createRelationship: vi.fn().mockResolvedValue(true) });
      result = await createRelationship(data, repoMock);
    });

    it("returns true", () => expect(result).toBe(true));
    it("passes all fields to the repo", () => expect(repoMock.createRelationship).toHaveBeenCalledWith(data));
  });

  describe("when a source or target entity does not exist", () => {
    let result: boolean;

    beforeAll(async () => {
      const repo = makeRepo({ createRelationship: vi.fn().mockResolvedValue(false) });
      result = await createRelationship(
        { projectId: "p1", sourceEntityId: "missing", targetEntityId: "e2", type: "KNOWS" },
        repo,
      );
    });

    it("returns false", () => expect(result).toBe(false));
  });
});

// ── deleteRelationship ────────────────────────────────────────────────────────

describe("deleteRelationship", () => {
  describe("when deletion succeeds", () => {
    let repoMock: IGraphRepository;
    let resolved: boolean;

    beforeAll(async () => {
      repoMock = makeRepo({ deleteRelationship: vi.fn().mockResolvedValue(undefined) });
      await deleteRelationship({ projectId: "p1", sourceEntityId: "e1", targetEntityId: "e2" }, repoMock);
      resolved = true;
    });

    it("delegates to repo.deleteRelationship", () => expect(repoMock.deleteRelationship).toHaveBeenCalled());
    it("resolves without throwing", () => expect(resolved).toBe(true));
  });

  describe("when the repo throws", () => {
    it("propagates the error", async () => {
      const repo = makeRepo({
        deleteRelationship: vi.fn().mockRejectedValue(new Error("Neo4j unavailable")),
      });
      await expect(
        deleteRelationship({ projectId: "p1", sourceEntityId: "a", targetEntityId: "b" }, repo),
      ).rejects.toThrow("Neo4j unavailable");
    });
  });
});

// ── getEntityAppearances ──────────────────────────────────────────────────────

describe("getEntityAppearances", () => {
  describe("when the entity has no appearances", () => {
    let result: IAppearanceRecord[];

    beforeAll(async () => {
      const repo = makeRepo({ getEntityAppearances: vi.fn().mockResolvedValue([]) });
      result = await getEntityAppearances("p1", "entity-1", repo);
    });

    it("returns an empty array", () => expect(result).toHaveLength(0));
  });

  describe("when the entity appears in chapters", () => {
    const appearances: IAppearanceRecord[] = [
      { manuscriptId: "m1", manuscriptTitle: "Part I", chapterId: "c1", chapterTitle: "The Beginning" },
    ];
    let result: IAppearanceRecord[];

    beforeAll(async () => {
      const repo = makeRepo({ getEntityAppearances: vi.fn().mockResolvedValue(appearances) });
      result = await getEntityAppearances("p1", "hero-id", repo);
    });

    it("returns one record", () => expect(result).toHaveLength(1));
    it("record has the correct chapterTitle", () => expect(result[0]?.chapterTitle).toBe("The Beginning"));
    it("record has the correct manuscriptId", () => expect(result[0]?.manuscriptId).toBe("m1"));
  });

  describe("parameters passed to the repo", () => {
    let repoMock: IGraphRepository;

    beforeAll(async () => {
      repoMock = makeRepo({ getEntityAppearances: vi.fn().mockResolvedValue([]) });
      await getEntityAppearances("project-xyz", "entity-abc", repoMock);
    });

    it("passes the correct projectId", () =>
      expect(repoMock.getEntityAppearances).toHaveBeenCalledWith(
        expect.objectContaining({ projectId: "project-xyz" }),
      ));

    it("passes the correct entityId", () =>
      expect(repoMock.getEntityAppearances).toHaveBeenCalledWith(
        expect.objectContaining({ entityId: "entity-abc" }),
      ));
  });
});
