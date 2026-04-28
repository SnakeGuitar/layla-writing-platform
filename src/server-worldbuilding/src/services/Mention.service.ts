import type { IMention } from "@/interfaces/manuscript/IManuscript";
import type { IWikiEntryNoDescription } from "@/interfaces/wiki/IWikiEntry";
import { container } from "./container";

/** Hard limits to mitigate ReDoS and runaway regex evaluation. */
const MAX_RTF_LENGTH = 5_000_000;
const MAX_PLAIN_TEXT_LENGTH = 1_000_000;
const MAX_ENTITY_NAME_LENGTH = 200;

/** Escapes special regex characters in a string. */
const escapeRegex = (str: string): string =>
  str.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");

/**
 * Strips RTF control words and formatting, returning only the visible text.
 * Used for entity name matching against chapter content.
 *
 * Handles, in order:
 *   1. Binary blobs:        `\binN ...N bytes`     → removed
 *   2. Ignored destinations: `{\* ... }`           → removed (best-effort)
 *   3. Hex escapes:          `\'XX`                → byte → char (latin-1)
 *   4. Unicode escapes:      `\uNNNN?`             → real character
 *   5. Control words:        `\word` / `\word123`  → space
 *   6. Stray braces and backslashes                → removed
 */
export const stripRtf = (rtf: string): string => {
  if (!rtf) return "";
  // Cap input size to avoid pathological regex evaluation on huge documents.
  let s = rtf.length > MAX_RTF_LENGTH ? rtf.slice(0, MAX_RTF_LENGTH) : rtf;
  if (!s.startsWith("{\\rtf")) return s.replace(/\s+/g, " ").trim();

  // 1. Strip binary data sections.
  s = s.replace(/\\bin\d+\s?[\s\S]*?(?=\\|\}|$)/g, " ");

  // 2. Drop ignored destinations like {\*\fonttbl ...} (best-effort, 1 level).
  s = s.replace(/\{\\\*[^{}]*\}/g, " ");

  // 3. Hex escapes \'e9 → byte 0xE9 → char.
  s = s.replace(/\\'([0-9a-fA-F]{2})/g, (_m, hex: string) =>
    String.fromCharCode(parseInt(hex, 16)),
  );

  // 4. Unicode escapes \u233? or \u-23? → real char.
  s = s.replace(/\\u(-?\d+)\??/g, (_m, code: string) => {
    const n = parseInt(code, 10);
    return String.fromCharCode(n < 0 ? n + 65536 : n);
  });

  // 5. Control words: \word, \word42, \word42 (with optional trailing space).
  s = s.replace(/\\[a-zA-Z]+-?\d*\s?/g, " ");

  // 6. Stray braces and remaining backslashes.
  s = s.replace(/[{}]/g, "").replace(/\\/g, "");

  return s.replace(/\s+/g, " ").trim();
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
  const text =
    plainText.length > MAX_PLAIN_TEXT_LENGTH
      ? plainText.slice(0, MAX_PLAIN_TEXT_LENGTH)
      : plainText;

  // Skip entries with empty or pathologically long names that could trigger
  // expensive regex compilation/matching.
  const safeEntries = entries.filter(
    (e) => e.name && e.name.length > 0 && e.name.length <= MAX_ENTITY_NAME_LENGTH,
  );

  const patterns = safeEntries.map((entry) => ({
    entry,
    regex: new RegExp(`\\b${escapeRegex(entry.name)}\\b`, "i"),
  }));

  for (const { entry, regex } of patterns) {
    if (found.has(entry.entityId)) continue;

    if (regex.test(text)) {
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
 *
 * Race-condition mitigation: between extracting mentions and syncing them,
 * a wiki entry may be deleted. We re-fetch entries immediately before sync
 * and discard any mention whose entity no longer exists, narrowing the
 * window where MongoDB and Neo4j can diverge.
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
  const entries: IWikiEntryNoDescription[] | null =
    await repo.wikiRepo.listEntries(data.projectId);
  if (!entries || entries.length === 0) return [];

  const plainText: string = stripRtf(data.content);
  const candidateMentions: IMention[] = extractMentions(plainText, entries);
  if (candidateMentions.length === 0) return [];

  // Re-validate against the freshest snapshot to drop entries deleted
  // between extraction and sync.
  const freshEntries = await repo.wikiRepo.listEntries(data.projectId);
  const freshIds = new Set((freshEntries ?? []).map((e) => e.entityId));
  const mentions = candidateMentions.filter((m) => freshIds.has(m.entityId));

  if (mentions.length === 0) return [];

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

