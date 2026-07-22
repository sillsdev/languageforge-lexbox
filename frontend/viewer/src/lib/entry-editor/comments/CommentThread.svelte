<script lang="ts">
  import type {IUserComment} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IUserComment';
  import {ThreadStatus} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ThreadStatus';
  import {t} from 'svelte-i18n-lingui';
  import CommentInput from './CommentInput.svelte';
  import CommentItem from './CommentItem.svelte';
  import type {ThreadView} from './types';

  let {
    threadView,
    canComment,
    saving,
    currentUserId,
    editingCommentId,
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
    onReply: (text: string) => void | Promise<void>;
    onStartEdit: (comment: IUserComment) => void;
    onCancelEdit: (commentId: string) => void;
    onSaveEdit: (commentId: string, text: string) => void;
  } = $props();

  let replyText = $state('');

  async function submitReply(): Promise<void> {
    const text = replyText.trim();
    if (!text) return;
    await onReply(text);
    replyText = '';
  }
</script>

<section class="space-y-3 rounded-md border p-3">
  <div class="flex items-center justify-between gap-2">
    <div class="min-w-0">
      <h3 class="text-sm font-semibold">{$t`Thread`}</h3>
      <span class="text-xs text-muted-foreground">
        {threadView.thread.authorName || $t`(unknown)`}
      </span>
    </div>
    <span class="shrink-0 text-xs text-muted-foreground">
      {threadView.thread.status === ThreadStatus.Closed ? $t`Closed` : $t`Open`}
    </span>
  </div>

  <div class="space-y-2">
    {#each threadView.comments as comment (comment.id)}
      <CommentItem
        {comment}
        canEdit={Boolean(currentUserId && comment.authorId === currentUserId)}
        {saving}
        editing={editingCommentId === comment.id}
        onStartEdit={() => onStartEdit(comment)}
        onCancelEdit={() => onCancelEdit(comment.id)}
        onSaveEdit={(text) => onSaveEdit(comment.id, text)}
      />
    {/each}
  </div>

  <div class="space-y-2">
    {#if canComment}
      <form onsubmit={(e) => { e.preventDefault(); void submitReply(); }} class="space-y-2">
        <CommentInput
          id={`reply-${threadView.thread.id}`}
          bind:value={replyText}
          placeholder={$t`Write a reply...`}
          loading={saving}
          disabled={saving}
        />
      </form>
    {/if}
  </div>
</section>
