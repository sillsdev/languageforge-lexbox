<script lang="ts">
  import * as Drawer from '$lib/components/ui/drawer';
  import * as Sheet from '$lib/components/ui/sheet';
  import {Button, buttonVariants} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {IsExtraLarge} from '$lib/hooks/is-extra-large.svelte';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {useMiniLcmApi} from '$lib/services/service-provider';
  import {useProjectContext} from '$project/project-context.svelte';
  import {cn, randomId} from '$lib/utils';
  import type {IUserComment} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IUserComment';
  import type {SubjectType} from '$lib/dotnet-types/generated-types/MiniLcm/Models/SubjectType';
  import {ThreadStatus} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ThreadStatus';
  import type {ClassValue} from 'clsx';
  import {resource, watch} from 'runed';
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

  let saving = $state(false);
  let newThreadText = $state('');
  let editingCommentId = $state<string>();
  let showResolved = $state(false);
  let addingComment = $state(false);
  let expandedThreadIds = $state(new Set<string>());
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

  const title = $derived(subjectName ? $t`Comments for ${subjectName}` : $t`Comments`);
  const dockBottom = $derived(!IsExtraLarge.value);

  // Bottom-dock: snap points least→most visible. Default open height is the middle stop.
  const commentSnapPoints = [0.35, 0.55, 0.9] as const;
  const defaultCommentSnap = commentSnapPoints[1];
  let activeSnapPoint = $state<number | string | null>(defaultCommentSnap);

  function setOpen(value: boolean): void {
    if (value && dockBottom) activeSnapPoint = defaultCommentSnap;
    open = value;
  }

  watch(
    () => open,
    (isOpen) => {
      if (isOpen) {
        if (dockBottom) activeSnapPoint = defaultCommentSnap;
        return;
      }
      showResolved = false;
      addingComment = false;
      newThreadText = '';
      editingCommentId = undefined;
      expandedThreadIds = new Set();
      mobileThreadId = null;
      // Vaul assigns snapPoints[0] after close; restore default for the next open.
      window.setTimeout(() => {
        if (!open) activeSnapPoint = defaultCommentSnap;
      }, 500);
    },
  );

  watch(
    () => dockBottom,
    (isBottom) => {
      if (!isBottom) mobileThreadId = null;
      else expandedThreadIds = new Set();
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

  async function resolveThread(threadView: ThreadView): Promise<void> {
    saving = true;
    try {
      const nextStatus =
        threadView.thread.status === ThreadStatus.Closed ? ThreadStatus.Open : ThreadStatus.Closed;
      await api.setCommentThreadStatus(threadView.thread.id, nextStatus);
      await threadsResource.refetch();
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
    } finally {
      saving = false;
    }
  }
</script>

{#snippet panel()}
  <CommentPanel
    bind:newThreadText
    bind:showResolved
    bind:addingComment
    bind:expandedThreadIds
    bind:mobileThreadId
    {canComment}
    {loading}
    {saving}
    {threadViews}
    {editingCommentId}
    {currentUserId}
    onClose={() => onOpenChange(false)}
    onStartThread={startThread}
    onReply={replyToThread}
    onResolve={resolveThread}
    onStartEdit={startEditing}
    onCancelEdit={cancelEditing}
    onSaveEdit={saveEdit}
  />
{/snippet}

{#if inlineSidebar && open && !dockBottom}
  <aside
    class={cn(
      'flex h-full min-h-0 w-[360px] shrink-0 flex-col overflow-hidden rounded-lg border bg-background shadow-sm',
      className,
    )}
  >
    {@render panel()}
  </aside>
{:else if inlineSidebar && dockBottom}
  <Drawer.Root
    bind:open={() => open, onOpenChange}
    bind:activeSnapPoint
    modal={false}
    shouldScaleBackground={false}
    snapPoints={[...commentSnapPoints]}
    snapToSequentialPoint
  >
    <Drawer.Content
      handle={false}
      class="fixed mt-0 h-dvh max-h-dvh overflow-hidden p-0 data-[vaul-drawer-direction=bottom]:mt-0 data-[vaul-drawer-direction=bottom]:h-dvh data-[vaul-drawer-direction=bottom]:max-h-dvh"
    >
      <Drawer.Title class="sr-only">{title}</Drawer.Title>
      <!--
        Vaul translates a full-viewport sheet for snaps. Keep position:fixed (do not use
        relative — it overrides fixed) and size the panel to the active snap so the reply
        footer sits on the visible bottom edge. Custom handle: the default one sits under
        this absolute panel and would be covered.
      -->
      <div
        class="absolute inset-x-0 top-0 flex flex-col overflow-hidden bg-background"
        style:height="{(typeof activeSnapPoint === 'number' ? activeSnapPoint : defaultCommentSnap) * 100}dvh"
      >
        <div class="bg-muted mx-auto mt-4 h-2 w-[100px] shrink-0 rounded-full" aria-hidden="true"></div>
        <div class="flex min-h-0 flex-1 flex-col overflow-hidden">
          {@render panel()}
        </div>
      </div>
    </Drawer.Content>
  </Drawer.Root>
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
