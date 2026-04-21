import express from "express";
import type { Request, Response, NextFunction } from "express";
import helmet from "helmet";
import cors from "cors";
import swaggerUi from "swagger-ui-express";
import { config } from "@/config/env";
import { connectMongoDB } from "@/db/mongoose";
import { verifyNeo4jConnection, closeNeo4jDriver } from "@/db/neo4j";
import {
	startProjectCreatedConsumer,
	closeRabbitMQ,
} from "@/consumers/projectCreated.consumer";
import { startNeo4jSyncWorker } from "@/workers/neo4jSyncWorker";
import { apiLimiter } from "@/middlewares/RateLimiter";
import { swaggerSpec } from "@/docs/swagger";

import ManuscriptsRouter from "@/routes/Manuscripts";
import WikiRouter from "@/routes/Wiki";
import GraphRouter from "@/routes/Graph";

const app = express();

// Security headers (XSS, clickjacking, MIME sniffing, etc.)
app.use(helmet());

// CORS — only allow traffic from server-core and the desktop/web clients
const allowedOrigins = (config.allowedOrigins ?? "")
	.split(",")
	.map((o) => o.trim())
	.filter(Boolean);

app.use(
	cors({
		origin: allowedOrigins.length > 0 ? allowedOrigins : false,
		methods: ["GET", "POST", "PUT", "DELETE"],
		allowedHeaders: ["Authorization", "Content-Type"],
	}),
);

app.use(express.json({ limit: "10mb" }));
app.use(apiLimiter);

app.get("/health", (_req, res) => res.send("OK"));

app.use("/api/manuscripts", ManuscriptsRouter);
app.use("/api/wiki", WikiRouter);
app.use("/api/graph", GraphRouter);

app.use("/api-docs", swaggerUi.serve, swaggerUi.setup(swaggerSpec));
app.get("/api-docs.json", (_req, res) => res.json(swaggerSpec));

/** Global error handler — Express 5 forwards thrown errors to this handler via asyncHandler. */
app.use((err: unknown, req: Request, res: Response, next: NextFunction) => {
	// Log full context server-side; never leak it to the client.
	console.error(
		`[GlobalError] ${req.method} ${req.originalUrl}`,
		err instanceof Error ? err.stack : err,
	);

	if (res.headersSent) {
		// Express requires delegating to the default handler when the
		// response has already started streaming.
		next(err);
		return;
	}

	res.status(500).json({ error: "Internal server error" });
});

/**
 * Application bootstrap function.
 *
 * Connects to all external dependencies in order, then starts the HTTP server
 * and background workers. Registers SIGTERM and SIGINT handlers for graceful shutdown.
 */
const bootstrap = async () => {
	await connectMongoDB();
	await verifyNeo4jConnection();
	await startProjectCreatedConsumer();

	startNeo4jSyncWorker();

	const server = app.listen(config.port, () => {
		console.log(`Server running on http://localhost:${config.port}`);
	});

	const shutdown = async () => {
		console.log("Shutting down server...");
		await new Promise<void>((resolve) => server.close(() => resolve()));
		await closeRabbitMQ();
		await closeNeo4jDriver();
		process.exit(0);
	};

	process.on("SIGTERM", shutdown);
	process.on("SIGINT", shutdown);
};

bootstrap().catch((err) => {
	console.error("Failed to start server:", err);
	process.exit(1);
});
