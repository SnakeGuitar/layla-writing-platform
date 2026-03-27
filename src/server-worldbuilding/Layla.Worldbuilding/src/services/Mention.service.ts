import type { IMention } from "@/interfaces/manuscript/IManuscript";
import type { IWikiEntry } from "@/interfaces/wiki/IWikiEntry";
import { MongooseWikiEntryRepository } from "@/repositories/MongooseWikiEntryRepository";
import { Neo4jGraphRepository } from "@/repositories/Neo4jGraphRepository";

const wikiRepo = new MongooseWikiEntryRepository();
const graphRepo = new Neo4jGraphRepository();

/**
 * Strips RTF control words and formatting, returning only the visible text.
 * Used for entity name matching against chapter content.
 */
export const stripRtf = (rtf: string): string => {
  if (!rtf || !rtf.startsWith("{\\rtf")) return rtf ?? "";

  const text: string = rtf
    .replace(/\\[a-z]+[-]?\d*\s?/gi, " ")
    .replace(/[{}]/g, "")
    .replace(/\\\*/g, "")
    .replace(/\\/g, "")
    .replace(/\s+/g, " ")
    .trim();

  return text;
};

/**
 * Scans plain text for known wiki entity names (case-insensitive, whole-word).
 * Returns a deduplicated array of mentions found.
 */
export const extractMentions = (
  plainText: string,
  entries: IWikiEntry[],
): IMention[] => {
  const found = new Map<string, IMention>();
  const lowerText = plainText.toLowerCase();

  for (const entry of entries) {
    if (found.has(entry.entityId)) continue;

    const pattern = new RegExp(`\\b${escapeRegex(entry.name)}\\b`, "i");

    if (pattern.test(lowerText)) {
      found.set(entry.entityId, {
        entityId: entry.entityId,
        name: entry.name,
        entityType: entry.entityType,
      });
    }
  }

  return Array.from(found.values());
};

/**
 * Full pipeline: extract mentions from chapter content, persist them
 * to the chapter document, and sync APPEARS_IN edges to Neo4j.
 */
export const syncChapterMentions = async (data: {
  projectId: string;
  manuscriptId: string;
  manuscriptTitle: string;
  chapterId: string;
  chapterTitle: string;
  content: string;
}): Promise<IMention[]> => {
  const entries = await wikiRepo.listEntries(data.projectId);
  const plainText = stripRtf(data.content);
  const mentions = extractMentions(plainText, entries);

  await graphRepo.clearChapterAppearances({
    projectId: data.projectId,
    chapterId: data.chapterId,
  });

  for (const mention of mentions) {
    try {
      await graphRepo.mergeAppearance({
        projectId: data.projectId,
        entityId: mention.entityId,
        manuscriptId: data.manuscriptId,
        manuscriptTitle: data.manuscriptTitle,
        chapterId: data.chapterId,
        chapterTitle: data.chapterTitle,
      });
    } catch (err) {
      console.warn(
        `[Mention.service] Failed to sync APPEARS_IN for entity ${mention.entityId} in chapter ${data.chapterId}`,
        err,
      );
    }
  }

  return mentions;
};

/**
 * Returns all chapters where a given entity appears, queried from Neo4j.
 */
export const getEntityAppearances = async (
  projectId: string,
  entityId: string,
) => {
  return graphRepo.getEntityAppearances({ projectId, entityId });
};

/** Escapes special regex characters in a string. */
const escapeRegex = (str: string): string =>
  str.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
