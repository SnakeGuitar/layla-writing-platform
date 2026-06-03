import { describe, it, expect, vi, beforeAll } from "vitest";
import type { IChapter, IManuscript } from "@/interfaces/manuscript/IManuscript";

// Prevent container module side effects
vi.mock("@/services/container", () => ({
  container: {
    wikiRepo: { listEntries: vi.fn().mockResolvedValue([]) },
    graphRepo: { syncAppearances: vi.fn().mockResolvedValue(undefined) },
    manuscriptRepo: {},
  },
  setContainer: vi.fn(),
}));

// Silence ChapterVersionModel calls (autosave/history not under test here)
vi.mock("@/models/ChapterVersion.model", () => ({
  ChapterVersionModel: { create: vi.fn().mockResolvedValue({}) },
}));

import {
  createManuscript,
  getFullStoryByProject,
  updateChapter,
  createChapter,
} from "@/services/Manuscript.service";

// ── helpers ───────────────────────────────────────────────────────────────────

function makeManuscript(overrides: Partial<IManuscript> = {}): IManuscript {
  return {
    manuscriptId: "m1",
    projectId: "p1",
    title: "My Manuscript",
    order: 0,
    chapters: [],
    createdAt: new Date(),
    updatedAt: new Date(),
    ...overrides,
  };
}

function makeChapter(overrides: Partial<IChapter> = {}): IChapter {
  return {
    chapterId: "c1",
    title: "Chapter 1",
    content: "{\\rtf1 content}",
    order: 0,
    mentions: [],
    createdAt: new Date(),
    updatedAt: new Date(Date.now() - 10_000), // 10 s ago by default
    ...overrides,
  };
}

function makeRepo(overrides: Partial<any> = {}) {
  return {
    getManuscriptsByProject: vi.fn().mockResolvedValue([]),
    getManuscript: vi.fn().mockResolvedValue(null),
    createManuscript: vi.fn().mockImplementation((data) =>
      Promise.resolve({ ...makeManuscript(), ...data }),
    ),
    updateManuscript: vi.fn().mockResolvedValue(null),
    deleteManuscript: vi.fn().mockResolvedValue(false),
    getChapter: vi.fn().mockResolvedValue(null),
    createChapter: vi.fn().mockImplementation((_p, _m, data) => Promise.resolve(data)),
    updateChapter: vi.fn().mockResolvedValue(null),
    deleteChapter: vi.fn().mockResolvedValue(false),
    ...overrides,
  };
}

// ── createManuscript — auto-order ─────────────────────────────────────────────

describe("createManuscript", () => {
  describe("when no order is provided and two manuscripts already exist", () => {
    let capturedOrder: number;

    beforeAll(async () => {
      const repo = makeRepo({
        getManuscriptsByProject: vi.fn().mockResolvedValue([
          makeManuscript({ order: 0 }),
          makeManuscript({ order: 1 }),
        ]),
        createManuscript: vi.fn().mockImplementation((data) => {
          capturedOrder = data.order;
          return Promise.resolve({ ...makeManuscript(), ...data });
        }),
      });
      await createManuscript("p1", { title: "New" }, repo);
    });

    it("assigns order equal to the existing manuscript count", () =>
      expect(capturedOrder).toBe(2));
  });

  describe("when an explicit order is provided", () => {
    let capturedOrder: number;

    beforeAll(async () => {
      const repo = makeRepo({
        getManuscriptsByProject: vi.fn().mockResolvedValue([makeManuscript()]),
        createManuscript: vi.fn().mockImplementation((data) => {
          capturedOrder = data.order;
          return Promise.resolve({ ...makeManuscript(), ...data });
        }),
      });
      await createManuscript("p1", { title: "New", order: 5 }, repo);
    });

    it("uses the provided order", () => expect(capturedOrder).toBe(5));
  });

  describe("returned index object", () => {
    let result: any;

    beforeAll(async () => {
      const repo = makeRepo({
        getManuscriptsByProject: vi.fn().mockResolvedValue([]),
        createManuscript: vi.fn().mockImplementation((data) =>
          Promise.resolve({ ...makeManuscript(), ...data }),
        ),
      });
      result = await createManuscript("p1", { title: "My Novel" }, repo);
    });

    it("has the correct title", () => expect(result.title).toBe("My Novel"));
    it("does not expose chapter content field", () =>
      expect(Object.prototype.hasOwnProperty.call(result, "content")).toBe(false));
  });
});

describe("getFullStoryByProject", () => {
  let result: any;

  beforeAll(async () => {
    const repo = makeRepo({
      getManuscriptsByProject: vi.fn().mockResolvedValue([
        makeManuscript({
          manuscriptId: "m2",
          title: "Book 2",
          order: 1,
          chapters: [makeChapter({ chapterId: "c3", title: "Chapter 3", content: "third", order: 0 })],
        }),
        makeManuscript({
          manuscriptId: "m1",
          title: "Book 1",
          order: 0,
          chapters: [
            makeChapter({ chapterId: "c2", title: "Chapter 2", content: "second", order: 1 }),
            makeChapter({ chapterId: "c1", title: "Chapter 1", content: "first", order: 0 }),
          ],
        }),
      ]),
    });

    result = await getFullStoryByProject("p1", repo);
  });

  it("returns manuscripts in reading order", () =>
    expect(result.map((m: any) => m.manuscriptId)).toEqual(["m1", "m2"]));

  it("returns chapters in reading order", () =>
    expect(result[0].chapters.map((c: any) => c.chapterId)).toEqual(["c1", "c2"]));

  it("includes chapter content for full-story reading", () =>
    expect(result[0].chapters[0].content).toBe("first"));
});

// ── updateChapter — Last-Write-Wins conflict detection ────────────────────────

describe("updateChapter", () => {
  describe("when the chapter does not exist", () => {
    let result: any;

    beforeAll(async () => {
      const repo = makeRepo({ getChapter: vi.fn().mockResolvedValue(null) });
      result = await updateChapter("p1", "m1", "c1", { title: "New" }, repo);
    });

    it("conflict is false", () => expect(result.conflict).toBe(false));
    it("chapter is undefined", () => expect(result.chapter).toBeUndefined());
  });

  describe("when clientTimestamp is older than server updatedAt (stale write)", () => {
    let result: any;

    beforeAll(async () => {
      // Server was updated 1 s ago
      const chapter = makeChapter({ updatedAt: new Date(Date.now() - 1_000) });
      const repo = makeRepo({ getChapter: vi.fn().mockResolvedValue(chapter) });
      // Client's timestamp is 5 s ago — older than the server's 1 s ago
      const staleTimestamp = new Date(Date.now() - 5_000).toISOString();
      result = await updateChapter("p1", "m1", "c1", { clientTimestamp: staleTimestamp }, repo);
    });

    it("conflict is true", () => expect(result.conflict).toBe(true));
    it("returns the current server chapter state", () => expect(result.chapter).toBeDefined());
  });

  describe("when clientTimestamp is newer than server updatedAt (up-to-date write)", () => {
    let result: any;
    let repo: ReturnType<typeof makeRepo>;

    beforeAll(async () => {
      // Server was last updated 10 s ago
      const chapter = makeChapter({ updatedAt: new Date(Date.now() - 10_000) });
      const manuscript = makeManuscript({ chapters: [chapter] });
      repo = makeRepo({
        getChapter: vi.fn().mockResolvedValue(chapter),
        getManuscript: vi.fn().mockResolvedValue(manuscript),
        updateChapter: vi.fn().mockResolvedValue(manuscript),
      });
      // Client has a timestamp from 5 s ago — more recent than server's 10 s ago
      const freshTimestamp = new Date(Date.now() - 5_000).toISOString();
      result = await updateChapter("p1", "m1", "c1", { clientTimestamp: freshTimestamp, title: "Updated" }, repo);
    });

    it("conflict is false", () => expect(result.conflict).toBe(false));
    it("calls repo.updateChapter", () => expect(repo.updateChapter).toHaveBeenCalled());
  });

  describe("when no clientTimestamp is supplied", () => {
    let repo: ReturnType<typeof makeRepo>;

    beforeAll(async () => {
      const chapter = makeChapter();
      const manuscript = makeManuscript({ chapters: [chapter] });
      repo = makeRepo({
        getChapter: vi.fn().mockResolvedValue(chapter),
        getManuscript: vi.fn().mockResolvedValue(manuscript),
        updateChapter: vi.fn().mockResolvedValue(manuscript),
      });
      await updateChapter("p1", "m1", "c1", { title: "No timestamp" }, repo);
    });

    it("writes the update without performing a conflict check", () =>
      expect(repo.updateChapter).toHaveBeenCalled());
  });
});

// ── createChapter — auto-order ────────────────────────────────────────────────

describe("createChapter", () => {
  describe("when no order is provided and manuscript has two chapters", () => {
    let capturedData: any;

    beforeAll(async () => {
      const manuscript = makeManuscript({
        chapters: [makeChapter({ order: 0 }), makeChapter({ chapterId: "c2", order: 1 })],
      });
      const repo = makeRepo({
        getManuscript: vi.fn().mockResolvedValue(manuscript),
        createChapter: vi.fn().mockImplementation((_p, _m, data) => {
          capturedData = data;
          return Promise.resolve(data);
        }),
      });
      await createChapter("p1", "m1", { title: "Chapter 3" }, repo);
    });

    it("assigns order equal to the current chapter count", () =>
      expect(capturedData.order).toBe(2));
  });

  describe("when an explicit order is provided", () => {
    let capturedData: any;

    beforeAll(async () => {
      const repo = makeRepo({
        getManuscript: vi.fn().mockResolvedValue(makeManuscript()),
        createChapter: vi.fn().mockImplementation((_p, _m, data) => {
          capturedData = data;
          return Promise.resolve(data);
        }),
      });
      await createChapter("p1", "m1", { title: "Epilogue", order: 99 }, repo);
    });

    it("uses the provided order", () => expect(capturedData.order).toBe(99));
  });

  describe("new chapter properties", () => {
    let capturedData: any;

    beforeAll(async () => {
      const repo = makeRepo({
        getManuscript: vi.fn().mockResolvedValue(makeManuscript()),
        createChapter: vi.fn().mockImplementation((_p, _m, data) => {
          capturedData = data;
          return Promise.resolve(data);
        }),
      });
      await createChapter("p1", "m1", { title: "Prologue", content: "Once upon a time" }, repo);
    });

    it("assigns a non-empty chapterId", () =>
      expect(capturedData.chapterId).toBeTruthy());
    it("sets the content from the argument", () =>
      expect(capturedData.content).toBe("Once upon a time"));
    it("sets the title from the argument", () =>
      expect(capturedData.title).toBe("Prologue"));
  });
});
