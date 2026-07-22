<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {Textarea} from '$lib/components/ui/textarea';
  import type {IUserComment} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IUserComment';
  import {cn} from '$lib/utils';
  import {t} from 'svelte-i18n-lingui';

  let {
    comment,
    canEdit,
    saving,
    editing,
    onStartEdit,
    onCancelEdit,
    onSaveEdit,
  }: {
    comment: IUserComment;
    canEdit: boolean;
    saving: boolean;
    editing: boolean;
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

<article class="rounded-md bg-muted/60 p-3">
  <div class="mb-2 flex items-center justify-between gap-2">
    <span class="text-xs font-medium text-muted-foreground">
      {comment.authorName || $t`(unknown)`}
    </span>
    {#if canEdit}
      <Button
        variant="ghost"
        size="xs"
        onclick={onStartEdit}
        disabled={saving || editing}
      >
        {$t`Edit`}
      </Button>
    {/if}
  </div>

  {#if editing}
    <div class="space-y-2">
      <Textarea
        bind:value={draftText}
        aria-label={$t`Edit comment`}
        rows={3}
        disabled={saving}
      />
      <div class="flex justify-end gap-2">
        <Button variant="secondary" size="sm" onclick={onCancelEdit} disabled={saving}>
          {$t`Cancel`}
        </Button>
        <Button
          size="sm"
          onclick={() => onSaveEdit(draftText)}
          disabled={!draftText.trim()}
          loading={saving}
        >
          {$t`Save`}
        </Button>
      </div>
    </div>
  {:else}
    <p class={cn('whitespace-pre-wrap text-sm', !comment.text && 'text-muted-foreground')}>
      {comment.text || $t`Empty comment`}
    </p>
  {/if}
</article>
