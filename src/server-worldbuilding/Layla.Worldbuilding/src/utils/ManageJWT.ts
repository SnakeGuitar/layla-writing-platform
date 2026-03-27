import jwt from "jsonwebtoken";
import type { SignOptions, VerifyOptions } from "jsonwebtoken";
import type JwtPayloadCustom from "@/interfaces/auth/JwtPayloadCustom";
import type TokenPair from "@/interfaces/auth/TokenPair";
import process from "node:process";

/**
 * Generates both an access token and a refresh token for the given payload.
 *
 * Expirations and secrets are sourced from the application config
 * (ultimately from environment variables).
 */
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

/**
 * Generates only a short-lived access token.
 */
export const generateAccessJWTToken = (payload: JwtPayloadCustom): string => {
  return jwt.sign(payload, process.env.JWT_SECRET!, {
    expiresIn: process.env.JWT_ACCESS_TOKEN_EXPIRY,
  } as SignOptions);
};

/**
 * Generates a verification token (e.g., for email confirmation),
 * allowing a custom expiration string. Default is `"1h"`.
 */
export const generateVerificationJWTToken = (
  payload: JwtPayloadCustom,
  expiresIn: string = "1h",
): string => {
  return jwt.sign(payload, process.env.JWT_SECRET!, {
    expiresIn,
  } as SignOptions);
};

/**
 * Synchronously verifies an access token using the primary JWT secret.
 * Throws an error (e.g., `TokenExpiredError`, `JsonWebTokenError`) if invalid.
 */
export const verifyAccessJWTToken = (token: string): JwtPayloadCustom => {
  return jwt.verify(token, process.env.JWT_SECRET!, {
    algorithms: ["HS256"],
  } as VerifyOptions) as JwtPayloadCustom;
};

/**
 * Synchronously verifies a refresh token using the refresh JWT secret.
 * Returns an object containing only the user `id`.
 */
export const verifyRefreshToken = (token: string): { id: string } => {
  return jwt.verify(token, process.env.JWT_SECRET_REFRESH!, {
    algorithms: ["HS256"],
  } as VerifyOptions) as { id: string };
};

/**
 * Synchronously verifies a verification token using the primary JWT secret.
 * Throws if the token is invalid or expired.
 */
export const verifyVerificationJWTToken = (token: string): JwtPayloadCustom => {
  return jwt.verify(token, process.env.JWT_SECRET!, {
    algorithms: ["HS256"],
  } as VerifyOptions) as JwtPayloadCustom;
};

/**
 * Decodes a token without verifying its signature.
 * Useful for inspecting payload headers or claims before validation.
 */
export const decodeJWTToken = (token: string): JwtPayloadCustom | null => {
  const decoded = jwt.decode(token);
  return decoded as JwtPayloadCustom | null;
};

/**
 * Checks if a token will expire within the given `thresholdMinutes` (default: 5).
 * Returns `true` if the token is already expired, expiring soon, or unparseable.
 */
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
