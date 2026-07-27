<script lang="ts">
  import * as Drawer from '$lib/components/ui/drawer';
  import * as Sheet from '$lib/components/ui/sheet';
  import * as InputGroup from '$lib/components/ui/input-group';
  import {Button, buttonVariants} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {Label} from '$lib/components/ui/label';
  import {Textarea} from '$lib/components/ui/textarea';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {useMiniLcmApi} from '$lib/services/service-provider';
  import {useProjectContext} from '$project/project-context.svelte';
  import {cn, randomId} from '$lib/utils';
  import type {ICommentThread} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ICommentThread';
  import type {IUserComment} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IUserComment';
  import type {SubjectType} from '$lib/dotnet-types/generated-types/MiniLcm/Models/SubjectType';
  import {ThreadStatus} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ThreadStatus';
  import type {ClassValue} from 'clsx';
  import {resource} from 'runed';
  import {MediaQuery} from 'svelte/reactivity';
  import {t} from 'svelte-i18n-lingui';

  type ThreadView = {
    thread: ICommentThread;
    comments: IUserComment[];
  };

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
  let replyTextByThreadId = $state<Record<string, string>>({});
  let editingCommentId = $state<string>();
  let editTextByCommentId = $state<Record<string, string>>({});

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

  async function replyToThread(threadView: ThreadView): Promise<void> {
    const threadId = threadView.thread.id;
    const text = replyTextByThreadId[threadId]?.trim();
    if (!text) return;

    saving = true;
    try {
      const now = new Date().toISOString();
      await api.addUserComment(threadId, {
        id: randomId(),
        commentThreadId: threadId,
        previousCommentId: threadView.comments.at(-1)?.id,
        text,
        createdAt: now,
        updatedAt: now,
      });
      replyTextByThreadId[threadId] = '';
      await threadsResource.refetch();
    } finally {
      saving = false;
    }
  }

  function startEditing(comment: IUserComment): void {
    editingCommentId = comment.id;
    editTextByCommentId[comment.id] = comment.text;
  }

  function cancelEditing(commentId: string): void {
    editingCommentId = undefined;
    delete editTextByCommentId[commentId];
  }

  async function saveEdit(commentId: string): Promise<void> {
    const text = editTextByCommentId[commentId]?.trim();
    if (!text) return;

    saving = true;
    try {
      await api.editUserComment(commentId, text);
      cancelEditing(commentId);
      await threadsResource.refetch();
    } finally {
      saving = false;
    }
  }

  function submitOnCtrlEnter(event: KeyboardEvent): void {
    if (event.key === 'Enter' && event.ctrlKey) {
      const target = event.target as HTMLTextAreaElement | null;
      target?.form?.requestSubmit();
      event.preventDefault();
    }
  }
</script>

{#snippet commentContent()}
  <div class="flex min-h-0 flex-1 flex-col overflow-hidden">
    <div class="min-h-0 flex-1 space-y-4 overflow-y-auto px-4 pb-4">
      <form onsubmit={(e) => { e.preventDefault(); void startThread(); }} class="space-y-2">
        <Label for="new-comment-thread">{$t`Start a new thread`}</Label>
        {#if canComment}
          <InputGroup.Root>
            <InputGroup.Textarea
              id="new-comment-thread"
              bind:value={newThreadText}
              placeholder={$t`Write the first comment...`}
              rows={2}
              disabled={loading || saving}
              onkeydown={submitOnCtrlEnter}
              class="min-h-4 py-1"
            />
            <InputGroup.Addon align="block-end" class="flex-row-reverse">
              <InputGroup.Button
                variant="default"
                size="icon-xs"
                icon="i-mdi-send"
                class="p-0! [&>.icon-wrapper]:flex [&>.icon-wrapper]:items-center [&>.icon-wrapper]:justify-center"
                iconProps={{ class: 'size-4' }}
                type="submit" disabled={!newThreadText.trim() || loading} loading={saving}
              />
            </InputGroup.Addon>
          </InputGroup.Root>
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
                  <article class="rounded-md bg-muted/60 p-3">
                    <div class="mb-2 flex items-center justify-between gap-2">
                      <span class="text-xs font-medium text-muted-foreground">
                        {comment.authorName || $t`(unknown)`}
                      </span>
                      {#if currentUserId && comment.authorId === currentUserId}
                        <Button
                          variant="ghost"
                          size="xs"
                          onclick={() => startEditing(comment)}
                          disabled={saving || editingCommentId === comment.id}
                        >
                          {$t`Edit`}
                        </Button>
                      {/if}
                    </div>

                    {#if editingCommentId === comment.id}
                      <div class="space-y-2">
                        <Textarea
                          bind:value={editTextByCommentId[comment.id]}
                          aria-label={$t`Edit comment`}
                          rows={3}
                          disabled={saving}
                        />
                        <div class="flex justify-end gap-2">
                          <Button variant="secondary" size="sm" onclick={() => cancelEditing(comment.id)} disabled={saving}>
                            {$t`Cancel`}
                          </Button>
                          <Button
                            size="sm"
                            onclick={() => void saveEdit(comment.id)}
                            disabled={!editTextByCommentId[comment.id]?.trim()}
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
                {/each}
              </div>

              <div class="space-y-2">
                {#if canComment}
                  <form onsubmit={(e) => { e.preventDefault(); void replyToThread(threadView); }} class="space-y-2">
                    <InputGroup.Root>
                      <InputGroup.Textarea
                        id={`reply-${threadView.thread.id}`}
                        bind:value={replyTextByThreadId[threadView.thread.id]}
                        placeholder={$t`Write a reply...`}
                        class="min-h-4 py-1"
                        rows={1}
                        disabled={saving}
                        onkeydown={submitOnCtrlEnter}
                      />
                      <InputGroup.Addon align="block-end" class="flex-row-reverse">
                        <InputGroup.Button
                          variant="ghost"
                          size="icon-xs"
                          icon="i-mdi-send"
                          class="p-0! [&>.icon-wrapper]:flex [&>.icon-wrapper]:items-center [&>.icon-wrapper]:justify-center"
                          iconProps={{ class: 'size-4' }}
                          type="submit"
                          disabled={!replyTextByThreadId[threadView.thread.id]?.trim()}
                          loading={saving}
                        />
                      </InputGroup.Addon>
                    </InputGroup.Root>
                  </form>
                {/if}
              </div>
            </section>
          {/each}
        {/if}
      </div>
    </div>
  </div>
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
    {@render commentContent()}
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
        {@render commentContent()}
      </div>
    </Drawer.Content>
  </Drawer.Root>
{:else}
  <Sheet.Root bind:open={() => open, onOpenChange}>
    <Sheet.Content side="right" class="w-full overflow-hidden p-0 sm:max-w-md">
      <Sheet.Header class="px-4 pb-0">
        <Sheet.Title class="max-w-[calc(100%-2rem)] truncate pr-2" title={title}>{title}</Sheet.Title>
      </Sheet.Header>
      {@render commentContent()}
    </Sheet.Content>
  </Sheet.Root>
{/if}
