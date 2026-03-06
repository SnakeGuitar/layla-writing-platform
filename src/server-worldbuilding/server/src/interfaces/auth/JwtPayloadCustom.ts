import { JwtPayload } from "jsonwebtoken";

export default interface InterfaceJwtPayloadCustom extends JwtPayload {
  id: number;
  email: string;
  role: string;
}
