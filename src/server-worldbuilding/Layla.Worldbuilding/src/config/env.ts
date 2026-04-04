import process from "node:process";
import "dotenv/config";

/**
 * Required environment variables.
 * The application will throw at startup if any of these are missing.
 */
const required = [
  "JWT_SECRET",
  "JWT_SECRET_REFRESH",
  "MONGODB_URI",
  "NEO4J_URI",
  "NEO4J_USERNAME",
  "NEO4J_PASSWORD",
  "RABBITMQ_URL",
] as const;

/**
 * Verify if some secret is empty or missing
 */
const missing = required.filter((key) => !process.env[key]?.trim());
if (missing.length > 0) {
  throw new Error(
    `Missing or empty required environment variables: ${missing.join(", ")}`,
  );
}

/**
 * Typed, validated application configuration derived from environment variables.
 *
 * All required keys are guaranteed to be present at this point; access is safe
 * via the non-null assertion operator (`!`).
 */
export const config = {
  /** HTTP server port. Defaults to `3000` if `PORT` is not set. */
  port: Number(process.env.PORT) || 3000,

  jwt: {
    secret: process.env.JWT_SECRET!,
    secretRefresh: process.env.JWT_SECRET_REFRESH!,
    /** Default: `"1440m"` (24 hours). */
    accessExpiry: process.env.JWT_ACCESS_TOKEN_EXPIRY ?? "1440m",
    /** Default: `"7d"`. */
    refreshExpiry: process.env.JWT_REFRESH_TOKEN_EXPIRY ?? "7d",
  },

  mongo: {
    uri: process.env.MONGODB_URI!,
  },

  neo4j: {
    uri: process.env.NEO4J_URI!,
    username: process.env.NEO4J_USERNAME!,
    password: process.env.NEO4J_PASSWORD!,
  },

  rabbitmq: {
    url: process.env.RABBITMQ_URL!,
    /** Default: `"worldbuilding.events"`. */
    exchange: process.env.RABBITMQ_EXCHANGE ?? "worldbuilding.events",
    /** Default: `"worldbuilding.node.queue"`. */
    queue: process.env.RABBITMQ_QUEUE ?? "worldbuilding.node.queue",
  },
} as const;
