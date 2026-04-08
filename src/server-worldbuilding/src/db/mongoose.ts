import mongoose from "mongoose";
import { config } from "@/config/env";

const MAX_RETRIES = 5;
const RETRY_DELAY_MS = 3000;

/**
 * Connects to MongoDB using the URI from the application config.
 *
 * Retries up to {@link MAX_RETRIES} times with a {@link RETRY_DELAY_MS} ms
 * backoff between attempts. Throws if all attempts fail.
 */
export const connectMongoDB = async (): Promise<void> => {
	for (let attempt = 1; attempt <= MAX_RETRIES; attempt++) {
		try {
			await mongoose.connect(config.mongo.uri, {
				serverSelectionTimeoutMS: 5000,
			});
			console.log("MongoDB connected.");
			return;
		} catch (error) {
			console.error(
				`MongoDB: attempt ${attempt}/${MAX_RETRIES} failed.\n`,
				error,
			);
			if (attempt === MAX_RETRIES) throw error;
			await new Promise((resolve) => setTimeout(resolve, RETRY_DELAY_MS));
		}
	}
};
