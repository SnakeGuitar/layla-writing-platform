import { describe, it, expect, vi, beforeAll } from "vitest";
import type { IWikiEntryNoDescription } from "@/interfaces/wiki/IWikiEntry";
import type { IMention } from "@/interfaces/manuscript/IManuscript";

// Prevent module-initialization side effects (container → Neo4jGraphRepository → config/env)
vi.mock("@/services/container", () => ({
  container: { wikiRepo: {}, graphRepo: {}, manuscriptRepo: {} },
  setContainer: vi.fn(),
}));

import { stripRtf, extractMentions, syncChapterMentions } from "@/services/Mention.service";

// ── helpers ───────────────────────────────────────────────────────────────────

function makeEntry(
  entityId: string,
  name: string,
  entityType: IWikiEntryNoDescription["entityType"] = "Character",
): IWikiEntryNoDescription {
  return {
    entityId,
    name,
    entityType,
    projectId: "p1",
    aliases: [],
    tags: [],
    neo4jSynced: true,
    createdAt: new Date(),
    updatedAt: new Date(),
  };
}

const baseData = {
  projectId: "p1",
  manuscriptId: "m1",
  manuscriptTitle: "Part I",
  chapterId: "c1",
  chapterTitle: "Chapter 1",
};

function makeRepo(
  listFn = vi.fn().mockResolvedValue([]),
  syncFn = vi.fn().mockResolvedValue(undefined),
) {
  return {
    wikiRepo: { listEntries: listFn },
    graphRepo: { syncAppearances: syncFn },
  } as any;
}

// ── stripRtf ──────────────────────────────────────────────────────────────────

describe("stripRtf", () => {
  describe("when input is empty", () => {
    it("returns empty string", () => expect(stripRtf("")).toBe(""));
  });

  describe("when input is plain text without RTF marker", () => {
    let result: string;
    beforeAll(() => { result = stripRtf("Hello World"); });
    it("returns the text unchanged", () => expect(result).toBe("Hello World"));
  });

  describe("when input contains RTF control words", () => {
    let result: string;
    beforeAll(() => { result = stripRtf("{\\rtf1\\ansi {\\b Hello} World}"); });
    it("preserves 'Hello'", () => expect(result).toContain("Hello"));
    it("preserves 'World'", () => expect(result).toContain("World"));
    it("removes backslash control sequences", () => expect(result).not.toMatch(/\\b\b/));
  });

  describe("when input has a hex escape", () => {
    let result: string;
    beforeAll(() => { result = stripRtf("{\\rtf1 caf\\'e9}"); });
    it("converts hex escape \\e9 to é", () => expect(result).toContain("é"));
  });

  describe("when input has a unicode escape", () => {
    let result: string;
    beforeAll(() => { result = stripRtf("{\\rtf1 \\u233?}"); });
    it("converts unicode escape \\u233 to é", () => expect(result).toContain("é"));
  });

  describe("when output has consecutive spaces", () => {
    let result: string;
    beforeAll(() => { result = stripRtf("{\\rtf1 word1    word2}"); });
    it("collapses multiple spaces to one", () => expect(result).not.toMatch(/  /));
  });
});

// ── extractMentions ────────────────────────────────────────────────────────────

describe("extractMentions", () => {
  describe("when entry list is empty", () => {
    it("returns empty array", () => expect(extractMentions("some text", [])).toHaveLength(0));
  });

  describe("when text does not contain any entity name", () => {
    it("returns empty array", () =>
      expect(extractMentions("random words", [makeEntry("e1", "Gandalf")])).toHaveLength(0));
  });

  describe("when text contains an exact entity name", () => {
    let result: IMention[];
    beforeAll(() => { result = extractMentions("Gandalf arrived.", [makeEntry("e1", "Gandalf")]); });
    it("returns one mention", () => expect(result).toHaveLength(1));
    it("mention has correct entityId", () => expect(result[0]?.entityId).toBe("e1"));
    it("mention has correct name", () => expect(result[0]?.name).toBe("Gandalf"));
    it("mention has correct entityType", () => expect(result[0]?.entityType).toBe("Character"));
  });

  describe("when match is case-insensitive", () => {
    let result: IMention[];
    beforeAll(() => { result = extractMentions("gandalf arrived.", [makeEntry("e1", "Gandalf")]); });
    it("still finds the mention", () => expect(result).toHaveLength(1));
  });

  describe("when entity name is a substring of another word (word-boundary check)", () => {
    it("does not match partial word", () =>
      expect(extractMentions("Heroic deeds.", [makeEntry("e1", "Hero")])).toHaveLength(0));
  });

  describe("when the same entity appears multiple times in the text", () => {
    let result: IMention[];
    beforeAll(() => {
      result = extractMentions(
        "Gandalf spoke. Gandalf left.",
        [makeEntry("e1", "Gandalf")],
      );
    });
    it("returns only one mention per entity", () => expect(result).toHaveLength(1));
  });

  describe("when an entry has an empty name", () => {
    it("skips that entry", () =>
      expect(extractMentions("anything", [makeEntry("e1", "")])).toHaveLength(0));
  });

  describe("when an entry name exceeds 200 characters", () => {
    it("skips that entry", () => {
      const longName = "A".repeat(201);
      expect(extractMentions(longName, [makeEntry("e1", longName)])).toHaveLength(0);
    });
  });

  describe("when multiple distinct entities are present", () => {
    let result: IMention[];
    beforeAll(() => {
      result = extractMentions("Frodo and Gandalf walked.", [
        makeEntry("e1", "Frodo"),
        makeEntry("e2", "Gandalf"),
      ]);
    });
    it("returns two mentions", () => expect(result).toHaveLength(2));
    it("first mention is Frodo", () => expect(result.some((m) => m.name === "Frodo")).toBe(true));
    it("second mention is Gandalf", () => expect(result.some((m) => m.name === "Gandalf")).toBe(true));
  });
});

// ── syncChapterMentions ────────────────────────────────────────────────────────

describe("syncChapterMentions", () => {
  describe("when wiki entry list is empty", () => {
    let result: IMention[];
    beforeAll(async () => {
      result = await syncChapterMentions(
        { ...baseData, content: "Gandalf was there." },
        makeRepo(vi.fn().mockResolvedValue([])),
      );
    });
    it("returns empty array", () => expect(result).toHaveLength(0));
  });

  describe("when content matches a wiki entry", () => {
    let result: IMention[];
    let syncFn: ReturnType<typeof vi.fn>;

    beforeAll(async () => {
      const entry = makeEntry("e1", "Gandalf");
      syncFn = vi.fn().mockResolvedValue(undefined);
      result = await syncChapterMentions(
        { ...baseData, content: "Gandalf walked." },
        makeRepo(vi.fn().mockResolvedValue([entry]), syncFn),
      );
    });

    it("returns one mention", () => expect(result).toHaveLength(1));
    it("calls syncAppearances once", () => expect(syncFn).toHaveBeenCalledOnce());
  });

  describe("when content has no matches", () => {
    let syncFn: ReturnType<typeof vi.fn>;

    beforeAll(async () => {
      const entry = makeEntry("e1", "Gandalf");
      syncFn = vi.fn().mockResolvedValue(undefined);
      await syncChapterMentions(
        { ...baseData, content: "No characters here." },
        makeRepo(vi.fn().mockResolvedValue([entry]), syncFn),
      );
    });

    it("does not call syncAppearances", () => expect(syncFn).not.toHaveBeenCalled());
  });

  describe("when the matched entity is deleted between extraction and sync", () => {
    let result: IMention[];

    beforeAll(async () => {
      const entry = makeEntry("e1", "Gandalf");
      // First listEntries → entry present; second (re-validation) → empty (entry was deleted)
      const listFn = vi.fn()
        .mockResolvedValueOnce([entry])
        .mockResolvedValueOnce([]);
      result = await syncChapterMentions(
        { ...baseData, content: "Gandalf walked." },
        makeRepo(listFn),
      );
    });

    it("filters out the deleted mention", () => expect(result).toHaveLength(0));
  });

  describe("when syncAppearances throws", () => {
    it("propagates the error to the caller", async () => {
      const entry = makeEntry("e1", "Gandalf");
      const repo = makeRepo(
        vi.fn().mockResolvedValue([entry]),
        vi.fn().mockRejectedValue(new Error("Neo4j unavailable")),
      );
      await expect(
        syncChapterMentions({ ...baseData, content: "Gandalf walked." }, repo),
      ).rejects.toThrow();
    });
  });
});
