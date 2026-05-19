import { Router } from "express";
import { MiddlewareAuthenticate } from "@/middlewares/Auth";
import { requireProjectAccess } from "@/middlewares/ProjectGuard";
import { asyncHandler } from "@/utils/asyncHandler";
import * as ManuscriptsController from "@/controllers/Manuscripts.controller";

/**
 * Express router for manuscript and chapter management.
 *
 * All routes are scoped under `/api/manuscripts` and require:
 * - A valid JWT via {@link MiddlewareAuthenticate}.
 * - Membership in the target project via {@link requireProjectAccess}.
 *
 * Manuscript routes: `/:projectId` and `/:projectId/:manuscriptId`
 * Chapter routes:    `/:projectId/:manuscriptId/chapters/:chapterId`
 */
const router: ReturnType<typeof Router> = Router();
router.use(MiddlewareAuthenticate);
router.use(asyncHandler(requireProjectAccess()));

router.get(
  "/:projectId",
  asyncHandler(ManuscriptsController.getManuscriptsByProject),
);

router.post(
  "/:projectId",
  asyncHandler(ManuscriptsController.createManuscript),
);

router.get(
  "/:projectId/:manuscriptId",
  asyncHandler(ManuscriptsController.getManuscript),
);

router.put(
  "/:projectId/:manuscriptId",
  asyncHandler(ManuscriptsController.updateManuscript),
);

router.delete(
  "/:projectId/:manuscriptId",
  asyncHandler(ManuscriptsController.deleteManuscript),
);

router.get(
  "/:projectId/:manuscriptId/chapters/:chapterId",
  asyncHandler(ManuscriptsController.getChapter),
);

router.post(
  "/:projectId/:manuscriptId/chapters",
  asyncHandler(ManuscriptsController.createChapter),
);

router.put(
  "/:projectId/:manuscriptId/chapters/:chapterId",
  asyncHandler(ManuscriptsController.updateChapter),
);

router.delete(
  "/:projectId/:manuscriptId/chapters/:chapterId",
  asyncHandler(ManuscriptsController.deleteChapter),
);

router.get(
  "/:projectId/:manuscriptId/chapters/:chapterId/mentions",
  asyncHandler(ManuscriptsController.getChapterMentions),
);

router.put(
  "/:projectId/:manuscriptId/chapters/:chapterId/autosave",
  asyncHandler(ManuscriptsController.autosaveChapter),
);

router.get(
  "/:projectId/:manuscriptId/chapters/:chapterId/versions",
  asyncHandler(ManuscriptsController.getChapterVersions),
);

router.get(
  "/:projectId/:manuscriptId/chapters/:chapterId/versions/:versionId",
  asyncHandler(ManuscriptsController.getChapterVersion),
);



router.put(
  "/:projectId/:manuscriptId/chapters/:chapterId/versions/:versionId/restore",
  asyncHandler(ManuscriptsController.restoreVersion),
);

export default router;
