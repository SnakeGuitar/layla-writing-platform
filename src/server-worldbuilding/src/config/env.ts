import process from "node:process";
import { config as loadEnv } from "dotenv";

// In local development the env file is .env.development.
// In Docker/production the variables are injected directly — dotenv is a no-op.
loadEnv({ path: ".env.development", override: false });
loadEnv({ override: false }); // fallback to .env if present

/**
 * Required environment variables.
 * The application will throw at startup if any of these are missing.
 */
const required = [
	"ALLOWED_ORIGINS",
	"JWT_SECRET",
	"JWT_SECRET_REFRESH",
	"JWT_ACCESS_TOKEN_EXPIRY",
	"JWT_REFRESH_TOKEN_EXPIRY",
	"MONGODB_URI",
	"NEO4J_URI",
	"NEO4J_USERNAME",
	"NEO4J_PASSWORD",
	"RABBITMQ_URL",
] as const;

/**
 * Variables that must additionally meet a minimum-length threshold so a
 * weak secret (e.g. `"a"`) is rejected at startup, not at first auth attempt.
 *
 * 32 bytes is the recommended minimum entropy for HS256 JWT signing keys.
 */
const MIN_SECRET_LENGTH = 32;
const minLengthRequired: ReadonlySet<string> = new Set([
	"JWT_SECRET",
	"JWT_SECRET_REFRESH",
]);

const missing: string[] = [];
const tooShort: string[] = [];

for (const key of required) {
	const value = process.env[key]?.trim();
	if (!value) {
		missing.push(key);
		continue;
	}
	if (minLengthRequired.has(key) && value.length < MIN_SECRET_LENGTH) {
		tooShort.push(key);
	}
}

if (missing.length > 0) {
	throw new Error(
		`Missing or empty required environment variables:\n\t ${missing.join(", ")}`,
	);
}
if (tooShort.length > 0) {
	throw new Error(
		`The following secrets must be at least ${MIN_SECRET_LENGTH} characters: ${tooShort.join(", ")}`,
	);
}

/**
 * Typed, validated application configuration derived from environment variables.
 *
 * All required keys are guaranteed to be present at this point; access is safe
 * via the non-null assertion operator (`!`).
 */
export const config = {
	/** Allowed origin for CORS policies. */
	allowedOrigins: process.env["ALLOWED_ORIGINS"]!,

	/** HTTP server port. Defaults to `3000` if `PORT` is not set. */
	port: process.env["PORT"]!,

	jwt: {
		secret: process.env["JWT_SECRET"]!,
		secretRefresh: process.env["JWT_SECRET_REFRESH"]!,
		/** Default: `"1440m"` (24 hours). */
		accessExpiry: process.env["JWT_ACCESS_TOKEN_EXPIRY"]!,
		/** Default: `"7d"`. */
		refreshExpiry: process.env["JWT_REFRESH_TOKEN_EXPIRY"]!,
	},

	mongo: {
		uri: process.env["MONGODB_URI"]!,
	},

	neo4j: {
		uri: process.env["NEO4J_URI"]!,
		username: process.env["NEO4J_USERNAME"]!,
		password: process.env["NEO4J_PASSWORD"]!,
	},

	rabbitmq: {
		url: process.env["RABBITMQ_URL"]!,
		/** Default: `"worldbuilding.events"`. */
		exchange: process.env["RABBITMQ_EXCHANGE"] ?? "worldbuilding.events",
		/** Default: `"worldbuilding.node.queue"`. */
		queue: process.env["RABBITMQ_QUEUE"] ?? "worldbuilding.node.queue",
	},
} as const;
