import neo4j, { Driver } from "neo4j-driver";
import { config } from "@/config/env";

/** Lazily-initialized singleton Neo4j driver instance. */
let driver: Driver | null = null;

/**
 * Returns the singleton Neo4j {@link Driver}, creating it on first call.
 *
 * The driver maintains an internal connection pool (`maxConnectionPoolSize: 50`).
 * Each operation should open its own session and close it in a `finally` block.
 *
 * @throws {Error} If the initialization fails.
 */
export const getNeo4jDriver = (): Driver => {
	if (!driver) {
		try {
			driver = neo4j.driver(
				config.neo4j.uri,
				neo4j.auth.basic(config.neo4j.username, config.neo4j.password),
				{
					maxConnectionPoolSize: 50,
					connectionAcquisitionTimeout: 5_000, // ms — evita bloqueos indefinidos
				},
			);
		} catch (err) {
			throw new Error(`[Neo4j] Failed to initialize driver.\n`, { cause: err });
		}
	}
	return driver;
};

const MAX_RETRIES = 5;
const RETRY_DELAY_MS = 3000;

/**
 * Verifies that the Neo4j driver can reach the server.
 * Called during application bootstrap to fail fast if Neo4j is unavailable.
 *
 * Retries up to {@link MAX_RETRIES} times with a {@link RETRY_DELAY_MS} ms
 * backoff between attempts. Throws if all attempts fail.
 *
 * @throws {Error} If connection fails
 */
export const verifyNeo4jConnection = async (): Promise<void> => {
	for (let attempt = 1; attempt <= MAX_RETRIES; attempt++) {
		try {
			await getNeo4jDriver().verifyConnectivity();
			console.log("Neo4j connected.");
			return;
		} catch (error) {
			console.error(
				`Neo4j: attempt ${attempt}/${MAX_RETRIES} failed.\n`,
				error,
			);
			if (attempt === MAX_RETRIES) throw error;
			await new Promise((resolve) => setTimeout(resolve, RETRY_DELAY_MS));
		}
	}
};

/**
 * Closes the Neo4j driver and releases all pooled connections.
 * Called during graceful shutdown.
 */
export const closeNeo4jDriver = async (): Promise<void> => {
	if (driver) {
		await driver.close();
		driver = null;
		console.log("[Neo4j] Driver closed.");
	}
};
