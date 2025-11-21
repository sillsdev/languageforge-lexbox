<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import EntryEditor from '$lib/entry-editor/object-editors/EntryEditor.svelte';
  import {resource, Debounced, watch} from 'runed';
  import { useMiniLcmApi } from '$lib/services/service-provider';
  import { fade } from 'svelte/transition';
  import ViewPicker from './EditorViewOptions.svelte';
  import EntryMenu from './EntryMenu.svelte';
  import {ScrollArea} from '$lib/components/ui/scroll-area';
  import {cn} from '$lib/utils';
  import {useWritingSystemService} from '$project/data';
  import {t} from 'svelte-i18n-lingui';
  import {Toggle} from '$lib/components/ui/toggle';
  import {XButton} from '$lib/components/ui/button';
  import type {IEntry} from '$lib/dotnet-types';
  import {copy, EntryPersistence} from '$lib/entry-editor/entry-persistence.svelte';
  import {useProjectEventBus} from '$lib/services/event-bus';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {findFirstTabbable} from '$lib/utils/tabbable';
  import {useFeatures} from '$lib/services/feature-service';
  import type {ReadonlyDeep} from 'type-fest';
  import DictionaryEntry from '$lib/components/dictionary/DictionaryEntry.svelte';

  const writingSystemService = useWritingSystemService();
  const eventBus = useProjectEventBus();
  const miniLcmApi = useMiniLcmApi();
  const features = useFeatures();
  const {
    entryId,
    onClose,
    showClose = false,
  }: {
    entryId: string;
    onClose?: () => void;
    showClose?: boolean;
  } = $props();

  const entryResource = resource(
    () => entryId,
    async (id) => {
      const entry = await miniLcmApi.getEntry(id);
      // IMMEDIATELY take a snapshot to ensure it doesn't get mutated by the editor before EntryPersistence gets it.
      // (dirty fields immediately push their current dirty value into the entry object, which can corrupt the update diff.)
      latestPersistedSnapshot = entry ? Object.freeze(copy(entry)) : undefined;
      return entry;
    },
  );

  eventBus.onEntryUpdated((e) => {
    if (e.id === entryId) {
      void entryResource.refetch();
    }
  });

  let latestPersistedSnapshot = $state<ReadonlyDeep<IEntry>>();
  const entryPersistence = new EntryPersistence(() => latestPersistedSnapshot);
  const entry = $derived(entryResource.current ?? undefined);
  const headword = $derived((entry && writingSystemService.headword(entry)) || $t`Untitled`);
  const loadingDebounced = new Debounced(() => entryResource.loading, 50);
  let dictionaryPreview: 'show' | 'hide' | 'sticky' = $state('show');
  const sticky = $derived.by(() => dictionaryPreview === 'sticky');

  let readonly = $state(false);

  const loadedEntryId = $derived(entry?.id);
  let entryScrollViewportRef: HTMLElement | null = $state(null);
  let editorRef: HTMLElement | null = $state(null);
  watch([() => [loadedEntryId, entryScrollViewportRef, editorRef]], () => {
    entryScrollViewportRef?.scrollTo({ top: 0, left: 0 });
    if (!IsMobile.value) findFirstTabbable(editorRef)?.focus();
  });
</script>

{#snippet preview(entry: IEntry)}
  <div class="md:pb-4">
    <DictionaryEntry {entry} showLinks class={cn('rounded bg-muted/80 dark:bg-muted/50 p-4')}>
      {#snippet actions()}
        <Toggle bind:pressed={() => sticky, (value) => dictionaryPreview = value ? 'sticky' : 'show'}
          aria-label={$t`Toggle pinned`} class="aspect-square" size="xs">
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
          <ViewPicker bind:dictionaryPreview bind:readonly />
          <EntryMenu {entry} />
        </div>
      </div>
      {#if dictionaryPreview === 'sticky'}
        <div class="md:px-2">
          {@render preview(entry)}
        </div>
      {/if}
    </header>
    <ScrollArea bind:viewportRef={entryScrollViewportRef} class={cn('grow md:pr-2')}>
      {#if dictionaryPreview === 'show'}
        <div class="md:pl-2">
          {@render preview(entry)}
        </div>
      {/if}
      <div class="max-md:p-2 md:px-2">
        <EntryEditor bind:ref={editorRef} {entry} readonly={readonly || !features.write} {...entryPersistence.entryEditorProps} />
      </div>
    </ScrollArea>
  {/if}
  {#if loadingDebounced.current && entryResource.current?.id !== entryId}
    <div
      class="absolute inset-0 opacity-50 bg-background z-10"
      transition:fade={{ duration: 150 }}>
      <Icon icon="i-mdi-loading" class="absolute inset-0 animate-spin m-auto size-12"></Icon>
    </div>
  {/if}
</div>
