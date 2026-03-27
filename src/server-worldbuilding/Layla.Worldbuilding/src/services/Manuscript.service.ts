import { v4 as uuidv4 } from "uuid";
import type {
  IChapter,
  IManuscript,
  IMention,
} from "@/interfaces/manuscript/IManuscript";
import { MongooseManuscriptRepository } from "@/repositories/MongooseManuscriptRepository";
import { syncChapterMentions } from "@/services/Mention.service";

const manuscriptRepo = new MongooseManuscriptRepository();

type ChapterMeta = Pick<
  IChapter,
  "chapterId" | "title" | "order" | "updatedAt" | "createdAt"
>;
const toChapterMeta = ({
  chapterId,
  title,
  order,
  updatedAt,
  createdAt,
}: IChapter): ChapterMeta => ({
  chapterId,
  title,
  order,
  updatedAt,
  createdAt,
});

/**
 * Returns all manuscripts belonging to `projectId` as index objects —
 * chapter metadata is included but `content` fields are omitted.
 */
export const getManuscriptsByProject = async (projectId: string) => {
  const manuscripts = await manuscriptRepo.getManuscriptsByProject(projectId);
  return manuscripts.map((m) => ({
    manuscriptId: m.manuscriptId,
    projectId: m.projectId,
    title: m.title,
    order: m.order,
    chapters: m.chapters.map(toChapterMeta),
    createdAt: m.createdAt,
    updatedAt: m.updatedAt,
  }));
};

/**
 * Returns a single manuscript identified by (`projectId`, `manuscriptId`) as
 * an index object — chapter metadata without `content`.
 * Returns `null` when not found.
 */
export const getManuscript = async (
  projectId: string,
  manuscriptId: string,
) => {
  const manuscript = await manuscriptRepo.getManuscript(
    projectId,
    manuscriptId,
  );
  if (!manuscript) return null;

  return {
    manuscriptId: manuscript.manuscriptId,
    projectId: manuscript.projectId,
    title: manuscript.title,
    order: manuscript.order,
    chapters: manuscript.chapters.map(toChapterMeta),
    createdAt: manuscript.createdAt,
    updatedAt: manuscript.updatedAt,
  };
};

/**
 * Creates a new manuscript for `projectId`.
 * If `order` is not supplied, the manuscript is appended after existing ones.
 */
export const createManuscript = async (
  projectId: string,
  data: { title: string; order?: number },
) => {
  const existing = await manuscriptRepo.getManuscriptsByProject(projectId);
  const order = data.order ?? existing.length;

  const manuscript = await manuscriptRepo.createManuscript({
    manuscriptId: uuidv4(),
    projectId,
    title: data.title,
    order,
    chapters: [],
  });

  return {
    manuscriptId: manuscript.manuscriptId,
    projectId: manuscript.projectId,
    title: manuscript.title,
    order: manuscript.order,
    chapters: [],
    createdAt: manuscript.createdAt,
    updatedAt: manuscript.updatedAt,
  };
};

/** Fields accepted by {@link updateManuscriptMeta}. */
export interface UpdateManuscriptData {
  /** New display title for the manuscript. */
  title?: string;
  /** New zero-based display order within the project. */
  order?: number;
}

/**
 * Updates the metadata (`title` and/or `order`) of the specified manuscript.
 * Returns the updated document, or `null` when not found.
 */
export const updateManuscriptMeta = async (
  projectId: string,
  manuscriptId: string,
  data: UpdateManuscriptData,
) => {
  const updateData = Object.fromEntries(
    Object.entries(data).filter(([, v]) => v !== undefined),
  ) as UpdateManuscriptData;

  return manuscriptRepo.updateManuscript(projectId, manuscriptId, updateData);
};

/**
 * Permanently deletes the manuscript and all of its embedded chapters.
 * Returns `true` on success, `false` if the manuscript was not found.
 */
export const deleteManuscript = async (
  projectId: string,
  manuscriptId: string,
) => {
  return manuscriptRepo.deleteManuscript(projectId, manuscriptId);
};

/**
 * Returns a single chapter (including `content`) identified by
 * (`projectId`, `manuscriptId`, `chapterId`), or `null` if not found.
 */
export const getChapter = async (
  projectId: string,
  manuscriptId: string,
  chapterId: string,
) => {
  return manuscriptRepo.getChapter(projectId, manuscriptId, chapterId);
};

/**
 * Creates a new chapter inside the specified manuscript.
 * If `order` is not supplied, defaults to `0`.
 */
export const createChapter = async (
  projectId: string,
  manuscriptId: string,
  data: { title: string; content?: string; order?: number },
) => {
  const manuscript = await manuscriptRepo.getManuscript(
    projectId,
    manuscriptId,
  );
  const defaultOrder = manuscript?.chapters.length ?? 0;

  const newChapter: Partial<IChapter> = {
    chapterId: uuidv4(),
    title: data.title,
    content: data.content ?? "",
    order: data.order ?? defaultOrder,
  };

  return manuscriptRepo.createChapter(projectId, manuscriptId, newChapter);
};

/** Fields accepted by {@link updateChapter}. */
export interface UpdateChapterData {
  /** New display title. */
  title?: string;
  /** Full RTF content to persist. */
  content?: string;
  /** New zero-based display order within the manuscript. */
  order?: number;
  /**
   * ISO-8601 timestamp of the client's last known server state.
   * When provided, the service rejects the write if it is older than
   * the server's `updatedAt` (Last-Write-Wins guard).
   */
  clientTimestamp?: string;
}

interface ChapterUpdatePayload {
  title?: string;
  content?: string;
  order?: number;
  mentions?: IMention[];
  updatedAt: Date;
}

/** Result shape returned by {@link updateChapter}. */
export interface UpdateChapterResult {
  /**
   * `true` when `clientTimestamp` is older than the server's `updatedAt`,
   * indicating a stale write. The caller should present the `chapter` to
   * the user for conflict resolution.
   */
  conflict: boolean;
  /** The current server state of the chapter — present whether or not there is a conflict. */
  chapter?: IChapter;
}

/**
 * Updates a chapter using a Last-Write-Wins (LWW) strategy.
 *
 * If `clientTimestamp` is supplied and precedes the stored `updatedAt`, the write
 * is rejected and `{ conflict: true, chapter }` is returned so the caller can
 * surface a conflict UI.
 */
export const updateChapter = async (
  projectId: string,
  manuscriptId: string,
  chapterId: string,
  data: UpdateChapterData,
): Promise<UpdateChapterResult> => {
  const chapter = await manuscriptRepo.getChapter(
    projectId,
    manuscriptId,
    chapterId,
  );
  if (!chapter) return { conflict: false };

  if (data.clientTimestamp) {
    const clientDate = new Date(data.clientTimestamp);
    if (clientDate < chapter.updatedAt) {
      return { conflict: true, chapter };
    }
  }

  const updatePayload: ChapterUpdatePayload = { updatedAt: new Date() };
  if (data.title !== undefined) updatePayload.title = data.title;
  if (data.content !== undefined) updatePayload.content = data.content;
  if (data.order !== undefined) updatePayload.order = data.order;

  if (data.content !== undefined) {
    const manuscript: IManuscript | null = await manuscriptRepo.getManuscript(
      projectId,
      manuscriptId,
    );
    const chapterDoc = manuscript?.chapters.find(
      (c: IChapter) => c.chapterId === chapterId,
    );

    if (manuscript && chapterDoc) {
      try {
        updatePayload.mentions = await syncChapterMentions({
          projectId,
          manuscriptId,
          manuscriptTitle: manuscript.title,
          chapterId,
          chapterTitle: chapterDoc.title,
          content: data.content,
        });
      } catch (err) {
        console.warn(
          `[Manuscript.service] Mention sync failed for chapter ${chapterId}`,
          err,
        );
      }
    }
  }

  await manuscriptRepo.updateChapter(
    projectId,
    manuscriptId,
    chapterId,
    updatePayload,
  );

  const updatedChapter = await manuscriptRepo.getChapter(
    projectId,
    manuscriptId,
    chapterId,
  );

  if (!updatedChapter) {
    throw new Error(
      `[Manuscript.service] Chapter ${chapterId} not found after update — possible race condition`,
    );
  }

  return { conflict: false, chapter: updatedChapter };
};

/**
 * Removes a chapter from the specified manuscript.
 * Returns `true` if the chapter existed and was removed, `false` otherwise.
 */
export const deleteChapter = async (
  projectId: string,
  manuscriptId: string,
  chapterId: string,
): Promise<boolean> => {
  return manuscriptRepo.deleteChapter(projectId, manuscriptId, chapterId);
};
