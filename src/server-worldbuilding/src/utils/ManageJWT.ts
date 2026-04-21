import jwt from "jsonwebtoken";
import type { VerifyOptions } from "jsonwebtoken";
import type JwtPayloadCustom from "@/interfaces/auth/JwtPayloadCustom";
import { config } from "@/config/env";

/**
 * Synchronously verifies an access token using the primary JWT secret.
 * Throws an error (e.g., `TokenExpiredError`, `JsonWebTokenError`) if invalid.
 *
 * server-core encodes the user ID as the standard `sub` claim.
 * This function normalises `sub` → `id` so the rest of the worldbuilding
 * service can use `req.user.id` uniformly, regardless of claim name.
 */
export const verifyAccessJWTToken = (token: string): JwtPayloadCustom => {
	const decoded = jwt.verify(token, config.jwt.secret, {
		algorithms: ["HS256"],
	} as VerifyOptions) as Record<string, unknown>;

	// Prefer an explicit 'id' claim; fall back to the standard 'sub' claim that
	// server-core uses. Default to empty string so downstream code never gets undefined.
	const id =
		(decoded["id"] as string | undefined) ??
		(decoded["sub"] as string | undefined) ??
		"";

	return { ...decoded, id } as JwtPayloadCustom;
};
