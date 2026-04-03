import type { Channel, ChannelModel } from "amqplib";
import amqplib from "amqplib";
import { getNeo4jDriver } from "@/db/neo4j";
import { ManuscriptModel } from "@/models/Manuscript.model";
import { config } from "@/config/env";

/** Shape of the payload published by server-core when a project is created. */
interface ProjectCreatedPayload {
  projectId: string;
  ownerId: string;
  title: string;
  createdAt: string;
}

/** RabbitMQ routing key this consumer subscribes to. */
const ROUTING_KEY = "project.created";

/**
 * Creates a `:Project` node in Neo4j for the given payload using MERGE,
 * so duplicate events are idempotent.
 */
const initializeProjectInNeo4j = async (
  payload: ProjectCreatedPayload,
): Promise<void> => {
  const driver = getNeo4jDriver();
  const session = driver.session();
  try {
    await session.run(
      `MERGE (p:Project { projectId: $projectId })
       ON CREATE SET p.title = $title, p.ownerId = $ownerId, p.createdAt = $createdAt`,
      {
        projectId: payload.projectId,
        title: payload.title,
        ownerId: payload.ownerId,
        createdAt: payload.createdAt,
      },
    );
  } finally {
    await session.close();
  }
};

/**
 * Creates an empty {@link Manuscript} document in MongoDB for the project if
 * one does not already exist (idempotent).
 */
const initializeManuscriptInMongo = async (
  projectId: string,
): Promise<void> => {
  const exists = await ManuscriptModel.exists({ projectId });
  if (!exists) {
    await ManuscriptModel.create({ projectId, chapters: [] });
  }
};

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

  let conn: ChannelModel | null = null;

  for (let attempt = 1; attempt <= MAX_RETRIES; attempt++) {
    try {
      conn = await amqplib.connect(config.rabbitmq.url);
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

  if (!conn) return;

  const channel: Channel = await conn.createChannel();

  await channel.assertExchange(config.rabbitmq.exchange, "topic", {
    durable: true,
  });
  await channel.assertQueue(config.rabbitmq.queue, { durable: true });
  await channel.bindQueue(
    config.rabbitmq.queue,
    config.rabbitmq.exchange,
    ROUTING_KEY,
  );
  channel.prefetch(1);

  console.log(
    `RabbitMQ consumer active: ${config.rabbitmq.queue} (${ROUTING_KEY})`,
  );

  channel.consume(config.rabbitmq.queue, async (msg) => {
    if (!msg) return;
    try {
      const payload: ProjectCreatedPayload = JSON.parse(msg.content.toString());
      await initializeProjectInNeo4j(payload);
      await initializeManuscriptInMongo(payload.projectId);
      channel.ack(msg);
      console.log(
        `Project ${payload.projectId} initialized in Neo4j and MongoDB`,
      );
    } catch (error) {
      console.error("Error processing ProjectCreatedEvent:", error);
      channel.nack(msg, false, false);
    }
  });

  conn.on("error", (err) => {
    console.error("RabbitMQ connection error:", err.message);
  });
};
