import { Response, NextFunction } from "express";
import { verifyAccessJWTToken } from "@/utils/ManageJWT";
import InterfaceAuthRequest from "@/interfaces/auth/AuthRequest";

/**
 * JWT TOken is required to access the route
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
 * Dont thrown an error if token is empty
 */
export const MiddlewareptionalAuth = (
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
    // Ignore errors on optional auth
  }

  next();
};

/**
 * Verify roles of user
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
