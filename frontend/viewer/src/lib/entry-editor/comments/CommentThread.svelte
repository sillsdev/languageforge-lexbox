<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import type {IUserComment} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IUserComment';
  import {ThreadStatus} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ThreadStatus';
  import {cn} from '$lib/utils';
  import {t} from 'svelte-i18n-lingui';
  import CommentAuthorAvatar from './CommentAuthorAvatar.svelte';
  import CommentItem from './CommentItem.svelte';
  import CommentReplyInput from './CommentReplyInput.svelte';
  import type {ThreadView} from './types';

  let {
    threadView,
    canComment,
    saving,
    currentUserId,
    editingCommentId,
    expanded = false,
    onToggle,
    onResolve,
    onReply,
    onStartEdit,
    onCancelEdit,
    onSaveEdit,
  }: {
    threadView: ThreadView;
    canComment: boolean;
    saving: boolean;
    currentUserId?: string;
    editingCommentId?: string;
    expanded?: boolean;
    onToggle: () => void;
    onResolve: () => void;
    onReply: (text: string) => void | Promise<void>;
    onStartEdit: (comment: IUserComment) => void;
    onCancelEdit: (commentId: string) => void;
    onSaveEdit: (commentId: string, text: string) => void;
  } = $props();

  const resolved = $derived(threadView.thread.status === ThreadStatus.Closed);
  const firstComment = $derived(threadView.comments[0]);
  const replyCount = $derived(Math.max(0, threadView.comments.length - 1));
</script>

<section
  class={cn(
    'shrink-0 overflow-hidden rounded-lg border border-border',
    resolved ? 'opacity-65' : 'border-l-[3px] border-l-primary bg-card',
  )}
>
  <button
    type="button"
    class="flex w-full cursor-pointer items-center gap-2 px-3.5 py-2.5 text-start"
    onclick={onToggle}
  >
    {#if firstComment}
      <CommentAuthorAvatar
        authorName={firstComment.authorName}
        authorId={firstComment.authorId}
        size="sm"
      />
    {/if}
    <div class="min-w-0 flex-1">
      {#if !expanded && firstComment}
        <p class="m-0 truncate text-xs text-muted-foreground">
          {firstComment.text}
        </p>
      {/if}
    </div>
    {#if !expanded && replyCount > 0}
      <span class="shrink-0 text-[11px] text-muted-foreground">
        {replyCount === 1 ? $t`1 reply` : $t`${replyCount} replies`}
      </span>
    {/if}
    <Icon
      icon="i-mdi-chevron-down"
      class={cn('size-4 shrink-0 text-muted-foreground transition-transform', expanded && 'rotate-180')}
    />
    <Button
      variant="outline"
      size="icon-xs"
      class={cn(
        'size-6 shrink-0',
        resolved ? 'text-muted-foreground' : 'border-primary text-primary hover:bg-primary/10 hover:text-primary',
      )}
      aria-label={resolved ? $t`Reopen` : $t`Resolve`}
      icon={resolved ? 'i-mdi-restore' : 'i-mdi-check-bold'}
      iconProps={{class: 'size-3.5'}}
      onclick={(e) => {
        e.stopPropagation();
        onResolve();
      }}
      disabled={saving}
    />
  </button>

  {#if expanded}
    <div class="border-t border-border px-3.5 pt-2.5 pb-3">
      <div class="flex flex-col">
        {#each threadView.comments as comment, index (comment.id)}
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

      {#if canComment && !resolved}
        <div class="mt-2.5 border-t border-border pt-2.5">
          <CommentReplyInput {saving} onSubmit={onReply} />
        </div>
      {/if}
    </div>
  {/if}
</section>
