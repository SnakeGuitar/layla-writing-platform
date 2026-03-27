import type { Response, NextFunction } from "express";
import { verifyAccessJWTToken } from "@/utils/ManageJWT";
import type InterfaceAuthRequest from "@/interfaces/auth/AuthRequest";

/**
 * Requires a valid Bearer JWT in the `Authorization` header.
 *
 * On success, populates `req.user` with the decoded payload and calls `next()`.
 * Returns **401** for missing, malformed, or expired tokens.
 */
export const MiddlewareAuthenticate = (
  req: InterfaceAuthRequest,
  res: Response,
  next: NextFunction,
): void => {
  const authHeader = req.headers.authorization;

  if (!authHeader) {
    res.status(401).json({ error: "No token provided" });
    return;
  }

  const parts = authHeader.split(" ");
  if (parts.length !== 2 || parts[0] !== "Bearer") {
    res.status(401).json({ error: "Invalid token format" });
    return;
  }

  const token = parts[1]!;

  try {
    const decoded = verifyAccessJWTToken(token);
    req.user = decoded;
    next();
  } catch (error) {
    if (error instanceof Error) {
      if (error.name === "TokenExpiredError") {
        res.status(401).json({ error: "Token expired" });
        return;
      }
      if (error.name === "JsonWebTokenError") {
        res.status(401).json({ error: "Invalid token" });
        return;
      }
    }
    res.status(401).json({ error: "Unauthorized" });
  }
};

/**
 * Optionally reads a Bearer JWT from the `Authorization` header.
 *
 * If present and valid, populates `req.user`. Otherwise calls `next()` silently.
 * Never blocks the request — use this on public routes that may benefit from
 * knowing the caller's identity.
 */
export const MiddlewareOptionalAuth = (
  req: InterfaceAuthRequest,
  res: Response,
  next: NextFunction,
): void => {
  const authHeader = req.headers.authorization;

  if (!authHeader) {
    next();
    return;
  }

  const parts = authHeader.split(" ");
  if (parts.length !== 2 || parts[0] !== "Bearer") {
    next();
    return;
  }

  const token = parts[1]!;

  try {
    const decoded = verifyAccessJWTToken(token);
    req.user = decoded;
  } catch {
    // Ignore errors on optional auth — caller is treated as unauthenticated
  }

  next();
};

/**
 * Enforces role-based access control.
 *
 * Must be used after {@link MiddlewareAuthenticate}. Returns **401** if
 * `req.user` is not set and **403** if the user's role is not in `allowedRoles`.
 *
 * @param allowedRoles - One or more role strings permitted to access the route.
 */
export const MiddlewareRequireRole = (...allowedRoles: string[]) => {
  return (
    req: InterfaceAuthRequest,
    res: Response,
    next: NextFunction,
  ): void => {
    if (!req.user) {
      res.status(401).json({ error: "Unauthorized" });
      return;
    }

    if (!req.user.role || !allowedRoles.includes(req.user.role)) {
      res.status(403).json({ error: "Forbidden" });
      return;
    }

    next();
  };
};
