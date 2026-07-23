<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {FormatRelativeDate} from '$lib/components/ui/format';
  import {Textarea} from '$lib/components/ui/textarea';
  import type {IUserComment} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IUserComment';
  import {cn} from '$lib/utils';
  import {t} from 'svelte-i18n-lingui';
  import CommentAuthorAvatar from './CommentAuthorAvatar.svelte';

  let {
    comment,
    canEdit,
    saving,
    editing,
    compact = false,
    onStartEdit,
    onCancelEdit,
    onSaveEdit,
  }: {
    comment: IUserComment;
    canEdit: boolean;
    saving: boolean;
    editing: boolean;
    compact?: boolean;
    onStartEdit: () => void;
    onCancelEdit: () => void;
    onSaveEdit: (text: string) => void;
  } = $props();

  let draftText = $state('');

  $effect(() => {
    if (editing) {
      draftText = comment.text;
    }
  });
</script>

<article class={cn('flex gap-2', compact && 'pt-2.5')}>
  <CommentAuthorAvatar
    authorName={comment.authorName}
    authorId={comment.authorId}
    size={compact ? 'sm' : 'md'}
  />
  <div class="min-w-0 flex-1">
    <div class="mb-0.5 flex items-center gap-1.5">
      <span class="truncate text-[13px] font-semibold">
        {comment.authorName || $t`(unknown)`}
      </span>
      <FormatRelativeDate
        class="ms-auto shrink-0 text-[11px] text-muted-foreground"
        date={comment.createdAt}
        maxUnits={1}
        smallestUnit="minutes"
        options={{style: 'narrow'}}
      />
    </div>

    {#if editing}
      <div class="flex flex-col gap-1.5">
        <Textarea
          bind:value={draftText}
          aria-label={$t`Edit comment`}
          rows={3}
          disabled={saving}
          class="text-[13px]"
        />
        <div class="flex gap-1.5">
          <Button size="sm" onclick={() => onSaveEdit(draftText)} disabled={!draftText.trim()} loading={saving}>
            {$t`Save`}
          </Button>
          <Button variant="outline" size="sm" onclick={onCancelEdit} disabled={saving}>
            {$t`Cancel`}
          </Button>
        </div>
      </div>
    {:else}
      <p class={cn('m-0 text-[13px] leading-relaxed break-words whitespace-pre-wrap', !comment.text && 'text-muted-foreground')}>
        {comment.text || $t`Empty comment`}
      </p>
      {#if canEdit}
        <Button
          variant="ghost"
          size="xs"
          class="mt-0.5 h-5 px-1 text-[11px] text-muted-foreground"
          onclick={onStartEdit}
          disabled={saving}
        >
          {$t`Edit`}
        </Button>
      {/if}
    {/if}
  </div>
</article>
