import { Schema, model } from "mongoose";
import type { IManuscript } from "@/interfaces/manuscript/IManuscript";

/**
 * Embedded sub-document schema for an individual chapter.
 *
 * Mongoose `timestamps` automatically manages `createdAt` and `updatedAt`,
 * where `updatedAt` is consumed by the Last-Write-Wins conflict detection
 * in {@link Manuscript.service}.
 */
const MentionSchema = new Schema(
  {
    entityId: { type: String, required: true },
    name: { type: String, required: true },
    entityType: { type: String, required: true },
  },
  { _id: false },
);

const ChapterSchema = new Schema(
  {
    chapterId: { type: String, required: true },
    title: { type: String, required: true, maxlength: 200 },
    content: { type: String, default: "" },
    order: { type: Number, required: true },
    mentions: { type: [MentionSchema], default: [] },
  },
  { timestamps: true },
);

/**
 * Top-level schema for a manuscript document.
 *
 * Each project may have multiple `Manuscript` documents. The compound index
 * `(projectId, manuscriptId)` enforces uniqueness and accelerates per-project
 * look-ups. The `order` field controls display order in the editor sidebar.
 */
const ManuscriptSchema = new Schema<IManuscript>(
  {
    manuscriptId: { type: String, required: true, index: true },
    projectId: { type: String, required: true, index: true },
    title: { type: String, required: true, maxlength: 200 },
    order: { type: Number, required: true, default: 0 },
    chapters: [ChapterSchema],
  },
  { timestamps: true },
);

ManuscriptSchema.index({ projectId: 1, manuscriptId: 1 }, { unique: true });

export const ManuscriptModel = model<IManuscript>(
  "Manuscript",
  ManuscriptSchema,
);
