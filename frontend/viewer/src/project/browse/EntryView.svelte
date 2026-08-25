<script lang="ts">
  import {Icon} from '$lib/components/ui/icon';
  import EntryEditor from '$lib/entry-editor/object-editors/EntryEditor.svelte';
  import {resource, Debounced, watch} from 'runed';
  import {useMiniLcmApi} from '$lib/services/service-provider';
  import {fade} from 'svelte/transition';
  import ViewPicker from './EditorViewOptions.svelte';
  import EntryMenu from './EntryMenu.svelte';
  import {ScrollArea} from '$lib/components/ui/scroll-area';
  import {cn} from '$lib/utils';
  import {useWritingSystemService} from '$project/data';
  import {t} from 'svelte-i18n-lingui';
  import {Toggle} from '$lib/components/ui/toggle';
  import {Button, XButton} from '$lib/components/ui/button';
  import type {IEntry} from '$lib/dotnet-types';
  import {copy, EntryPersistence} from '$lib/entry-editor/entry-persistence.svelte';
  import {createEntryOptions} from '$lib/create-entry-options';
  import {useProjectEventBus} from '$lib/services/event-bus';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {findFirstTabbable} from '$lib/utils/tabbable';
  import {useFeatures} from '$lib/services/feature-service';
  import type {ReadonlyDeep} from 'type-fest';
  import DictionaryEntry from '$lib/components/dictionary/DictionaryEntry.svelte';
  import * as Alert from '$lib/components/ui/alert';
  import {pt} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {useProjectStorage} from '$lib/storage/project-storage.svelte';
  import CommentDialog from '$lib/entry-editor/comments/CommentDialog.svelte';
  import {SubjectType} from '$lib/dotnet-types/generated-types/MiniLcm/Models/SubjectType';
  import type {IUserComment} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IUserComment';
  import DevContent from '$lib/layout/DevContent.svelte';
  import {ResizableHandle, ResizablePane, ResizablePaneGroup} from '$lib/components/ui/resizable';
  import {IsExtraLarge} from '$lib/hooks/is-extra-large.svelte';

  type DictionaryPreviewMode = 'show' | 'hide' | 'sticky';

  const writingSystemService = useWritingSystemService();
  const eventBus = useProjectEventBus();
  const miniLcmApi = useMiniLcmApi();
  const features = useFeatures();
  const viewService = useViewService();
  const dictionaryPreviewStorage = useProjectStorage().dictionaryPreview;
  let {
    entryId,
    onClose,
    showClose = false,
    showComments = $bindable(false),
  }: {
    entryId: string;
    onClose?: () => void;
    showClose?: boolean;
    showComments?: boolean;
  } = $props();

  // Reactive firewall:
  // When we delete the current entry, the resource gets retriggered with the same/deleted entryId
  // (due to parent updates/reactivity) and then getEntry returns undefined, so we lose the entry.
  // We want to keep the deleted entry in the view, so the user can optionally restore it.
  const dedupedEntryId = $derived(entryId);

  let editor = $state<EntryEditor>();

  const entryResource = resource(
    () => dedupedEntryId,
    async (id) => {
      await editor?.commit();
      const entry = await miniLcmApi.getEntry(id);
      // The fetcher's return value is what sets entryResource.current, so we must NOT also
      // call entryResource.mutate() here or current gets set twice and reactivity double-fires.
      return snapshotEntry(entry);
    },
  );

  function snapshotEntry(entry: IEntry | undefined): IEntry | undefined {
    // IMMEDIATELY take a snapshot to ensure it doesn't get mutated by the editor before EntryPersistence gets it.
    // (dirty fields immediately push their current dirty value into the entry object, which can corrupt the update diff.)
    latestPersistedSnapshot = entry ? Object.freeze(copy(entry)) : undefined;
    deleted = !!entry?.deletedAt;
    return entry;
  }

  // For entry updates that arrive OUTSIDE the resource fetcher (event bus, restore), we must
  // push the new value into the resource ourselves via mutate().
  function setEntry(entry: IEntry | undefined): IEntry | undefined {
    snapshotEntry(entry);
    entryResource.mutate(entry);
    return entry;
  }

  eventBus.onEntryUpdated((id) => {
    if (id !== entryId) return;
    void miniLcmApi.getEntry(id).then(refreshed => {
      if (id === entryId && refreshed) setEntry(refreshed); // entryId may have changed mid-fetch
    });
  });

  eventBus.onEntryDeleted(id => {
    if (id === entryId) {
      deleted = true;
    }
  });

  async function restore() {
    if (!entry) return;
    const restoredEntry = await miniLcmApi.createEntry(entry, createEntryOptions.asIs);
    setEntry(restoredEntry);
  }

  let latestPersistedSnapshot = $state<ReadonlyDeep<IEntry>>();
  const entryPersistence = new EntryPersistence(() => latestPersistedSnapshot);
  let entry = $derived(entryResource.current ?? undefined);
  const headword = $derived((entry && writingSystemService.headword(entry)) || $t`Untitled`);
  const loadingDebounced = new Debounced(() => entryResource.loading, 50);
  const dictionaryPreview: DictionaryPreviewMode = $derived(
    isDictionaryPreviewMode(dictionaryPreviewStorage.current) ? dictionaryPreviewStorage.current : 'show'
  );
  function isDictionaryPreviewMode(value: string): value is DictionaryPreviewMode {
    return value === 'show' || value === 'hide' || value === 'sticky';
  }
  const sticky = $derived(dictionaryPreview === 'sticky');

  let deleted = $state(false);
  const showCommentDialog = $derived(showComments && features.comments);

  const entryUnreadResource = resource(
    () => (features.comments ? dedupedEntryId : undefined),
    async (id): Promise<IUserComment[]> => {
      if (!id) return [];
      return miniLcmApi.getUnreadCommentsForSubject(SubjectType.Entry, id);
    },
    {initialValue: [] satisfies IUserComment[]},
  );
  const entryUnreadCount = $derived(entryUnreadResource.current.length);

  watch(
    () => showCommentDialog,
    (isOpen) => {
      if (isOpen && features.comments) void entryUnreadResource.refetch();
    },
  );

  // Entry and comments share the space instead of the comments floating over the entry:
  // side by side once there's room, stacked below xl.
  const commentsDirection = $derived(IsExtraLarge.value ? 'horizontal' : 'vertical');
  const commentsLayout = $derived(IsExtraLarge.value ? [65, 35] as const : [55, 45] as const);
  let entryPane = $state<ResizablePane>();
  let commentsPane = $state<ResizablePane>();

  const loadedEntryId = $derived(entry?.id);
  let entryScrollViewportRef: HTMLElement | null = $state(null);
  let editorRef: HTMLElement | null = $state(null);
  watch([() => [loadedEntryId, entryScrollViewportRef, editorRef]], () => {
    entryScrollViewportRef?.scrollTo({ top: 0, left: 0 });
    if (!IsMobile.value) findFirstTabbable(editorRef)?.focus();
  });
</script>

{#snippet preview(entry: IEntry)}
  <div class="md:pb-3">
    <DictionaryEntry {entry} showLinks class={cn('rounded bg-muted/80 dark:bg-muted/50 p-4')}>
      {#snippet actions()}
        <Toggle bind:pressed={() => sticky, (value) => void dictionaryPreviewStorage.set(value ? 'sticky' : 'show')}
          aria-label={$t`Toggle pinned`} class="aspect-square" size="sm">
          <Icon icon="i-mdi-pin-outline" class="size-5" />
        </Toggle>
      {/snippet}
    </DictionaryEntry>
  </div>
{/snippet}

<div class="h-full flex flex-col relative">
  {#if entry}
    <header>
      <div class="max-md:p-2 md:mb-4 flex justify-between">
        {#if showClose && onClose}
          <XButton onclick={onClose} size="icon" />
        {/if}
        <h2 class="ml-4 text-2xl font-semibold mb-2 inline">{headword}</h2>
        <div class="flex">
          <DevContent>
            {#if features.comments}
              <div class="relative">
                <Button
                  variant="ghost"
                  size="icon"
                  icon={showCommentDialog ? 'i-mdi-comment-text' : 'i-mdi-comment-text-outline'}
                  aria-pressed={showCommentDialog}
                  aria-label={entryUnreadCount > 0
                    ? $t`Comments, ${entryUnreadCount} unread`
                    : $t`Comments`}
                  onclick={() => showComments = !showComments}
                />
                {#if entryUnreadCount > 0}
                  <span
                    class="pointer-events-none absolute top-1.5 right-1.5 size-2 rounded-full bg-primary ring-2 ring-background"
                    aria-hidden="true"
                  ></span>
                {/if}
              </div>
            {/if}
          </DevContent>
          <ViewPicker bind:dictionaryPreview={() => dictionaryPreview, (v) => void dictionaryPreviewStorage.set(v)} />
          <EntryMenu {entry} />
        </div>
      </div>
      {#if deleted}
        {@const entity = pt($t`entry`, $t`word`, viewService.currentView)}
        <div class="mb-2 px-2">
          <Alert.Root variant="destructive">
            <Alert.Description class="flex justify-between items-center">
              <span class="inline-flex gap-2">
                <Icon icon="i-mdi-alert-circle" class="size-5" />
                {$t`This ${entity} was deleted`}
              </span>
              {#if features.write}
                <Button size="sm" variant="secondary" onclick={() => restore()}>
                  {$t`Restore`}
                </Button>
              {/if}
            </Alert.Description>
          </Alert.Root>
        </div>
      {/if}
    </header>
    {#snippet entryColumn(currentEntry: IEntry)}
      <div class="flex min-h-0 min-w-0 grow flex-col">
        {#if dictionaryPreview === 'sticky'}
          <div class="shrink-0 md:px-2">
            {@render preview(currentEntry)}
          </div>
        {/if}
        <ScrollArea bind:viewportRef={entryScrollViewportRef} class={cn('min-w-0 grow md:pr-2')}>
          {#if dictionaryPreview === 'show'}
            <div class="md:pl-2">
              {@render preview(currentEntry)}
            </div>
          {/if}
          <div class="max-md:p-2 md:pt-1 md:pb-2 md:px-2">
            {#key currentEntry.id}
              <EntryEditor
                bind:this={editor}
                bind:ref={editorRef}
                bind:entry={() => currentEntry, (updated) => entry = updated}
                readonly={!features.write || deleted}
                {...entryPersistence.entryEditorProps} />
            {/key}
          </div>
        </ScrollArea>
      </div>
    {/snippet}
    <div class="flex min-h-0 grow">
      {#if showCommentDialog}
        <!-- Re-mount on direction change so the panes pick up the layout for the new axis. -->
        {#key commentsDirection}
          <ResizablePaneGroup direction={commentsDirection} class="min-h-0 grow">
            <ResizablePane bind:this={entryPane} defaultSize={commentsLayout[0]} minSize={25} class="flex min-h-0 min-w-0 flex-col">
              {@render entryColumn(entry)}
            </ResizablePane>
            <ResizableHandle
              withHandle
              variant={commentsDirection === 'vertical' ? 'grab-bar' : 'divider'}
              leftPane={entryPane}
              rightPane={commentsPane}
              resetTo={commentsLayout}
              class="my-2 data-[direction=vertical]:my-0"
            />
            <ResizablePane bind:this={commentsPane} defaultSize={commentsLayout[1]} minSize={20} class="flex min-h-0 min-w-0 flex-col">
              <CommentDialog
                bind:open={() => showCommentDialog, (v) => showComments = v}
                inlineSidebar
                subjectType={SubjectType.Entry}
                subjectId={entry.id}
                subjectName={headword}
                unreadComments={entryUnreadResource.current}
                onUnreadCommentsChange={(comments) => entryUnreadResource.mutate(comments)}
                class={commentsDirection === 'vertical'
                  ? 'rounded-none border-0 shadow-none'
                  : undefined}
              />
            </ResizablePane>
          </ResizablePaneGroup>
        {/key}
      {:else}
        {@render entryColumn(entry)}
      {/if}
    </div>
  {/if}
  {#if loadingDebounced.current && entryResource.current?.id !== entryId}
    <div
      class="absolute inset-0 opacity-50 bg-background z-10"
      transition:fade={{ duration: 150 }}>
      <Icon icon="i-mdi-loading" class="absolute inset-0 animate-spin m-auto size-12"></Icon>
    </div>
  {/if}
</div>
