import { describe, it, expect, vi, beforeAll } from "vitest";
import jwt from "jsonwebtoken";
import { TokenExpiredError, JsonWebTokenError } from "jsonwebtoken";

// Inline — vi.mock is hoisted before const declarations
vi.mock("@/config/env", () => ({
  config: { jwt: { secret: "test-secret-must-be-at-least-32-chars!!" } },
}));

const TEST_SECRET = "test-secret-must-be-at-least-32-chars!!";

import { verifyAccessJWTToken } from "@/utils/ManageJWT";

// ── helpers ───────────────────────────────────────────────────────────────────

function sign(
  payload: Record<string, unknown>,
  secret = TEST_SECRET,
  options: jwt.SignOptions = {},
) {
  return jwt.sign(payload, secret, { algorithm: "HS512", expiresIn: "1h", ...options });
}

// ── valid token — sub claim mapped to id ──────────────────────────────────────

describe("verifyAccessJWTToken", () => {
  describe("when token has a sub claim (server-core standard)", () => {
    let result: any;
    beforeAll(() => {
      result = verifyAccessJWTToken(sign({ sub: "user-1", email: "a@b.com", role: "editor" }));
    });
    it("maps sub to id", () => expect(result.id).toBe("user-1"));
    it("preserves email claim", () => expect(result.email).toBe("a@b.com"));
    it("preserves role claim", () => expect(result.role).toBe("editor"));
  });

  describe("when token has an explicit id claim", () => {
    let result: any;
    beforeAll(() => {
      result = verifyAccessJWTToken(sign({ id: "explicit-id", sub: "sub-id" }));
    });
    it("prefers id over sub", () => expect(result.id).toBe("explicit-id"));
  });

  describe("when token has neither id nor sub", () => {
    let result: any;
    beforeAll(() => {
      result = verifyAccessJWTToken(sign({ email: "a@b.com", role: "viewer" }));
    });
    it("defaults id to empty string", () => expect(result.id).toBe(""));
  });

  describe("when token is expired", () => {
    it("throws TokenExpiredError", () => {
      const token = sign({ sub: "u1" }, TEST_SECRET, { expiresIn: -1 });
      expect(() => verifyAccessJWTToken(token)).toThrow(TokenExpiredError);
    });
  });

  describe("when token was signed with a different secret", () => {
    it("throws JsonWebTokenError", () => {
      const token = sign({ sub: "u1" }, "wrong-secret-must-also-be-at-least-32-chars");
      expect(() => verifyAccessJWTToken(token)).toThrow(JsonWebTokenError);
    });
  });

  describe("when token was signed with a different algorithm", () => {
    it("throws JsonWebTokenError", () => {
      const token = jwt.sign({ sub: "u1" }, TEST_SECRET, { algorithm: "HS256" });
      expect(() => verifyAccessJWTToken(token)).toThrow(JsonWebTokenError);
    });
  });
});
