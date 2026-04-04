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
      throw new Error(`[Neo4j] Failed to initialize driver`, { cause: err });
    }
  }
  return driver;
};

/**
 * Verifies that the Neo4j driver can reach the server.
 * Called during application bootstrap to fail fast if Neo4j is unavailable.
 *
 * @throws {Error} If connection fails
 */
export const verifyNeo4jConnection = async (): Promise<void> => {
  await getNeo4jDriver().verifyConnectivity();
  console.log("Neo4j connected");
};

/**
 * Closes the Neo4j driver and releases all pooled connections.
 * Called during graceful shutdown.
 */
export const closeNeo4jDriver = async (): Promise<void> => {
  if (driver) {
    await driver.close();
    driver = null;
    console.log("[Neo4j] Driver closed");
  }
};
