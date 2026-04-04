import jwt from "jsonwebtoken";
import type { VerifyOptions } from "jsonwebtoken";
import type JwtPayloadCustom from "@/interfaces/auth/JwtPayloadCustom";
import { config } from "@/config/env";

/**
 * Synchronously verifies an access token using the primary JWT secret.
 * Throws an error (e.g., `TokenExpiredError`, `JsonWebTokenError`) if invalid.
 */
export const verifyAccessJWTToken = (token: string): JwtPayloadCustom => {
  return jwt.verify(token, config.jwt.secret, {
    algorithms: ["HS256"],
  } as VerifyOptions) as JwtPayloadCustom;
};
