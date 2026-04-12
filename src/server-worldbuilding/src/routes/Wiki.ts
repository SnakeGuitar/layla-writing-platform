import { Router } from "express";
import { MiddlewareAuthenticate } from "@/middlewares/Auth";
import { requireProjectAccess } from "@/middlewares/ProjectGuard";
import { asyncHandler } from "@/utils/asyncHandler";
import * as WikiController from "@/controllers/Wiki.controller";

/** Routes for wiki entry management, scoped to a project. */
const router: ReturnType<typeof Router> = Router();
router.use(MiddlewareAuthenticate);
router.use(asyncHandler(requireProjectAccess()));

router.get("/:projectId/entries", asyncHandler(WikiController.listEntries));

router.get(
	"/:projectId/entries/:entityId",
	asyncHandler(WikiController.getEntry),
);

router.post("/:projectId/entries", asyncHandler(WikiController.createEntry));

router.put(
	"/:projectId/entries/:entityId",
	asyncHandler(WikiController.updateEntry),
);

router.delete(
	"/:projectId/entries/:entityId",
	asyncHandler(WikiController.deleteEntry),
);

router.get(
	"/:projectId/entries/:entityId/appearances",
	asyncHandler(WikiController.getEntityAppearances),
);

export default router;
