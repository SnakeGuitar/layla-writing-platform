import type { Channel, ChannelModel } from "amqplib";
import amqplib from "amqplib";
import { z } from "zod";
import { getNeo4jDriver } from "@/db/neo4j";
import { config } from "@/config/env";

const CollaboratorJoinedPayloadSchema = z.object({
	projectId: z.string().uuid(),
	userId: z.string().uuid(),
	role: z.string().min(1),
});

type CollaboratorJoinedPayload = z.infer<typeof CollaboratorJoinedPayloadSchema>;

const CollaboratorRemovedPayloadSchema = z.object({
	projectId: z.string().uuid(),
	userId: z.string().uuid(),
});

type CollaboratorRemovedPayload = z.infer<typeof CollaboratorRemovedPayloadSchema>;

const JOINED_ROUTING_KEY = "collaborator.joined";
const REMOVED_ROUTING_KEY = "collaborator.removed";

const addCollaboratorInNeo4j = async (
	payload: CollaboratorJoinedPayload,
): Promise<void> => {
	const driver = getNeo4jDriver();
	const session = driver.session();
	try {
		await session.executeWrite(async (tx) => {
			await tx.run(
				`MERGE (p:Project { projectId: $projectId })`,
				{ projectId: payload.projectId }
			);
			await tx.run(
				`MERGE (u:User { id: $userId })
         WITH u
         MATCH (p:Project { projectId: $projectId })
         MERGE (u)-[r:MEMBER_OF]->(p)
         SET r.role = $role`,
				{
					userId: payload.userId,
					projectId: payload.projectId,
					role: payload.role.toUpperCase(),
				},
			);
		});
	} finally {
		await session.close();
	}
};

const removeCollaboratorInNeo4j = async (
	payload: CollaboratorRemovedPayload,
): Promise<void> => {
	const driver = getNeo4jDriver();
	const session = driver.session();
	try {
		await session.executeWrite(async (tx) => {
			await tx.run(
				`MATCH (u:User { id: $userId })-[r:MEMBER_OF]->(p:Project { projectId: $projectId })
         DELETE r`,
				{
					userId: payload.userId,
					projectId: payload.projectId,
				},
			);
		});
	} finally {
		await session.close();
	}
};

let _conn: ChannelModel | null = null;
let _channel: Channel | null = null;

export const startCollaboratorConsumer = async (): Promise<void> => {
	const MAX_RETRIES = 5;
	const RETRY_DELAY_MS = 3000;

	for (let attempt = 1; attempt <= MAX_RETRIES; attempt++) {
		try {
			_conn = await amqplib.connect(config.rabbitmq.url);
			break;
		} catch (err) {
			console.error(
				`RabbitMQ (Collaborator): connection attempt ${attempt}/${MAX_RETRIES} failed`,
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

	// Use a separate queue or bind to the same topic
	const queueName = `${config.rabbitmq.queue}_collaborators`;
	await _channel.assertQueue(queueName, { durable: true });

	await _channel.bindQueue(
		queueName,
		config.rabbitmq.exchange,
		JOINED_ROUTING_KEY,
	);
	await _channel.bindQueue(
		queueName,
		config.rabbitmq.exchange,
		REMOVED_ROUTING_KEY,
	);

	_channel.prefetch(1);

	console.log(
		`RabbitMQ Collaborator consumer active: ${queueName}`,
	);

	const channel = _channel;
	channel.consume(queueName, async (msg) => {
		if (!msg) return;
		try {
			const routingKey = msg.fields.routingKey;
			const raw = JSON.parse(msg.content.toString()) as unknown;

			if (routingKey === JOINED_ROUTING_KEY) {
				const parseResult = CollaboratorJoinedPayloadSchema.safeParse(raw);
				if (!parseResult.success) {
					console.error(
						"[RabbitMQ] Malformed CollaboratorJoinedEvent:",
						parseResult.error.issues,
					);
					channel.nack(msg, false, false);
					return;
				}
				await addCollaboratorInNeo4j(parseResult.data);
				console.log(
					`[RabbitMQ] Collaborator ${parseResult.data.userId} added to project ${parseResult.data.projectId} as ${parseResult.data.role}`,
				);
			} else if (routingKey === REMOVED_ROUTING_KEY) {
				const parseResult = CollaboratorRemovedPayloadSchema.safeParse(raw);
				if (!parseResult.success) {
					console.error(
						"[RabbitMQ] Malformed CollaboratorRemovedEvent:",
						parseResult.error.issues,
					);
					channel.nack(msg, false, false);
					return;
				}
				await removeCollaboratorInNeo4j(parseResult.data);
				console.log(
					`[RabbitMQ] Collaborator ${parseResult.data.userId} removed from project ${parseResult.data.projectId}`,
				);
			} else {
				console.warn(`[RabbitMQ] Unknown routing key: ${routingKey}`);
			}

			channel.ack(msg);
		} catch (error) {
			console.error("[RabbitMQ] Error processing collaborator event:", error);
			try {
				channel.nack(msg, false, false);
			} catch (nackErr) {
				console.error("[RabbitMQ] Failed to nack collaborator message:", nackErr);
			}
		}
	});

	_conn.on("error", (err) => {
		console.error("[RabbitMQ Collaborator] Connection error:", err.message);
	});
};

export const closeCollaboratorRabbitMQ = async (): Promise<void> => {
	try {
		if (_channel) {
			await _channel.close();
			_channel = null;
		}
		if (_conn) {
			await _conn.close();
			_conn = null;
		}
		console.log("[RabbitMQ Collaborator] Connection closed");
	} catch (err) {
		console.error("[RabbitMQ Collaborator] Error during cleanup:", err);
	}
};
