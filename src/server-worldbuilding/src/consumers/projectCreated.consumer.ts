import type { Channel, ChannelModel } from "amqplib";
import amqplib from "amqplib";
import { z } from "zod";
import { v4 as uuidv4 } from "uuid";
import { getNeo4jDriver } from "@/db/neo4j";
import { ManuscriptModel } from "@/models/Manuscript.model";
import { config } from "@/config/env";

/** Zod schema that validates the payload published by server-core. */
const ProjectCreatedPayloadSchema = z.object({
	projectId: z.string().uuid(),
	ownerId: z.string().uuid(),
	title: z.string().min(1).max(200),
	createdAt: z.string(),
});

/** Shape of the payload published by server-core when a project is created. */
type ProjectCreatedPayload = z.infer<typeof ProjectCreatedPayloadSchema>;

/** RabbitMQ routing key this consumer subscribes to. */
const ROUTING_KEY = "project.created";

/**
 * Creates a `:Project` node and a `:User`→`:MEMBER_OF`→`:Project` edge for
 * the owner inside a single Neo4j transaction (idempotent via MERGE).
 *
 * Without the `:User` + `:MEMBER_OF` link the {@link requireProjectAccess}
 * guard would always return 403 because no membership data existed.
 */
const initializeProjectInNeo4j = async (
	payload: ProjectCreatedPayload,
): Promise<void> => {
	const driver = getNeo4jDriver();
	const session = driver.session();
	try {
		await session.executeWrite(async (tx) => {
			await tx.run(
				`MERGE (p:Project { projectId: $projectId })
         ON CREATE SET p.title = $title, p.ownerId = $ownerId, p.createdAt = $createdAt`,
				{
					projectId: payload.projectId,
					title: payload.title,
					ownerId: payload.ownerId,
					createdAt: payload.createdAt,
				},
			);

			await tx.run(
				`MERGE (u:User { id: $ownerId })
         WITH u
         MATCH (p:Project { projectId: $projectId })
         MERGE (u)-[:MEMBER_OF]->(p)`,
				{
					ownerId: payload.ownerId,
					projectId: payload.projectId,
				},
			);
		});
	} finally {
		await session.close();
	}
};

/**
 * Creates a default {@link Manuscript} document in MongoDB for the project if
 * one does not already exist (idempotent).
 *
 * Both `manuscriptId` and `title` are required by the schema.
 * We generate a UUID for `manuscriptId` and use the project title so the
 * document passes Mongoose validation.
 */
const initializeManuscriptInMongo = async (
	projectId: string,
	projectTitle: string,
): Promise<void> => {
	const exists = await ManuscriptModel.exists({ projectId });
	if (!exists) {
		await ManuscriptModel.create({
			manuscriptId: uuidv4(),
			projectId,
			title: projectTitle,
			order: 0,
			chapters: [],
		});
	}
};

/** Module-level references kept for {@link closeRabbitMQ}. */
let _conn: ChannelModel | null = null;
let _channel: Channel | null = null;

/**
 * Connects to RabbitMQ and starts consuming the `project.created` routing key
 * from the configured exchange and queue.
 *
 * For each event, initializes a `:Project` node in Neo4j and an empty
 * `Manuscript` document in MongoDB. Retries the initial connection up to 5 times
 * with a 3-second backoff before throwing.
 *
 * Messages that fail processing are nack'd without requeue.
 */
export const startProjectCreatedConsumer = async (): Promise<void> => {
	const MAX_RETRIES = 5;
	const RETRY_DELAY_MS = 3000;

	for (let attempt = 1; attempt <= MAX_RETRIES; attempt++) {
		try {
			_conn = await amqplib.connect(config.rabbitmq.url);
			break;
		} catch (err) {
			console.error(
				`RabbitMQ: connection attempt ${attempt}/${MAX_RETRIES} failed`,
			);
			if (attempt === MAX_RETRIES)
				throw new Error("Could not connect to RabbitMQ after maximum retries", {
					cause: err,
				});
			await new Promise((resolve) => setTimeout(resolve, RETRY_DELAY_MS));
		}
	}

	if (!_conn) return;

	_channel = await _conn.createChannel();

	await _channel.assertExchange(config.rabbitmq.exchange, "topic", {
		durable: true,
	});
	await _channel.assertQueue(config.rabbitmq.queue, { durable: true });
	await _channel.bindQueue(
		config.rabbitmq.queue,
		config.rabbitmq.exchange,
		ROUTING_KEY,
	);
	_channel.prefetch(1);

	console.log(
		`RabbitMQ consumer active: ${config.rabbitmq.queue} (${ROUTING_KEY})`,
	);

	// Capture local references so the closure is null-safe even if
	// `closeRabbitMQ` clears the module-level vars during shutdown.
	const channel = _channel;
	channel.consume(config.rabbitmq.queue, async (msg) => {
		if (!msg) return;
		try {
			const raw = JSON.parse(msg.content.toString()) as unknown;
			const parseResult = ProjectCreatedPayloadSchema.safeParse(raw);
			if (!parseResult.success) {
				console.error(
					"[RabbitMQ] Malformed ProjectCreatedEvent — nacking without requeue:",
					parseResult.error.issues,
				);
				try {
					channel.nack(msg, false, false);
				} catch {
					/* best-effort */
				}
				return;
			}
			const payload: ProjectCreatedPayload = parseResult.data;
			await initializeProjectInNeo4j(payload);
			await initializeManuscriptInMongo(payload.projectId, payload.title);
			channel.ack(msg);
			console.log(
				`[RabbitMQ] Project ${payload.projectId} initialized in Neo4j and MongoDB`,
			);
		} catch (error) {
			console.error("[RabbitMQ] Error processing ProjectCreatedEvent:", error);
			try {
				channel.nack(msg, false, false);
			} catch (nackErr) {
				console.error("[RabbitMQ] Failed to nack message:", nackErr);
			}
		}
	});

	_conn.on("error", (err) => {
		console.error("[RabbitMQ] Connection error:", err.message);
	});
};

/**
 * Closes the RabbitMQ channel and connection gracefully.
 * Called during application shutdown from {@link bootstrap}.
 */
export const closeRabbitMQ = async (): Promise<void> => {
	try {
		if (_channel) {
			await _channel.close();
			_channel = null;
		}
		if (_conn) {
			await _conn.close();
			_conn = null;
		}
		console.log("[RabbitMQ] Connection closed");
	} catch (err) {
		console.error("[RabbitMQ] Error during cleanup:", err);
	}
};
