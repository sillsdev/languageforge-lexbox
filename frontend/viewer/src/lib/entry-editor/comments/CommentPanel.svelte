<script lang="ts">
  import {Button, XButton} from '$lib/components/ui/button';
  import * as Collapsible from '$lib/components/ui/collapsible';
  import {Icon} from '$lib/components/ui/icon';
  import {Textarea} from '$lib/components/ui/textarea';
  import type {IUserComment} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IUserComment';
  import {ThreadStatus} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ThreadStatus';
  import {IsExtraLarge} from '$lib/hooks/is-extra-large.svelte';
  import {cn} from '$lib/utils';
  import {useProjectContext} from '$project/project-context.svelte';
  import {t} from 'svelte-i18n-lingui';
  import CommentAuthorAvatar from './CommentAuthorAvatar.svelte';
  import CommentItem from './CommentItem.svelte';
  import CommentReplyInput from './CommentReplyInput.svelte';
  import CommentThread from './CommentThread.svelte';
  import type {ThreadView} from './types';
  import {slide} from 'svelte/transition';

  let {
    canComment,
    loading,
    saving,
    threadViews,
    newThreadText = $bindable(''),
    editingCommentId,
    currentUserId,
    addingComment = $bindable(false),
    expandedThreadIds = $bindable(new Set<string>()),
    mobileThreadId = $bindable<string | null>(null),
    onClose,
    onStartThread,
    onReply,
    onResolve,
    onStartEdit,
    onCancelEdit,
    onSaveEdit,
    unreadThreadIds = new Set<string>(),
    unreadCount = 0,
    onThreadOpen,
  }: {
    canComment: boolean;
    loading: boolean;
    saving: boolean;
    threadViews: ThreadView[];
    newThreadText?: string;
    editingCommentId?: string;
    currentUserId?: string;
    addingComment?: boolean;
    expandedThreadIds?: Set<string>;
    mobileThreadId?: string | null;
    unreadThreadIds?: Set<string>;
    unreadCount?: number;
    onClose?: () => void;
    onStartThread: () => void;
    onReply: (threadView: ThreadView, text: string) => void | Promise<void>;
    onResolve: (threadView: ThreadView) => void | Promise<void>;
    onStartEdit: (comment: IUserComment) => void;
    onCancelEdit: (commentId: string) => void;
    onSaveEdit: (commentId: string, text: string) => void;
    onThreadOpen?: (threadId: string) => void | Promise<void>;
  } = $props();

  const projectContext = useProjectContext();
  const authorName = $derived(projectContext.projectData?.lastUserName);

  const openThreads = $derived(threadViews.filter((tv) => tv.thread.status === ThreadStatus.Open));
  const resolvedThreads = $derived(threadViews.filter((tv) => tv.thread.status === ThreadStatus.Closed));
  const mobileThreadView = $derived(
    mobileThreadId ? threadViews.find((tv) => tv.thread.id === mobileThreadId) : undefined,
  );
  const mobileResolved = $derived(mobileThreadView?.thread.status === ThreadStatus.Closed);
  /** Full-area thread detail (mobile + tablet bottom drawer), vs in-place expand on desktop sidebar */
  const useThreadDetail = $derived(!IsExtraLarge.value);
  const showThreadDetail = $derived(useThreadDetail && Boolean(mobileThreadView));

  function toggleExpanded(threadId: string): void {
    if (useThreadDetail) {
      mobileThreadId = threadId;
      onThreadOpen?.(threadId);
      return;
    }
    const next = new Set(expandedThreadIds);
    if (next.has(threadId)) next.delete(threadId);
    else {
      next.add(threadId);
      onThreadOpen?.(threadId);
    }
    expandedThreadIds = next;
  }

  function startAdding(): void {
    addingComment = true;
    newThreadText = '';
  }

  function cancelAdding(): void {
    addingComment = false;
    newThreadText = '';
  }

  function submitNewThread(): void {
    if (!newThreadText.trim()) return;
    onStartThread();
  }

  function backFromMobileDetail(): void {
    mobileThreadId = null;
  }
</script>

<div class="flex h-full min-h-0 flex-col overflow-hidden bg-background">
  {#if !showThreadDetail}
    <div class="flex shrink-0 items-center justify-between gap-2 border-b border-border px-4 pt-3.5 pb-2.5">
      <div class="flex items-center gap-2">
        {#if onClose}
          <XButton class="size-6" onclick={onClose} />
        {/if}
        <h2 class="text-sm font-semibold">{$t`Comments`}</h2>
        {#if openThreads.length > 0}
          <span
            class="inline-flex h-[18px] items-center rounded-[10px] bg-primary px-2 text-[11px] font-bold text-primary-foreground"
          >
            {openThreads.length}
          </span>
        {/if}
        {#if unreadCount > 0}
          <span
            class="inline-flex h-[18px] min-w-[18px] items-center justify-center rounded-[10px] bg-destructive px-1.5 text-[11px] font-bold text-destructive-foreground"
            title={$t`Unread comments`}
          >
            {unreadCount}
          </span>
        {/if}
      </div>
      <div class="flex items-center gap-1.5">
        {#if canComment}
          <Button size="xs" icon="i-mdi-plus" class="h-7 px-2.5 text-xs" onclick={startAdding}>
            {$t`Conversation`}
          </Button>
        {/if}
      </div>
    </div>

    {#if addingComment}
      <div class="flex shrink-0 flex-col gap-2 border-b border-border px-3.5 py-3" transition:slide>
        {#if canComment}
          <div class="flex items-start gap-2">
            <CommentAuthorAvatar {authorName} authorId={currentUserId} size="md" class="mt-1 shrink-0" />
            <div class="flex flex-1 flex-col gap-1.5">
              <Textarea
                autofocus
                bind:value={newThreadText}
                placeholder={$t`Start a conversation…`}
                rows={3}
                disabled={loading || saving}
                class="text-sm"
                onkeydown={(e) => {
                  if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
                    e.preventDefault();
                    submitNewThread();
                  }
                }}
              />
              <div class="flex justify-end gap-1.5">
                <Button variant="outline" size="sm" onclick={cancelAdding} disabled={saving}>
                  {$t`Cancel`}
                </Button>
                <Button size="sm" onclick={submitNewThread} disabled={!newThreadText.trim() || loading} loading={saving}>
                  {$t`Comment`}
                </Button>
              </div>
            </div>
          </div>
        {:else}
          <p class="rounded-md border border-dashed p-3 text-sm text-muted-foreground">
            {$t`Sign in to add comments.`}
          </p>
        {/if}
      </div>
    {/if}
  {/if}

  {#if showThreadDetail && mobileThreadView}
    <div class="flex h-full min-h-0 flex-col overflow-hidden">
      <div class="flex shrink-0 items-center gap-2.5 border-b border-border px-3.5 py-2.5">
        <Button
          variant="ghost"
          size="xs"
          class="h-7 px-2 text-[13px]"
          icon="i-mdi-arrow-left"
          onclick={backFromMobileDetail}
        >
          {$t`Back`}
        </Button>
        <Button
          variant="outline"
          size="xs"
          class={cn(
            'ms-auto h-7 shrink-0 px-2.5 text-[11px]',
            !mobileResolved && 'border-primary text-primary hover:bg-primary/10 hover:text-primary',
          )}
          icon={mobileResolved ? 'i-mdi-restore' : 'i-mdi-check-bold'}
          onclick={() => onResolve(mobileThreadView)}
          disabled={saving}
        >
          {mobileResolved ? $t`Reopen` : $t`Resolve`}
        </Button>
      </div>

      <div class="min-h-0 flex-1 overflow-y-auto p-3.5">
        <div class="flex flex-col">
          {#each mobileThreadView.comments as comment, index (comment.id)}
            <CommentItem
              {comment}
              compact={index > 0}
              canEdit={Boolean(currentUserId && comment.authorId === currentUserId)}
              {saving}
              editing={editingCommentId === comment.id}
              onStartEdit={() => onStartEdit(comment)}
              onCancelEdit={() => onCancelEdit(comment.id)}
              onSaveEdit={(text) => onSaveEdit(comment.id, text)}
            />
          {/each}
        </div>
      </div>

      {#if canComment && !mobileResolved}
        <div class="shrink-0 border-t border-border px-3.5 py-2.5">
          <CommentReplyInput
            autofocus
            variant="inline"
            {saving}
            onSubmit={(text) => onReply(mobileThreadView, text)}
          />
        </div>
      {/if}
    </div>
  {:else}
    <div class="min-h-0 flex-1 overflow-y-auto p-3">
      <div class="flex flex-col gap-2.5">
        {#if loading}
          <p class="pt-8 text-center text-[13px] text-muted-foreground">{$t`Loading comments...`}</p>
        {:else}
          {#if openThreads.length === 0}
            {#if canComment}
              <div class="pt-8 text-center text-[13px] leading-relaxed text-muted-foreground">
                <p>{$t`No open conversations`}</p>
                <Button size="xs" icon="i-mdi-plus" class="h-7 px-2.5 text-xs" onclick={startAdding}>
                  {$t`Start a conversation`}
                </Button>
              </div>
            {:else}
              <p class="pt-8 text-center text-[13px] text-muted-foreground">
                {$t`No open conversations`}
              </p>
            {/if}
          {:else}
            {#each openThreads as threadView (threadView.thread.id)}
              <CommentThread
                {threadView}
                {canComment}
                {saving}
                {currentUserId}
                {editingCommentId}
                hasUnread={unreadThreadIds.has(threadView.thread.id)}
                expanded={!useThreadDetail && expandedThreadIds.has(threadView.thread.id)}
                onToggle={() => toggleExpanded(threadView.thread.id)}
                onResolve={() => onResolve(threadView)}
                onReply={(text) => onReply(threadView, text)}
                {onStartEdit}
                {onCancelEdit}
                {onSaveEdit}
              />
            {/each}
          {/if}
          {#if resolvedThreads.length > 0}
            <Collapsible.Root class={cn('group/resolved', openThreads.length > 0 && 'mt-1')}>
              <Collapsible.Trigger class="flex cursor-pointer items-center gap-1 text-[11px] text-muted-foreground">
                <Icon
                  icon="i-mdi-chevron-down"
                  class="size-3.5 shrink-0 transition-transform group-data-[state=open]/resolved:rotate-180"
                />
                {$t`${resolvedThreads.length} resolved`}
              </Collapsible.Trigger>
              <Collapsible.Content class="overflow-hidden">
                <div class="mt-2.5 flex flex-col gap-2.5">
                  {#each resolvedThreads as threadView (threadView.thread.id)}
                    <CommentThread
                      {threadView}
                      {canComment}
                      {saving}
                      {currentUserId}
                      {editingCommentId}
                      hasUnread={unreadThreadIds.has(threadView.thread.id)}
                      expanded={!useThreadDetail && expandedThreadIds.has(threadView.thread.id)}
                      onToggle={() => toggleExpanded(threadView.thread.id)}
                      onResolve={() => onResolve(threadView)}
                      onReply={(text) => onReply(threadView, text)}
                      {onStartEdit}
                      {onCancelEdit}
                      {onSaveEdit}
                    />
                  {/each}
                </div>
              </Collapsible.Content>
            </Collapsible.Root>
          {/if}
        {/if}
      </div>
    </div>
  {/if}
</div>
