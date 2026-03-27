import type { JwtPayload } from "jsonwebtoken";

/**
 * Expected structure of the decoded JWT payload issued by server-core.
 * Contains core identity and authorization claims.
 */
export default interface InterfaceJwtPayloadCustom extends JwtPayload {
  id: string;
  email: string;
  role: string;
}
