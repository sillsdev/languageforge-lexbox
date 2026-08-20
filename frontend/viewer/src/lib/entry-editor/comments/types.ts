import type {ICommentThread} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ICommentThread';
import type {IUserComment} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IUserComment';

export type ThreadView = {
  thread: ICommentThread;
  comments: IUserComment[];
};
