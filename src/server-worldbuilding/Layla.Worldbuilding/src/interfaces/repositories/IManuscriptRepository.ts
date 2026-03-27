import type { IManuscript, IChapter } from "../manuscript/IManuscript";

/** Data-access contract for manuscript and chapter persistence. */
export interface IManuscriptRepository {
  /**
   * Returns all manuscripts that belong to `projectId`, sorted by `order` ascending.
   * Returns an empty array when the project has no manuscripts.
   */
  getManuscriptsByProject(projectId: string): Promise<IManuscript[]>;

  /**
   * Returns a single manuscript identified by the compound key (`projectId`, `manuscriptId`),
   * or `null` when not found.
   */
  getManuscript(
    projectId: string,
    manuscriptId: string,
  ): Promise<IManuscript | null>;

  /**
   * Persists a new manuscript document and returns it.
   * The caller is responsible for supplying a pre-generated `manuscriptId`.
   */
  createManuscript(data: Partial<IManuscript>): Promise<IManuscript>;

  /**
   * Applies a partial update to the manuscript identified by the compound key and
   * returns the updated document, or `null` if not found.
   */
  updateManuscript(
    projectId: string,
    manuscriptId: string,
    data: Partial<IManuscript>,
  ): Promise<IManuscript | null>;

  /**
   * Deletes the manuscript and all its embedded chapters.
   * Returns `true` if a document was deleted, `false` if not found.
   */
  deleteManuscript(projectId: string, manuscriptId: string): Promise<boolean>;

  /**
   * Finds a single chapter by `chapterId` within the specified manuscript,
   * or `null` if either the manuscript or the chapter is not found.
   */
  getChapter(
    projectId: string,
    manuscriptId: string,
    chapterId: string,
  ): Promise<IChapter | null>;

  /**
   * Appends a chapter to the manuscript's `chapters` array and returns the
   * newly inserted chapter, or `null` if the manuscript does not exist.
   * The caller is responsible for supplying a pre-generated `chapterId`.
   */
  createChapter(
    projectId: string,
    manuscriptId: string,
    chapter: Partial<IChapter>,
  ): Promise<IChapter | null>;

  /**
   * Merges `data` into the chapter identified by `chapterId` and persists the
   * parent manuscript document. Returns the updated manuscript, or `null` if
   * either the manuscript or the chapter is not found.
   */
  updateChapter(
    projectId: string,
    manuscriptId: string,
    chapterId: string,
    data: IChapter,
  ): Promise<IManuscript | null>;

  /**
   * Removes the chapter from the manuscript's `chapters` array using `$pull`.
   * Returns `true` if the chapter was found and removed, `false` otherwise.
   */
  deleteChapter(
    projectId: string,
    manuscriptId: string,
    chapterId: string,
  ): Promise<boolean>;
}
