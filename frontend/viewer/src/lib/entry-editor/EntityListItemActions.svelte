<script lang="ts" generics="T">
  import HistoryView from '../history/HistoryView.svelte';
  import {useFeatures} from '$lib/services/feature-service';
  import { Reorderer } from '$lib/components/reorderer';
  import {Button} from '$lib/components/ui/button';
  import CommentDialog from './CommentDialog.svelte';
  import type {SubjectType} from '$lib/dotnet-types/generated-types/MiniLcm/Models/SubjectType';

  type Props<T> = {
    items: T[];
    i: number;
    id?: string;
    subjectType?: SubjectType;
    getDisplayName: (item: T) => string | undefined;
    getCommentSubjectName?: (item: T, index: number) => string | undefined;
    readonly: boolean;
    onmove?: (newIndex: number) => void;
    ondelete?: () => void;
  };

  let {
    items = $bindable(),
    i,
    id,
    subjectType,
    getDisplayName,
    getCommentSubjectName,
    readonly,
    onmove,
    ondelete,
  }: Props<T> = $props();

  const features = useFeatures();

  let showHistoryView = $state(false);
  let showCommentDialog = $state(false);
  const item = $derived(items[i]);
  const subjectName = $derived(item ? getCommentSubjectName?.(item, i) ?? getDisplayName(item) : undefined);
</script>

{#if !readonly || features.history || (subjectType && id)}
<div class="flex gap-2">
  {#if !readonly}
    <Reorderer
      direction="vertical"
      {item}
      {items}
      {getDisplayName}
      onchange={(_newItems, _fromIndex, newIndex) => onmove?.(newIndex)}
    />
  {/if}
  {#if subjectType && id}
    <Button onclick={() => showCommentDialog = true} size="icon" variant="secondary" icon="i-mdi-comment-text-outline" />
    <CommentDialog bind:open={showCommentDialog} {subjectType} subjectId={id} {subjectName} />
  {/if}
  {#if features.history && id}
    <Button onclick={() => showHistoryView = true} size="icon" variant="secondary" icon="i-mdi-history" />
    <HistoryView bind:open={showHistoryView} {id}/>
  {/if}
  {#if !readonly}
    <Button onclick={ondelete} size="icon" variant="secondary" icon="i-mdi-trash-can" />
  {/if}
</div>
{/if}
