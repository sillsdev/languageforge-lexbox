<script lang="ts">
  import * as Drawer from '$lib/components/ui/drawer';
  import * as Sheet from '$lib/components/ui/sheet';
  import {Button, buttonVariants} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {useMiniLcmApi} from '$lib/services/service-provider';
  import {useProjectContext} from '$project/project-context.svelte';
  import {cn, randomId} from '$lib/utils';
  import type {IUserComment} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IUserComment';
  import type {SubjectType} from '$lib/dotnet-types/generated-types/MiniLcm/Models/SubjectType';
  import {ThreadStatus} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ThreadStatus';
  import type {ClassValue} from 'clsx';
  import {resource} from 'runed';
  import {MediaQuery} from 'svelte/reactivity';
  import {t} from 'svelte-i18n-lingui';
  import CommentPanel from './CommentPanel.svelte';
  import type {ThreadView} from './types';

  let {
    open = $bindable(false),
    subjectType,
    subjectId,
    subjectName,
    inlineSidebar = false,
    class: className,
  }: {
    open: boolean;
    subjectType: SubjectType;
    subjectId: string;
    subjectName?: string;
    inlineSidebar?: boolean;
    class?: ClassValue;
  } = $props();

  const api = useMiniLcmApi();
  const projectContext = useProjectContext();
  const currentUserId = $derived(projectContext.projectData?.lastUserId);
  const canComment = $derived(Boolean(currentUserId));
  const isExtraLarge = new MediaQuery('min-width: 1280px');

  let saving = $state(false);
  let newThreadText = $state('');
  let editingCommentId = $state<string>();

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

  const title = $derived(subjectName ? $t`Comments for ${subjectName}` : $t`Comments`);
  const hasThreads = $derived(threadViews.length > 0);
  const shouldUseSidebar = $derived(inlineSidebar && isExtraLarge.current);

  function onOpenChange(value: boolean): void {
    open = value;
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
      await threadsResource.refetch();
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
    } finally {
      saving = false;
    }
  }

  function startEditing(comment: IUserComment): void {
    editingCommentId = comment.id;
  }

  function cancelEditing(commentId: string): void {
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
    } finally {
      saving = false;
    }
  }
</script>

{#snippet panel()}
  <CommentPanel
    bind:newThreadText
    {canComment}
    {loading}
    {saving}
    {hasThreads}
    {threadViews}
    {editingCommentId}
    {currentUserId}
    onStartThread={startThread}
    onReply={replyToThread}
    onStartEdit={startEditing}
    onCancelEdit={cancelEditing}
    onSaveEdit={saveEdit}
  />
{/snippet}

{#if shouldUseSidebar && open}
  <aside
    class={cn(
      'flex h-full min-h-0 w-96 shrink-0 flex-col overflow-hidden rounded-lg border bg-background shadow-sm',
      className,
    )}
  >
    <div class="flex items-start gap-2 px-4 pt-4 pb-0">
      <div class="min-w-0 flex-1">
        <h2 class="truncate text-lg font-semibold" title={title}>{title}</h2>
      </div>
      <Button variant="ghost" size="icon" aria-label={$t`Close`} onclick={() => onOpenChange(false)}>
        <Icon icon="i-mdi-close" />
      </Button>
    </div>
    {@render panel()}
  </aside>
{:else if IsMobile.value}
  <Drawer.Root bind:open={() => open, onOpenChange}>
    <Drawer.Content class="max-h-[90dvh] overflow-hidden">
      <Drawer.Close
        class={buttonVariants({variant: 'ghost', size: 'icon', class: 'absolute top-4 right-4 z-10'})}
        aria-label={$t`Close`}
      >
        <Icon icon="i-mdi-close" />
      </Drawer.Close>

      <div class="mx-auto flex max-h-[90dvh] w-full max-w-lg flex-1 flex-col overflow-hidden">
        <Drawer.Header class="px-4 text-left">
          <Drawer.Title class="max-w-[calc(100%-2rem)] truncate pr-2 text-left" title={title}>{title}</Drawer.Title>
        </Drawer.Header>
        {@render panel()}
      </div>
    </Drawer.Content>
  </Drawer.Root>
{:else}
  <Sheet.Root bind:open={() => open, onOpenChange}>
    <Sheet.Content side="right" class="w-full overflow-hidden p-0 sm:max-w-md">
      <Sheet.Header class="px-4 pb-0">
        <Sheet.Title class="max-w-[calc(100%-2rem)] truncate pr-2" title={title}>{title}</Sheet.Title>
      </Sheet.Header>
      {@render panel()}
    </Sheet.Content>
  </Sheet.Root>
{/if}
