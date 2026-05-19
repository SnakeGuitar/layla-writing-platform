import { Schema, model } from "mongoose";
import type { IChapterVersion } from "@/interfaces/manuscript/IChapterVersion";

const ChapterVersionSchema = new Schema<IChapterVersion>(
  {
    chapterId: { type: String, required: true, index: true },
    projectId: { type: String, required: true, index: true },
    content: { type: String, default: "" },
    isMilestone: { type: Boolean, default: false },
    createdBy: { type: String, required: true },
  },
  { timestamps: true }
);

ChapterVersionSchema.index({ chapterId: 1, createdAt: -1 });

export const ChapterVersionModel = model<IChapterVersion>(
  "ChapterVersion",
  ChapterVersionSchema
);