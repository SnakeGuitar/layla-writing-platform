import { describe, it, expect, vi, beforeAll } from "vitest";
import type { Response, NextFunction } from "express";
import type InterfaceAuthRequest from "@/interfaces/auth/AuthRequest";

vi.mock("@/db/neo4j", () => ({
  getNeo4jDriver: vi.fn(),
}));

vi.mock("@/config/env", () => ({
  config: {
    coreApiUrl: "http://localhost:7166",
  },
}));

import { requireWriteAccess, requireProjectAccess } from "@/middlewares/ProjectGuard";
import { getNeo4jDriver } from "@/db/neo4j";

const mockGetDriver = vi.mocked(getNeo4jDriver);

// ── helpers ───────────────────────────────────────────────────────────────────

function makeReq(overrides: Partial<InterfaceAuthRequest> = {}): InterfaceAuthRequest {
  return {
    params: {},
    headers: {},
    originalUrl: "",
    url: "",
    ...overrides,
  } as unknown as InterfaceAuthRequest;
}

function makeRes() {
  const ctx = { statusCode: 0, body: null as unknown };
  const res = {
    status(s: number) { ctx.statusCode = s; return res; },
    json(b: unknown) { ctx.body = b; return res; },
  } as unknown as Response;
  return { res, ctx };
}

function makeSession(records: Array<Record<string, unknown>>) {
  return {
    run: vi.fn().mockResolvedValue({ records: records.map((r) => ({ get: (k: string) => r[k] })) }),
    close: vi.fn().mockResolvedValue(undefined),
  };
}

// ── requireWriteAccess — READER ───────────────────────────────────────────────

describe("requireWriteAccess", () => {
  describe("when role is READER", () => {
    let statusCode: number;
    let nextCalled: boolean;

    beforeAll(() => {
      const req = makeReq({ projectRole: "READER" });
      const { res, ctx } = makeRes();
      nextCalled = false;
      requireWriteAccess()(req, res, () => { nextCalled = true; });
      statusCode = ctx.statusCode;
    });

    it("responds with 403", () => expect(statusCode).toBe(403));
    it("does not call next", () => expect(nextCalled).toBe(false));
  });

  describe("when role is undefined", () => {
    let statusCode: number;
    let nextCalled: boolean;

    beforeAll(() => {
      const req = makeReq({ projectRole: undefined });
      const { res, ctx } = makeRes();
      nextCalled = false;
      requireWriteAccess()(req, res, () => { nextCalled = true; });
      statusCode = ctx.statusCode;
    });

    it("responds with 403", () => expect(statusCode).toBe(403));
    it("does not call next", () => expect(nextCalled).toBe(false));
  });

  describe("when role is EDITOR", () => {
    let nextCalled: boolean;

    beforeAll(() => {
      const req = makeReq({ projectRole: "EDITOR" });
      const { res } = makeRes();
      nextCalled = false;
      requireWriteAccess()(req, res, () => { nextCalled = true; });
    });

    it("calls next", () => expect(nextCalled).toBe(true));
  });

  describe("when role is OWNER", () => {
    let nextCalled: boolean;

    beforeAll(() => {
      const req = makeReq({ projectRole: "OWNER" });
      const { res } = makeRes();
      nextCalled = false;
      requireWriteAccess()(req, res, () => { nextCalled = true; });
    });

    it("calls next", () => expect(nextCalled).toBe(true));
  });
});

// ── requireProjectAccess — no projectId ───────────────────────────────────────

describe("requireProjectAccess", () => {
  describe("when no projectId can be resolved from path or params", () => {
    let nextCalled: boolean;

    beforeAll(async () => {
      vi.clearAllMocks();
      const req = makeReq({ params: {}, originalUrl: "/api/other", url: "/other" });
      const { res } = makeRes();
      nextCalled = false;
      await requireProjectAccess()(req, res, () => { nextCalled = true; });
    });

    it("calls next without blocking", () => expect(nextCalled).toBe(true));
  });

  // ── no user ───────────────────────────────────────────────────────────────

  describe("when projectId is present but req.user is not set", () => {
    let statusCode: number;

    beforeAll(async () => {
      vi.clearAllMocks();
      const req = makeReq({
        params: { projectId: "proj-1" } as Record<string, string>,
        user: undefined,
      });
      const { res, ctx } = makeRes();
      await requireProjectAccess()(req, res, () => {});
      statusCode = ctx.statusCode;
    });

    it("responds with 401", () => expect(statusCode).toBe(401));
  });

  // ── Neo4j confirms OWNER ──────────────────────────────────────────────────

  describe("when Neo4j confirms the user is OWNER", () => {
    let req: InterfaceAuthRequest;
    let nextCalled: boolean;

    beforeAll(async () => {
      vi.clearAllMocks();
      const session = makeSession([{ isOwner: true, role: null }]);
      mockGetDriver.mockReturnValue({ session: () => session } as any);

      req = makeReq({
        params: { projectId: "proj-1" } as Record<string, string>,
        user: { id: "owner-id", email: "o@layla.io", role: "editor" as const } as any,
      });
      const { res } = makeRes();
      nextCalled = false;
      await requireProjectAccess()(req, res, () => { nextCalled = true; });
    });

    it("calls next", () => expect(nextCalled).toBe(true));
    it("sets req.projectRole to OWNER", () => expect(req.projectRole).toBe("OWNER"));
  });

  // ── Neo4j confirms EDITOR ─────────────────────────────────────────────────

  describe("when Neo4j confirms the user is EDITOR", () => {
    let req: InterfaceAuthRequest;
    let nextCalled: boolean;

    beforeAll(async () => {
      vi.clearAllMocks();
      const session = makeSession([{ isOwner: false, role: "EDITOR" }]);
      mockGetDriver.mockReturnValue({ session: () => session } as any);

      req = makeReq({
        params: { projectId: "proj-2" } as Record<string, string>,
        user: { id: "editor-id", email: "e@layla.io", role: "editor" as const } as any,
      });
      const { res } = makeRes();
      nextCalled = false;
      await requireProjectAccess()(req, res, () => { nextCalled = true; });
    });

    it("calls next", () => expect(nextCalled).toBe(true));
    it("sets req.projectRole to EDITOR", () => expect(req.projectRole).toBe("EDITOR"));
  });

  // ── fallback: no record in Neo4j, no bearer token ─────────────────────────

  describe("when Neo4j has no record and no bearer token is available", () => {
    let statusCode: number;
    let nextCalled: boolean;

    beforeAll(async () => {
      vi.clearAllMocks();
      const session = makeSession([]);
      mockGetDriver.mockReturnValue({ session: () => session } as any);

      const req = makeReq({
        params: { projectId: "proj-3" } as Record<string, string>,
        headers: {},
        user: { id: "u1", email: "u@layla.io", role: "viewer" as const } as any,
      });
      const { res, ctx } = makeRes();
      nextCalled = false;
      await requireProjectAccess()(req, res, () => { nextCalled = true; });
      statusCode = ctx.statusCode;
    });

    it("responds with 403", () => expect(statusCode).toBe(403));
    it("does not call next", () => expect(nextCalled).toBe(false));
  });
});
