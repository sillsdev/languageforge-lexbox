<script lang="ts">
  import {Label} from '$lib/components/ui/label';
  import type {IUserComment} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IUserComment';
  import {t} from 'svelte-i18n-lingui';
  import CommentInput from './CommentInput.svelte';
  import CommentThread from './CommentThread.svelte';
  import type {ThreadView} from './types';

  let {
    canComment,
    loading,
    saving,
    hasThreads,
    threadViews,
    newThreadText = $bindable(''),
    editingCommentId,
    currentUserId,
    onStartThread,
    onReply,
    onStartEdit,
    onCancelEdit,
    onSaveEdit,
  }: {
    canComment: boolean;
    loading: boolean;
    saving: boolean;
    hasThreads: boolean;
    threadViews: ThreadView[];
    newThreadText?: string;
    editingCommentId?: string;
    currentUserId?: string;
    onStartThread: () => void;
    onReply: (threadView: ThreadView, text: string) => void | Promise<void>;
    onStartEdit: (comment: IUserComment) => void;
    onCancelEdit: (commentId: string) => void;
    onSaveEdit: (commentId: string, text: string) => void;
  } = $props();
</script>

<div class="flex min-h-0 flex-1 flex-col overflow-hidden">
  <div class="min-h-0 flex-1 space-y-4 overflow-y-auto px-4 pb-4">
    <form onsubmit={(e) => { e.preventDefault(); onStartThread(); }} class="space-y-2">
      <Label for="new-comment-thread">{$t`Start a new thread`}</Label>
      {#if canComment}
        <CommentInput
          id="new-comment-thread"
          bind:value={newThreadText}
          placeholder={$t`Write the first comment...`}
          rows={2}
          disabled={loading || saving}
          loading={saving}
          buttonVariant="default"
          submitDisabled={!newThreadText.trim() || loading}
        />
      {:else}
        <p class="rounded-md border border-dashed p-3 text-sm text-muted-foreground">
          {$t`Sign in to add comments.`}
        </p>
      {/if}
    </form>

    <div class="space-y-3">
      {#if loading}
        <p class="text-sm text-muted-foreground">{$t`Loading comments...`}</p>
      {:else if !hasThreads}
        <p class="rounded-md border border-dashed p-4 text-sm text-muted-foreground">
          {$t`No comment threads yet.`}
        </p>
      {:else}
        {#each threadViews as threadView (threadView.thread.id)}
          <CommentThread
            {threadView}
            {canComment}
            {saving}
            {currentUserId}
            {editingCommentId}
            onReply={(text) => onReply(threadView, text)}
            {onStartEdit}
            {onCancelEdit}
            {onSaveEdit}
          />
        {/each}
      {/if}
    </div>
  </div>
</div>
