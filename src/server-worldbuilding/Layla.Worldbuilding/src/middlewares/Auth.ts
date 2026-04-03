import type { Response, NextFunction } from "express";
import type InterfaceAuthRequest from "@/interfaces/auth/AuthRequest";
import { TokenExpiredError, JsonWebTokenError } from "jsonwebtoken";
import { verifyAccessJWTToken } from "@/utils/ManageJWT";

/**
 * Extracts the Bearer token from the Authorization header.
 * Returns the token string or null if the header is missing / malformed.
 */
const extractBearerToken = (authHeader: string | undefined): string | null => {
  if (!authHeader) return null;
  const [scheme, token] = authHeader.split(" ");
  return scheme === "Bearer" && token ? token : null;
};

/**
 * Maps a JWT verification error to its HTTP response.
 * Returns true if the error was handled, false if it was unknown.
 */
const handleJWTError = (error: unknown, res: Response): boolean => {
  if (error instanceof TokenExpiredError) {
    res.status(401).json({ error: "Token expired" });
    return true;
  }
  if (error instanceof JsonWebTokenError) {
    res.status(401).json({ error: "Invalid token" });
    return true;
  }
  return false;
};

export const MiddlewareAuthenticate = (
  req: InterfaceAuthRequest,
  res: Response,
  next: NextFunction,
): void => {
  const token = extractBearerToken(req.headers.authorization);

  if (!token) {
    res.status(401).json({ error: "No token provided" });
    return;
  }

  try {
    req.user = verifyAccessJWTToken(token);
    next();
  } catch (error) {
    if (!handleJWTError(error, res)) {
      res.status(401).json({ error: "Unauthorized" });
    }
  }
};

export const MiddlewareOptionalAuth = (
  req: InterfaceAuthRequest,
  _res: Response,
  next: NextFunction,
): void => {
  const token = extractBearerToken(req.headers.authorization);

  if (token) {
    try {
      req.user = verifyAccessJWTToken(token);
    } catch {
      // Treated as unauthenticated — not an error on optional routes
    }
  }

  next();
};

export const MiddlewareRequireRole =
  (...allowedRoles: string[]) =>
  (req: InterfaceAuthRequest, res: Response, next: NextFunction): void => {
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
