import jwt, { SignOptions, VerifyOptions } from "jsonwebtoken";
import process from "node:process";
import JwtPayloadCustom from "@/interfaces/auth/JwtPayloadCustom";
import TokenPair from "@/interfaces/auth/TokenPair";

export const generatejwtTokens = (payload: JwtPayloadCustom): TokenPair => {
  const accessToken = jwt.sign(payload, process.env.JWT_SECRET!, {
    expiresIn: process.env.JWT_ACCESS_TOKEN_EXPIRY,
  } as SignOptions);

  const refreshToken = jwt.sign(
    { id: payload.id },
    process.env.JWT_SECRET_REFRESH!,
    {
      expiresIn: process.env.JWT_REFRESH_TOKEN_EXPIRY,
    } as SignOptions,
  );

  return { accessToken, refreshToken };
};

export const generateAccessJWTToken = (payload: JwtPayloadCustom): string => {
  return jwt.sign(payload, process.env.JWT_SECRET!, {
    expiresIn: process.env.JWT_ACCESS_TOKEN_EXPIRY,
  } as SignOptions);
};

export const generateVerificationJWTToken = (
  payload: JwtPayloadCustom,
  expiresIn: string = "1h",
): string => {
  return jwt.sign(payload, process.env.JWT_SECRET!, {
    expiresIn,
  } as SignOptions);
};

export const verifyAccessJWTToken = (token: string): JwtPayloadCustom => {
  return jwt.verify(token, process.env.JWT_SECRET!, {
    algorithms: ["HS256"],
  } as VerifyOptions) as JwtPayloadCustom;
};

export const verifyRefreshToken = (token: string): { id: string } => {
  return jwt.verify(token, process.env.JWT_REFRESH_SECRET!, {
    algorithms: ["HS256"],
  } as VerifyOptions) as { id: string };
};

export const verifyVerificationJWTToken = (token: string): JwtPayloadCustom => {
  return jwt.verify(token, process.env.JWT_SECRET!, {
    algorithms: ["HS256"],
  } as VerifyOptions) as JwtPayloadCustom;
};

export const decodeJWTToken = (token: string): JwtPayloadCustom | null => {
  const decoded = jwt.decode(token);
  return decoded as JwtPayloadCustom | null;
};

export const isJWTExpiringSoon = (
  token: string,
  thresholdMinutes: number = 5,
): boolean => {
  const decoded = decodeJWTToken(token);
  if (!decoded || !decoded.exp) return true;

  const expirationDate = new Date(decoded.exp * 1000);
  const now = new Date();
  const threshold = thresholdMinutes * 60 * 1000;

  return expirationDate.getTime() - now.getTime() < threshold;
};
