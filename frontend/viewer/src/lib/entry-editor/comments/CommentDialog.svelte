<script lang="ts">
  import {IsExtraLarge} from '$lib/hooks/is-extra-large.svelte';
  import {useMiniLcmApi} from '$lib/services/service-provider';
  import {useProjectContext} from '$project/project-context.svelte';
  import {cn, randomId} from '$lib/utils';
  import type {IUserComment} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IUserComment';
  import type {SubjectType} from '$lib/dotnet-types/generated-types/MiniLcm/Models/SubjectType';
  import {ThreadStatus} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ThreadStatus';
  import type {ClassValue} from 'clsx';
  import {resource, watch} from 'runed';
  import CommentPanel from './CommentPanel.svelte';
  import type {ThreadView} from './types';
  import {SvelteSet} from 'svelte/reactivity';

  let {
    open = $bindable(false),
    subjectType,
    subjectId,
    unreadComments = null,
    class: className,
    onUnreadCommentsChange,
  }: {
    open: boolean;
    subjectType: SubjectType;
    subjectId: string;
    /** When null, unread comments are fetched for the subject. When set, used as-is (no fetch). */
    unreadComments?: IUserComment[] | null;
    class?: ClassValue;
    onUnreadCommentsChange?: (comments: IUserComment[]) => void;
  } = $props();

  const api = useMiniLcmApi();
  const projectContext = useProjectContext();
  const currentUserId = $derived(projectContext.projectData?.lastUserId);
  const canComment = $derived(Boolean(currentUserId) && !!projectContext.features.write);

  let saving = $state(false);
  let newThreadText = $state('');
  let editingCommentId = $state<string>();
  let addingComment = $state(false);
  const expandedThreadIds = new SvelteSet<string>();
  let mobileThreadId = $state<string | null>(null);

  const threadsResource = resource(
    [() => open, () => subjectType, () => subjectId],
    async ([isOpen, targetSubjectType, targetSubjectId]): Promise<ThreadView[]> => {
      if (!isOpen) return [];
      const threads = await api.getCommentThreads(targetSubjectType, targetSubjectId, true);
      return threads.map((thread) => ({thread, comments: thread.comments ?? []}));
    },
    {initialValue: [] satisfies ThreadView[]},
  );
  const threadViews = $derived(threadsResource.current);
  const loading = $derived(threadsResource.loading);

  const unreadResource = resource(
    [() => open, () => subjectType, () => subjectId, () => unreadComments],
    async ([isOpen, targetSubjectType, targetSubjectId, externalUnread]): Promise<IUserComment[]> => {
      if (!isOpen || externalUnread !== null) return [];
      return api.getUnreadCommentsForSubject(targetSubjectType, targetSubjectId);
    },
    {initialValue: [] satisfies IUserComment[]},
  );

  let localUnreadComments = $state<IUserComment[]>([]);

  function syncLocalUnreadFromSource(): void {
    if (!open) {
      localUnreadComments = [];
      return;
    }
    if (unreadComments !== null) {
      localUnreadComments = [...unreadComments];
      return;
    }
    localUnreadComments = [...unreadResource.current];
  }

  watch(
    () => [open, subjectType, subjectId, unreadComments, unreadResource.current] as const,
    () => {
      syncLocalUnreadFromSource();
    },
  );

  const unreadThreadIds = $derived(new Set(localUnreadComments.map((c) => c.commentThreadId)));

  async function refetchUnreadIfNeeded(): Promise<void> {
    if (open && unreadComments === null) {
      await unreadResource.refetch();
      syncLocalUnreadFromSource();
    }
  }

  async function onThreadOpen(threadId: string): Promise<void> {
    if (!unreadThreadIds.has(threadId)) return;
    await api.markCommentThreadRead(threadId);
    localUnreadComments = localUnreadComments.filter((c) => c.commentThreadId !== threadId);
    onUnreadCommentsChange?.(localUnreadComments);
  }

  /** Debug only: puts a thread back in the unread state so unread handling can be re-tested. */
  async function markThreadUnread(threadId: string): Promise<void> {
    await api.markCommentThreadUnread(threadId);
    localUnreadComments = await api.getUnreadCommentsForSubject(subjectType, subjectId);
    onUnreadCommentsChange?.(localUnreadComments);
  }

  // Matches CommentPanel: below xl a thread opens as its own full-panel view.
  const useThreadDetail = $derived(!IsExtraLarge.value);

  function setOpen(value: boolean): void {
    open = value;
  }

  watch(
    () => open,
    (isOpen) => {
      if (isOpen) return;
      addingComment = false;
      newThreadText = '';
      editingCommentId = undefined;
      expandedThreadIds.clear();
      mobileThreadId = null;
    },
  );

  watch(
    () => useThreadDetail,
    (threadDetail) => {
      if (!threadDetail) mobileThreadId = null;
      else expandedThreadIds.clear();
    },
  );

  function onOpenChange(value: boolean): void {
    setOpen(value);
  }

  async function startThread(): Promise<void> {
    const text = newThreadText.trim();
    if (!text) return;

    saving = true;
    try {
      const now = new Date().toISOString();
      const threadId = randomId();
      await api.createCommentThread({
        id: threadId,
        subjectId,
        subjectType,
        status: ThreadStatus.Open,
        createdAt: now,
        updatedAt: now,
      }, {
        id: randomId(),
        commentThreadId: threadId,
        text,
        createdAt: now,
        updatedAt: now,
      });
      newThreadText = '';
      addingComment = false;
      expandedThreadIds.add(threadId);
      await threadsResource.refetch();
      await refetchUnreadIfNeeded();
    } finally {
      saving = false;
    }
  }

  async function replyToThread(threadView: ThreadView, text: string): Promise<void> {
    const threadId = threadView.thread.id;
    const trimmed = text.trim();
    if (!trimmed) return;

    saving = true;
    try {
      const now = new Date().toISOString();
      await api.addUserComment(threadId, {
        id: randomId(),
        commentThreadId: threadId,
        previousCommentId: threadView.comments.at(-1)?.id,
        text: trimmed,
        createdAt: now,
        updatedAt: now,
      });
      await threadsResource.refetch();
      await refetchUnreadIfNeeded();
    } finally {
      saving = false;
    }
  }

  async function toggleResolveThread(threadView: ThreadView): Promise<void> {
    saving = true;
    try {
      const nextStatus =
        threadView.thread.status === ThreadStatus.Closed ? ThreadStatus.Open : ThreadStatus.Closed;
      await api.setCommentThreadStatus(threadView.thread.id, nextStatus);
      await threadsResource.refetch();
      await refetchUnreadIfNeeded();
    } finally {
      saving = false;
    }
  }

  function startEditing(comment: IUserComment): void {
    editingCommentId = comment.id;
  }

  function cancelEditing(_commentId: string): void {
    editingCommentId = undefined;
  }

  async function saveEdit(commentId: string, text: string): Promise<void> {
    const trimmed = text.trim();
    if (!trimmed) return;

    saving = true;
    try {
      await api.editUserComment(commentId, trimmed);
      cancelEditing(commentId);
      await threadsResource.refetch();
      await refetchUnreadIfNeeded();
    } finally {
      saving = false;
    }
  }
</script>
{#if open}
  <aside
    class={cn(
      'flex h-full min-h-0 w-full min-w-0 flex-col overflow-hidden rounded-lg border bg-background shadow-sm',
      className,
    )}
  >
  <CommentPanel
    bind:newThreadText
    bind:addingComment
    {expandedThreadIds}
    bind:mobileThreadId
    {canComment}
    {loading}
    {saving}
    {threadViews}
    {editingCommentId}
    {currentUserId}
    {unreadThreadIds}
    onClose={() => onOpenChange(false)}
    onStartThread={startThread}
    onReply={replyToThread}
    onResolve={toggleResolveThread}
    onStartEdit={startEditing}
    onCancelEdit={cancelEditing}
    onSaveEdit={saveEdit}
    onThreadOpen={onThreadOpen}
    onMarkUnread={markThreadUnread}
  />
  </aside>
{/if}
