import { describe, it, expect, vi, beforeAll } from "vitest";
import type { Response, NextFunction } from "express";
import type InterfaceAuthRequest from "@/interfaces/auth/AuthRequest";
import { TokenExpiredError, JsonWebTokenError } from "jsonwebtoken";

vi.mock("@/utils/ManageJWT");
vi.mock("@/config/env", () => ({
  config: {
    jwt: { secret: "test-secret" },
    coreApiUrl: "http://localhost:7166",
    port: 3000,
  },
}));

import {
  MiddlewareAuthenticate,
  MiddlewareOptionalAuth,
  MiddlewareRequireRole,
} from "@/middlewares/Auth";
import { verifyAccessJWTToken } from "@/utils/ManageJWT";

const mockVerify = vi.mocked(verifyAccessJWTToken);

// ── helpers ───────────────────────────────────────────────────────────────────

function makeReq(authHeader?: string, user?: InterfaceAuthRequest["user"]): InterfaceAuthRequest {
  return { headers: { authorization: authHeader }, user } as unknown as InterfaceAuthRequest;
}

function makeRes() {
  const ctx = { statusCode: 0, body: null as unknown };
  const res = {
    status(s: number) { ctx.statusCode = s; return res; },
    json(b: unknown) { ctx.body = b; return res; },
  } as unknown as Response;
  return { res, ctx };
}

// ── MiddlewareAuthenticate — no Authorization header ──────────────────────────

describe("MiddlewareAuthenticate", () => {
  describe("when no Authorization header is present", () => {
    let statusCode: number;
    let body: unknown;
    let nextCalled: boolean;

    beforeAll(() => {
      vi.clearAllMocks();
      const req = makeReq(undefined);
      const { res, ctx } = makeRes();
      nextCalled = false;
      MiddlewareAuthenticate(req, res, () => { nextCalled = true; });
      statusCode = ctx.statusCode;
      body = ctx.body;
    });

    it("responds with 401", () => expect(statusCode).toBe(401));
    it("includes no-token error message", () => expect((body as any).error).toBe("No token provided"));
    it("does not call next", () => expect(nextCalled).toBe(false));
  });

  // ── non-Bearer scheme ─────────────────────────────────────────────────────

  describe("when Authorization header uses Basic scheme", () => {
    let statusCode: number;
    let nextCalled: boolean;

    beforeAll(() => {
      vi.clearAllMocks();
      const req = makeReq("Basic dXNlcjpwYXNz");
      const { res, ctx } = makeRes();
      nextCalled = false;
      MiddlewareAuthenticate(req, res, () => { nextCalled = true; });
      statusCode = ctx.statusCode;
    });

    it("responds with 401", () => expect(statusCode).toBe(401));
    it("does not call next", () => expect(nextCalled).toBe(false));
  });

  // ── expired token ─────────────────────────────────────────────────────────

  describe("when token is expired", () => {
    let statusCode: number;
    let body: unknown;
    let nextCalled: boolean;

    beforeAll(() => {
      vi.clearAllMocks();
      mockVerify.mockImplementation(() => { throw new TokenExpiredError("jwt expired", new Date()); });
      const req = makeReq("Bearer expired.jwt.token");
      const { res, ctx } = makeRes();
      nextCalled = false;
      MiddlewareAuthenticate(req, res, () => { nextCalled = true; });
      statusCode = ctx.statusCode;
      body = ctx.body;
    });

    it("responds with 401", () => expect(statusCode).toBe(401));
    it("includes expired-token error message", () => expect((body as any).error).toBe("Token expired"));
    it("does not call next", () => expect(nextCalled).toBe(false));
  });

  // ── invalid signature ─────────────────────────────────────────────────────

  describe("when token has an invalid signature", () => {
    let statusCode: number;
    let body: unknown;

    beforeAll(() => {
      vi.clearAllMocks();
      mockVerify.mockImplementation(() => { throw new JsonWebTokenError("invalid signature"); });
      const req = makeReq("Bearer bad.signature.token");
      const { res, ctx } = makeRes();
      MiddlewareAuthenticate(req, res, () => {});
      statusCode = ctx.statusCode;
      body = ctx.body;
    });

    it("responds with 401", () => expect(statusCode).toBe(401));
    it("includes invalid-token error message", () => expect((body as any).error).toBe("Invalid token"));
  });

  // ── unexpected error ──────────────────────────────────────────────────────

  describe("when verify throws an unexpected error", () => {
    let statusCode: number;
    let body: unknown;

    beforeAll(() => {
      vi.clearAllMocks();
      mockVerify.mockImplementation(() => { throw new Error("unexpected"); });
      const req = makeReq("Bearer unknown.error");
      const { res, ctx } = makeRes();
      MiddlewareAuthenticate(req, res, () => {});
      statusCode = ctx.statusCode;
      body = ctx.body;
    });

    it("responds with 401", () => expect(statusCode).toBe(401));
    it("includes generic unauthorized message", () => expect((body as any).error).toBe("Unauthorized"));
  });

  // ── valid token ───────────────────────────────────────────────────────────

  describe("when token is valid", () => {
    const payload = { id: "user-1", email: "a@b.com", role: "editor" as const };
    let req: InterfaceAuthRequest;
    let nextCalled: boolean;

    beforeAll(() => {
      vi.clearAllMocks();
      mockVerify.mockReturnValue(payload as any);
      req = makeReq("Bearer valid.jwt.token");
      const { res } = makeRes();
      nextCalled = false;
      MiddlewareAuthenticate(req, res, () => { nextCalled = true; });
    });

    it("calls next", () => expect(nextCalled).toBe(true));
    it("sets req.user", () => expect(req.user).toEqual(payload));
    it("sets req.user.id", () => expect(req.user?.id).toBe("user-1"));
    it("sets req.user.email", () => expect(req.user?.email).toBe("a@b.com"));
  });
});

// ── MiddlewareOptionalAuth ────────────────────────────────────────────────────

describe("MiddlewareOptionalAuth", () => {
  describe("when no Authorization header", () => {
    let req: InterfaceAuthRequest;
    let nextCalled: boolean;

    beforeAll(() => {
      vi.clearAllMocks();
      req = makeReq(undefined);
      const { res } = makeRes();
      nextCalled = false;
      MiddlewareOptionalAuth(req, res, () => { nextCalled = true; });
    });

    it("calls next", () => expect(nextCalled).toBe(true));
    it("does not set req.user", () => expect(req.user).toBeUndefined());
  });

  describe("when a valid token is provided", () => {
    const payload = { id: "u2", email: "b@c.com", role: "viewer" as const };
    let req: InterfaceAuthRequest;

    beforeAll(() => {
      vi.clearAllMocks();
      mockVerify.mockReturnValue(payload as any);
      req = makeReq("Bearer valid");
      const { res } = makeRes();
      MiddlewareOptionalAuth(req, res, () => {});
    });

    it("sets req.user", () => expect(req.user).toEqual(payload));
  });

  describe("when the token is invalid", () => {
    let req: InterfaceAuthRequest;
    let nextCalled: boolean;

    beforeAll(() => {
      vi.clearAllMocks();
      mockVerify.mockImplementation(() => { throw new JsonWebTokenError("bad"); });
      req = makeReq("Bearer bad");
      const { res } = makeRes();
      nextCalled = false;
      MiddlewareOptionalAuth(req, res, () => { nextCalled = true; });
    });

    it("still calls next (optional route — never blocks)", () => expect(nextCalled).toBe(true));
    it("does not set req.user", () => expect(req.user).toBeUndefined());
  });
});

// ── MiddlewareRequireRole ─────────────────────────────────────────────────────

describe("MiddlewareRequireRole", () => {
  describe("when req.user is not set", () => {
    let statusCode: number;
    let nextCalled: boolean;

    beforeAll(() => {
      const req = makeReq(undefined, undefined);
      const { res, ctx } = makeRes();
      nextCalled = false;
      MiddlewareRequireRole("admin")(req, res, () => { nextCalled = true; });
      statusCode = ctx.statusCode;
    });

    it("responds with 401", () => expect(statusCode).toBe(401));
    it("does not call next", () => expect(nextCalled).toBe(false));
  });

  describe("when user role is not in the allowed list", () => {
    let statusCode: number;
    let nextCalled: boolean;

    beforeAll(() => {
      const req = makeReq(undefined, { id: "u1", email: "a@b.com", role: "viewer" as const } as any);
      const { res, ctx } = makeRes();
      nextCalled = false;
      MiddlewareRequireRole("admin", "editor")(req, res, () => { nextCalled = true; });
      statusCode = ctx.statusCode;
    });

    it("responds with 403", () => expect(statusCode).toBe(403));
    it("does not call next", () => expect(nextCalled).toBe(false));
  });

  describe("when user role matches the required role", () => {
    let nextCalled: boolean;

    beforeAll(() => {
      const req = makeReq(undefined, { id: "u2", email: "b@c.com", role: "editor" as const } as any);
      const { res } = makeRes();
      nextCalled = false;
      MiddlewareRequireRole("editor")(req, res, () => { nextCalled = true; });
    });

    it("calls next", () => expect(nextCalled).toBe(true));
  });

  describe("when user role matches one of multiple allowed roles", () => {
    let nextCalled: boolean;

    beforeAll(() => {
      const req = makeReq(undefined, { id: "u3", email: "c@d.com", role: "admin" as const } as any);
      const { res } = makeRes();
      nextCalled = false;
      MiddlewareRequireRole("viewer", "editor", "admin")(req, res, () => { nextCalled = true; });
    });

    it("calls next", () => expect(nextCalled).toBe(true));
  });
});
