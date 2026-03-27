import type { Request } from "express";
import type InterfaceJwtPayloadCustom from "@/interfaces/auth/JwtPayloadCustom";

/**
 * Extends the standard Express Request object to include the authenticated
 * user's decoded JWT payload. Populated by {@link MiddlewareAuthenticate}.
 */
export default interface InterfaceAuthRequest extends Request {
  user?: InterfaceJwtPayloadCustom;
}
