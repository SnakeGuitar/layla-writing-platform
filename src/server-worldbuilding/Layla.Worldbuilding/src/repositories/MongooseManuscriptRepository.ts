import { ManuscriptModel } from "@/models/Manuscript.model";
import type {
  IManuscript,
  IChapter,
  ChapterUpdatePayload,
} from "@/interfaces/manuscript/IManuscript";
import type { IManuscriptRepository } from "@/interfaces/repositories/IManuscriptRepository";

/** Mongoose implementation of {@link IManuscriptRepository}. */
export class MongooseManuscriptRepository implements IManuscriptRepository {
  /** @inheritdoc */
  async getManuscriptsByProject(projectId: string): Promise<IManuscript[]> {
    return ManuscriptModel.find({ projectId })
      .sort({ order: 1 })
      .lean() as unknown as IManuscript[];
  }

  /** @inheritdoc */
  async getManuscript(
    projectId: string,
    manuscriptId: string,
  ): Promise<IManuscript | null> {
    return ManuscriptModel.findOne({
      projectId,
      manuscriptId,
    }).lean() as unknown as IManuscript | null;
  }

  /** @inheritdoc */
  async createManuscript(data: Partial<IManuscript>): Promise<IManuscript> {
    const manuscript = await ManuscriptModel.create(data);
    return manuscript.toObject() as unknown as IManuscript;
  }

  /** @inheritdoc */
  async updateManuscript(
    projectId: string,
    manuscriptId: string,
    data: Partial<IManuscript>,
  ): Promise<IManuscript | null> {
    return ManuscriptModel.findOneAndUpdate(
      { projectId, manuscriptId },
      { $set: data },
      { new: true },
    ).lean() as unknown as IManuscript | null;
  }

  /** @inheritdoc */
  async deleteManuscript(
    projectId: string,
    manuscriptId: string,
  ): Promise<boolean> {
    const result = await ManuscriptModel.deleteOne({ projectId, manuscriptId });
    return result.deletedCount > 0;
  }

  /** @inheritdoc */
  async getChapter(
    projectId: string,
    manuscriptId: string,
    chapterId: string,
  ): Promise<IChapter | null> {
    const manuscript: IManuscript | null = await ManuscriptModel.findOne({
      projectId,
      manuscriptId,
    }).lean();
    if (!manuscript) return null;
    return (
      (manuscript.chapters.find(
        (c: IChapter) => c.chapterId === chapterId,
      ) ?? null) as unknown as IChapter | null
    );
  }

  /** @inheritdoc */
  async createChapter(
    projectId: string,
    manuscriptId: string,
    chapter: Partial<IChapter>,
  ): Promise<IChapter | null> {
    const manuscript: IManuscript | null =
      await ManuscriptModel.findOneAndUpdate(
        { projectId, manuscriptId },
        { $push: { chapters: chapter } },
        { new: true },
      );
    if (!manuscript) return null;
    return (
      (manuscript.chapters.find(
        (c: IChapter) => c.chapterId === chapter.chapterId,
      ) ?? null) as unknown as IChapter | null
    );
  }

  /**
   * @inheritdoc
   *
   * Uses `Object.assign` + `document.save()` instead of a positional `$set`
   * so that Mongoose sub-document hooks and `timestamps` remain active.
   */
  async updateChapter(
    projectId: string,
    manuscriptId: string,
    chapterId: string,
    data: ChapterUpdatePayload,
  ): Promise<IManuscript | null> {
    const manuscript = await ManuscriptModel.findOne({
      projectId,
      manuscriptId,
    });
    if (!manuscript) return null;

    const chapter = manuscript.chapters.find(
      (c: IChapter) => c.chapterId === chapterId,
    );
    if (!chapter) return null;

    Object.assign(chapter, data);
    await manuscript.save();
    return manuscript.toObject() as unknown as IManuscript;
  }

  /** @inheritdoc */
  async deleteChapter(
    projectId: string,
    manuscriptId: string,
    chapterId: string,
  ): Promise<boolean> {
    const result = await ManuscriptModel.updateOne(
      { projectId, manuscriptId },
      { $pull: { chapters: { chapterId } } },
    );
    return result.modifiedCount > 0;
  }
}
