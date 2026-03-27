import type { JwtPayload } from "jsonwebtoken";

export type UserRole = "admin" | "editor" | "viewer";
/**
 * Expected structure of the decoded JWT payload issued by server-core.
 * Contains core identity and authorization claims.
 */
export default interface InterfaceJwtPayloadCustom extends JwtPayload {
  id: string;
  email: string;
  role: UserRole;
}
