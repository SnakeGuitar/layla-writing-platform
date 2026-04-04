import type { IMention } from "@/interfaces/manuscript/IManuscript";
import type { IWikiEntryNoDescription } from "@/interfaces/wiki/IWikiEntry";
import { container } from "./container";

/** Escapes special regex characters in a string. */
const escapeRegex = (str: string): string =>
  str.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");

/**
 * Strips RTF control words and formatting, returning only the visible text.
 * Used for entity name matching against chapter content.
 */
export const stripRtf = (rtf: string): string => {
  if (!rtf) return "";

  const source: string = rtf.startsWith("{\\rtf")
    ? rtf
        .replace(/\\[a-z]+[-]?\d*\s?/gi, " ")
        .replace(/[{}]/g, "")
        .replace(/\\\*/g, "")
        .replace(/\\/g, "")
    : rtf;

  return source.replace(/\s+/g, " ").trim();
};

/**
 * Scans plain text for known wiki entity names (case-insensitive, whole-word).
 * Returns a deduplicated array of mentions found.
 *
 * Pre-compiles all regex patterns before scanning to avoid O(n) compilations.
 */
export const extractMentions = (
  plainText: string,
  entries: IWikiEntryNoDescription[],
): IMention[] => {
  const found = new Map<string, IMention>();

  const patterns = entries.map((entry) => ({
    entry,
    regex: new RegExp(`\\b${escapeRegex(entry.name)}\\b`, "i"),
  }));

  for (const { entry, regex } of patterns) {
    if (found.has(entry.entityId)) continue;

    if (regex.test(plainText)) {
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
export const syncChapterMentions = async (
  data: {
    projectId: string;
    manuscriptId: string;
    manuscriptTitle: string;
    chapterId: string;
    chapterTitle: string;
    content: string;
  },
  repo = container,
): Promise<IMention[]> => {
  const entries: IWikiEntryNoDescription[] = await repo.wikiRepo.listEntries(
    data.projectId,
  );
  const plainText: string = stripRtf(data.content);
  const mentions: IMention[] = extractMentions(plainText, entries);

  try {
    await repo.graphRepo.syncAppearances({
      projectId: data.projectId,
      manuscriptId: data.manuscriptId,
      manuscriptTitle: data.manuscriptTitle,
      chapterId: data.chapterId,
      chapterTitle: data.chapterTitle,
      entityIds: mentions.map((m) => m.entityId),
    });
  } catch (err) {
    throw new Error(
      `[Mention.service] Failed to sync APPEARS_IN edges for chapter ${data.chapterId}`,
      { cause: err },
    );
  }

  return mentions;
};

