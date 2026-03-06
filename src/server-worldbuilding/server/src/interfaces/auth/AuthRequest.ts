import { Request } from "express";
import InterfaceJwtPayloadCustom from "@/interfaces/auth/JwtPayloadCustom";

export default interface InterfaceAuthRequest extends Request {
  user?: InterfaceJwtPayloadCustom;
}
