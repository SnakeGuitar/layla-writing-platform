export interface IChapterVersion {
  chapterId: string;
  projectId: string;
  content: string;
  isMilestone: boolean;
  createdBy: string;
  createdAt: Date;
  updatedAt: Date;
}