import type { Request, Response } from "express";
import * as ManuscriptService from "@/services/Manuscript.service";
import {
  CreateManuscriptSchema,
  UpdateManuscriptSchema,
  CreateChapterSchema,
  UpdateChapterSchema,
  validate,
} from "@/validation";

/**
 * GET /api/manuscripts/:projectId
 *
 * Returns all manuscripts for the project as index objects
 * (chapter metadata without content), ordered by `order` ascending.
 */
export const getManuscriptsByProject = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const manuscripts = await ManuscriptService.getManuscriptsByProject(
    req.params["projectId"] as string,
  );
  res.json(manuscripts);
};

/**
 * GET /api/manuscripts/:projectId/:manuscriptId
 *
 * Returns a single manuscript with its chapter index (no content).
 * Responds with **404** when the manuscript does not exist.
 */
export const getManuscript = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const manuscript = await ManuscriptService.getManuscript(
    req.params["projectId"] as string,
    req.params["manuscriptId"] as string,
  );
  if (!manuscript) {
    res.status(404).json({ error: "Manuscript not found" });
    return;
  }
  res.json(manuscript);
};

/**
 * POST /api/manuscripts/:projectId
 *
 * Creates a new manuscript in the project.
 */
export const createManuscript = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const parsed = validate(CreateManuscriptSchema, req.body);
  if (!parsed.success) {
    res.status(400).json({ error: parsed.error });
    return;
  }

  const manuscript = await ManuscriptService.createManuscript(
    req.params["projectId"] as string,
    parsed.data,
  );
  res.status(201).json(manuscript);
};

/**
 * PUT /api/manuscripts/:projectId/:manuscriptId
 *
 * Updates a manuscript's `title` and/or `order`.
 * Responds with **404** when the manuscript does not exist.
 */
export const updateManuscript = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const parsed = validate(UpdateManuscriptSchema, req.body);
  if (!parsed.success) {
    res.status(400).json({ error: parsed.error });
    return;
  }

  const result = await ManuscriptService.updateManuscriptMeta(
    req.params["projectId"] as string,
    req.params["manuscriptId"] as string,
    parsed.data,
  );
  if (!result) {
    res.status(404).json({ error: "Manuscript not found" });
    return;
  }
  res.json(result);
};

/**
 * DELETE /api/manuscripts/:projectId/:manuscriptId
 *
 * Permanently deletes the manuscript and all its chapters.
 * Returns **204 No Content** on success, **404** when not found.
 */
export const deleteManuscript = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const deleted = await ManuscriptService.deleteManuscript(
    req.params["projectId"] as string,
    req.params["manuscriptId"] as string,
  );
  if (!deleted) {
    res.status(404).json({ error: "Manuscript not found" });
    return;
  }
  res.status(204).send();
};

/**
 * GET /api/manuscripts/:projectId/:manuscriptId/chapters/:chapterId
 *
 * Returns the full content of a single chapter.
 */
export const getChapter = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const chapter = await ManuscriptService.getChapter(
    req.params["projectId"] as string,
    req.params["manuscriptId"] as string,
    req.params["chapterId"] as string,
  );
  if (!chapter) {
    res.status(404).json({ error: "Chapter not found" });
    return;
  }
  res.json(chapter);
};

/**
 * POST /api/manuscripts/:projectId/:manuscriptId/chapters
 *
 * Creates a new chapter in the specified manuscript.
 */
export const createChapter = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const parsed = validate(CreateChapterSchema, req.body);
  if (!parsed.success) {
    res.status(400).json({ error: parsed.error });
    return;
  }

  const chapter = await ManuscriptService.createChapter(
    req.params["projectId"] as string,
    req.params["manuscriptId"] as string,
    parsed.data,
  );
  if (!chapter) {
    res.status(404).json({ error: "Manuscript not found" });
    return;
  }
  res.status(201).json(chapter);
};

/**
 * PUT /api/manuscripts/:projectId/:manuscriptId/chapters/:chapterId
 *
 * Updates a chapter's `title`, `content`, and/or `order`.
 * Responds with **409 Conflict** when `clientTimestamp` is stale (LWW guard).
 */
export const updateChapter = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const parsed = validate(UpdateChapterSchema, req.body);
  if (!parsed.success) {
    res.status(400).json({ error: parsed.error });
    return;
  }

  const result = await ManuscriptService.updateChapter(
    req.params["projectId"] as string,
    req.params["manuscriptId"] as string,
    req.params["chapterId"] as string,
    parsed.data,
  );

  if (result.conflict) {
    res.status(409).json({
      error: "Version conflict (Last-Write-Wins)",
      currentVersion: result.chapter,
    });
    return;
  }

  if (!result.chapter) {
    res.status(404).json({ error: "Chapter not found" });
    return;
  }

  res.json(result.chapter);
};

/**
 * DELETE /api/manuscripts/:projectId/:manuscriptId/chapters/:chapterId
 *
 * Removes a chapter from the manuscript.
 */
export const deleteChapter = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const deleted = await ManuscriptService.deleteChapter(
    req.params["projectId"] as string,
    req.params["manuscriptId"] as string,
    req.params["chapterId"] as string,
  );
  if (!deleted) {
    res.status(404).json({ error: "Chapter not found" });
    return;
  }
  res.status(204).send();
};

/**
 * GET /api/manuscripts/:projectId/:manuscriptId/chapters/:chapterId/mentions
 *
 * Returns the detected wiki mentions stored inside the chapter.
 */
export const getChapterMentions = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const chapter = await ManuscriptService.getChapter(
    req.params["projectId"] as string,
    req.params["manuscriptId"] as string,
    req.params["chapterId"] as string,
  );
  if (!chapter) {
    res.status(404).json({ error: "Chapter not found" });
    return;
  }
  res.json(chapter.mentions || []);
};

/**
 * PUT /api/manuscripts/:projectId/:manuscriptId/chapters/:chapterId/autosave
 *
 * Handles client debounced autosaves including locally-detected mentions.
 */
export const autosaveChapter = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const { content, mentions, isMilestone } = req.body;
  // User context is populated by auth middleware
  const userId = req.user?.id;

  if (!userId) {
    res.status(401).json({ error: "Unauthorized" });
    return;
  }

  await ManuscriptService.autosaveChapter(
    req.params["projectId"] as string,
    req.params["manuscriptId"] as string,
    req.params["chapterId"] as string,
    content || "",
    mentions || [],
    userId,
    isMilestone === true
  );

  res.status(200).send();
};

/**
 * GET /api/manuscripts/:projectId/:manuscriptId/chapters/:chapterId/versions
 *
 * Returns version history metadata for a chapter.
 */
export const getChapterVersions = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const versions = await ManuscriptService.getChapterVersions(
    req.params["projectId"] as string,
    req.params["chapterId"] as string,
  );
  res.json(versions);
};

/**
 * GET /api/manuscripts/:projectId/:manuscriptId/chapters/:chapterId/versions/:versionId
 *
 * Returns a specific chapter version including content.
 */
export const getChapterVersion = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const version = await ManuscriptService.getChapterVersion(
    req.params["projectId"] as string,
    req.params["chapterId"] as string,
    req.params["versionId"] as string,
  );
  if (!version) {
    res.status(404).json({ error: "Version not found" });
    return;
  }
  res.json(version);
};



/**
 * PUT /api/manuscripts/:projectId/:manuscriptId/chapters/:chapterId/versions/:versionId/restore
 *
 * Restores a chapter to a specific version.
 */
export const restoreVersion = async (
  req: Request,
  res: Response,
): Promise<void> => {
  const userId = req.user?.id;
  if (!userId) {
    res.status(401).json({ error: "Unauthorized" });
    return;
  }

  const restored = await ManuscriptService.restoreVersion(
    req.params["projectId"] as string,
    req.params["manuscriptId"] as string,
    req.params["chapterId"] as string,
    req.params["versionId"] as string,
    userId,
  );

  if (!restored) {
    res.status(404).json({ error: "Version or chapter not found" });
    return;
  }

  res.json(restored);
};
