import { describe, it, expect, vi, beforeAll } from "vitest";
import type { IWikiEntry } from "@/interfaces/wiki/IWikiEntry";

vi.mock("@/services/container", () => ({
  container: { wikiRepo: {}, graphRepo: {}, manuscriptRepo: {} },
  setContainer: vi.fn(),
}));

import { createEntry, updateEntry, deleteEntry } from "@/services/WikiEntry.service";

// ── helpers ───────────────────────────────────────────────────────────────────

function makeEntry(overrides: Partial<IWikiEntry> = {}): IWikiEntry {
  return {
    entityId: "e1",
    projectId: "p1",
    name: "Gandalf",
    entityType: "Character",
    description: "A wizard",
    aliases: [],
    tags: [],
    neo4jSynced: false,
    createdAt: new Date(),
    updatedAt: new Date(),
    ...overrides,
  };
}

function makeContainer(overrides: {
  wikiRepo?: Partial<Record<string, ReturnType<typeof vi.fn>>>;
  graphRepo?: Partial<Record<string, ReturnType<typeof vi.fn>>>;
} = {}) {
  return {
    wikiRepo: {
      createEntry: vi.fn().mockResolvedValue(makeEntry()),
      updateEntry: vi.fn().mockResolvedValue(makeEntry()),
      deleteEntry: vi.fn().mockResolvedValue(false),
      listEntries: vi.fn().mockResolvedValue([]),
      getEntry: vi.fn().mockResolvedValue(null),
      ...overrides.wikiRepo,
    },
    graphRepo: {
      mergeEntity: vi.fn().mockResolvedValue(undefined),
      deleteEntity: vi.fn().mockResolvedValue(undefined),
      ...overrides.graphRepo,
    },
  } as any;
}

// ── createEntry — Neo4j sync succeeds ─────────────────────────────────────────

describe("createEntry", () => {
  describe("when creation and Neo4j sync succeed", () => {
    let result: IWikiEntry;
    let repo: ReturnType<typeof makeContainer>;

    beforeAll(async () => {
      const entry = makeEntry({ neo4jSynced: false });
      repo = makeContainer({
        wikiRepo: {
          createEntry: vi.fn().mockResolvedValue(entry),
          updateEntry: vi.fn().mockResolvedValue({ ...entry, neo4jSynced: true }),
        },
        graphRepo: { mergeEntity: vi.fn().mockResolvedValue(undefined) },
      });
      result = await createEntry(
        { projectId: "p1", name: "Gandalf", entityType: "Character" },
        repo,
      );
    });

    it("returns the entry", () => expect(result).toBeDefined());
    it("calls mergeEntity to sync to Neo4j", () =>
      expect(repo.graphRepo.mergeEntity).toHaveBeenCalledOnce());
    it("marks entry as synced", () => expect(result.neo4jSynced).toBe(true));
  });

  describe("when Neo4j sync fails", () => {
    let result: IWikiEntry;

    beforeAll(async () => {
      const entry = makeEntry({ neo4jSynced: false });
      const repo = makeContainer({
        wikiRepo: { createEntry: vi.fn().mockResolvedValue(entry) },
        graphRepo: { mergeEntity: vi.fn().mockRejectedValue(new Error("Neo4j down")) },
      });
      result = await createEntry(
        { projectId: "p1", name: "Gandalf", entityType: "Character" },
        repo,
      );
    });

    it("still returns the entry (tolerant to Neo4j failures)", () => expect(result).toBeDefined());
    it("entry remains marked as not synced", () => expect(result.neo4jSynced).toBe(false));
  });
});

// ── updateEntry ───────────────────────────────────────────────────────────────

describe("updateEntry", () => {
  describe("when the entry does not exist", () => {
    let result: IWikiEntry | null;

    beforeAll(async () => {
      const repo = makeContainer({
        wikiRepo: { updateEntry: vi.fn().mockResolvedValue(null) },
      });
      result = await updateEntry("e-missing", { name: "New Name" }, "p1", repo);
    });

    it("returns null", () => expect(result).toBeNull());
  });

  describe("when entry exists and Neo4j sync succeeds", () => {
    let result: IWikiEntry | null;
    let repo: ReturnType<typeof makeContainer>;

    beforeAll(async () => {
      const entry = makeEntry({ neo4jSynced: true });
      repo = makeContainer({
        wikiRepo: { updateEntry: vi.fn().mockResolvedValue(entry) },
        graphRepo: { mergeEntity: vi.fn().mockResolvedValue(undefined) },
      });
      result = await updateEntry("e1", { name: "Updated Name" }, "p1", repo);
    });

    it("returns the entry", () => expect(result).toBeDefined());
    it("calls mergeEntity to re-sync the graph node", () =>
      expect(repo.graphRepo.mergeEntity).toHaveBeenCalledOnce());
  });

  describe("when Neo4j sync fails on update", () => {
    let result: IWikiEntry | null;

    beforeAll(async () => {
      const entry = makeEntry();
      const repo = makeContainer({
        wikiRepo: { updateEntry: vi.fn().mockResolvedValue(entry) },
        graphRepo: { mergeEntity: vi.fn().mockRejectedValue(new Error("Neo4j down")) },
      });
      result = await updateEntry("e1", { name: "Updated" }, "p1", repo);
    });

    it("still returns the entry (tolerant to Neo4j failures)", () => expect(result).toBeDefined());
  });
});

// ── deleteEntry ───────────────────────────────────────────────────────────────

describe("deleteEntry", () => {
  describe("when the wiki entry is not found in MongoDB", () => {
    let result: boolean;
    let repo: ReturnType<typeof makeContainer>;

    beforeAll(async () => {
      repo = makeContainer({
        wikiRepo: { deleteEntry: vi.fn().mockResolvedValue(false) },
      });
      result = await deleteEntry("e-missing", "p1", repo);
    });

    it("returns false", () => expect(result).toBe(false));
    it("never calls deleteEntity on Neo4j", () =>
      expect(repo.graphRepo.deleteEntity).not.toHaveBeenCalled());
  });

  describe("when MongoDB deletion succeeds and Neo4j responds on first attempt", () => {
    let result: boolean;
    let repo: ReturnType<typeof makeContainer>;

    beforeAll(async () => {
      repo = makeContainer({
        wikiRepo: { deleteEntry: vi.fn().mockResolvedValue(true) },
        graphRepo: { deleteEntity: vi.fn().mockResolvedValue(undefined) },
      });
      result = await deleteEntry("e1", "p1", repo);
    });

    it("returns true", () => expect(result).toBe(true));
    it("calls deleteEntity exactly once (no retry needed)", () =>
      expect(repo.graphRepo.deleteEntity).toHaveBeenCalledTimes(1));
  });

  describe("when Neo4j delete fails on all three retries", () => {
    let result: boolean;
    let repo: ReturnType<typeof makeContainer>;

    beforeAll(async () => {
      vi.useFakeTimers();
      repo = makeContainer({
        wikiRepo: { deleteEntry: vi.fn().mockResolvedValue(true) },
        graphRepo: {
          deleteEntity: vi.fn().mockRejectedValue(new Error("Neo4j unavailable")),
        },
      });
      const promise = deleteEntry("e1", "p1", repo);
      await vi.runAllTimersAsync();
      result = await promise;
      vi.useRealTimers();
    });

    it("returns true (MongoDB deletion succeeded, orphaned node is acceptable)", () =>
      expect(result).toBe(true));
    it("retries deleteEntity exactly three times", () =>
      expect(repo.graphRepo.deleteEntity).toHaveBeenCalledTimes(3));
  });

  describe("when Neo4j delete succeeds on the second attempt", () => {
    let result: boolean;
    let repo: ReturnType<typeof makeContainer>;

    beforeAll(async () => {
      vi.useFakeTimers();
      let callCount = 0;
      repo = makeContainer({
        wikiRepo: { deleteEntry: vi.fn().mockResolvedValue(true) },
        graphRepo: {
          deleteEntity: vi.fn().mockImplementation(() => {
            callCount++;
            return callCount < 2
              ? Promise.reject(new Error("transient"))
              : Promise.resolve(undefined);
          }),
        },
      });
      const promise = deleteEntry("e1", "p1", repo);
      await vi.runAllTimersAsync();
      result = await promise;
      vi.useRealTimers();
    });

    it("returns true", () => expect(result).toBe(true));
    it("stops retrying after first success (calls deleteEntity twice)", () =>
      expect(repo.graphRepo.deleteEntity).toHaveBeenCalledTimes(2));
  });
});
