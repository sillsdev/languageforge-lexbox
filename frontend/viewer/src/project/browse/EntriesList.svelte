<script lang="ts">
  import {type IEntry, type IPartOfSpeech, type ISemanticDomain} from '$lib/dotnet-types';
  import {Debounced, watch} from 'runed';
  import EntryRow from './EntryRow.svelte';
  import Button from '$lib/components/ui/button/button.svelte';
  import {cn} from '$lib/utils';
  import {t} from 'svelte-i18n-lingui';
  import DevContent from '$lib/layout/DevContent.svelte';
  import PrimaryNewEntryButton from '../PrimaryNewEntryButton.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {useProjectEventBus} from '$lib/services/event-bus';
  import EntryMenu from './EntryMenu.svelte';
  import FabContainer from '$lib/components/fab/fab-container.svelte';
  import {VList, type VListHandle} from 'virtua/svelte';
  import type {SortConfig} from './sort/options';
  import {AppNotification} from '$lib/notifications/notifications';
  import {Icon} from '$lib/components/ui/icon';
  import {useProjectContext} from '$project/project-context.svelte';
  import Delayed from '$lib/components/Delayed.svelte';
  import {EntryLoaderService} from '$lib/services/entry-loader-service.svelte';
  import {untrack} from 'svelte';

  let {
    search = '',
    selectedEntryId = undefined,
    partOfSpeech = undefined,
    semanticDomain = undefined,
    sort,
    onSelectEntry,
    gridifyFilter = undefined,
    previewDictionary = false,
    disableNewEntry = false,
    entryCount = $bindable(null),
  }: {
    search?: string;
    selectedEntryId?: string;
    partOfSpeech?: IPartOfSpeech;
    semanticDomain?: ISemanticDomain;
    sort?: SortConfig;
    onSelectEntry: (entry?: IEntry) => void;
    gridifyFilter?: string;
    previewDictionary?: boolean,
    disableNewEntry?: boolean,
    entryCount?: number | null,
  } = $props();
  const projectContext = useProjectContext();
  const miniLcmApi = $derived(projectContext.maybeApi);
  const dialogsService = useDialogsService();
  const projectEventBus = useProjectEventBus();

  // Create deps object reactively so watch can track changes
  // The closures maybe need to be created OUTSIDE untrack so they maintain reactivity
  const deps = {
    search: () => search,
    sort: () => sort,
    gridifyFilter: () => gridifyFilter,
  };

  let entryLoader = $derived(!miniLcmApi ? undefined : untrack(() => new EntryLoaderService(miniLcmApi, deps)));

  // Debounce the loading state for smoother UI
  const loading = new Debounced(() => entryLoader?.loading ?? true, 0);

  // Keep entryCount in sync
  $effect(() => {
    entryCount = entryLoader?.totalCount ?? null;
  });

  // Handle entry deleted events
  projectEventBus.onEntryDeleted(entryId => {
    entryLoader?.removeEntryById(entryId);
  });

  // Handle entry updated events
  projectEventBus.onEntryUpdated(entry => {
    void entryLoader?.updateEntry(entry).then(() => {
      // follow the selected entry if it "jumps"/reorders
      if (entry.id === selectedEntryId) {
        return tryToScrollToEntry(entry.id);
      }
    });
  });

  watch(
    () => entryLoader?.generation,
    () => void scrollToSelectedOrTop());

  $effect(() => {
    if (entryLoader?.error) {
      AppNotification.error($t`Failed to load entries`, entryLoader.error.message);
    }
  });

  // Generate a random number of skeleton rows between 10 and 13
  const skeletonRowCount = Math.ceil(Math.random() * 10) + 3;

  // Generate index array for virtual list.
  // We use a small number of skeletons if the total count is not yet known
  // to avoid a "white phase" between initial load and list initialization.
  const indexArray = $derived(
    entryLoader?.totalCount !== undefined
      ? Array.from({ length: entryLoader.totalCount }, (_, i) => i)
      : Array.from({ length: skeletonRowCount }, (_, i) => i)
  );

  async function handleNewEntry() {
    const entry = await dialogsService.createNewEntry(undefined, {
      semanticDomains: semanticDomain ? [semanticDomain] : [],
      partOfSpeech: partOfSpeech,
    });
    if (!entry) return;
    onSelectEntry(entry);
  }

  let vList = $state<VListHandle>();
  $effect(() => {
    if (!vList || !selectedEntryId || entryLoader?.loading !== false) return;

    void untrack(scrollToSelectedOrTop);
  });

  async function scrollToSelectedOrTop() {
    const currentId = selectedEntryId;
    if (!vList || !currentId) return;
    const scrolled = await tryToScrollToEntry(currentId);
    if (!scrolled && selectedEntryId === currentId) vList.scrollTo(0);
  }

  async function tryToScrollToEntry(entryId: string): Promise<boolean> {
    if (!entryLoader || !vList) return false;

    const index = await entryLoader.getOrLoadEntryIndex(entryId);
    if (entryId !== selectedEntryId) return false;
    if (index < 0) return false;

    const visibleStart = vList.getScrollOffset();
    const visibleSize = vList.getViewportSize();
    const visibleEnd = visibleStart + visibleSize;
    const itemStart = vList.getItemOffset(index);
    const itemSize = vList.getItemSize(index);
    const itemEnd = itemStart + itemSize;

    if (itemStart < visibleStart || itemEnd > visibleEnd) {
      //using smooth scroll caused lag, maybe only do it if scrolling a short distance?
      vList.scrollToIndex(index, { align: 'center' });
    }
    return true;
  }

  export async function selectNextEntry(): Promise<IEntry | undefined> {
    if (!entryLoader) return undefined;
    const indexOfSelected = selectedEntryId
      ? await entryLoader.getOrLoadEntryIndex(selectedEntryId)
      : -1;
    const nextIndex = indexOfSelected < 0 ? 0 : indexOfSelected + 1;

    // Check count bounds
    if (entryLoader.totalCount === undefined || nextIndex >= entryLoader.totalCount) {
      return undefined;
    }

    const nextEntry = await entryLoader.getOrLoadEntryByIndex(nextIndex);
    if (nextEntry) {
      onSelectEntry(nextEntry);
    }
    return nextEntry;
  }

</script>

<FabContainer>
  <DevContent>
    <Button
      icon={loading.current ? 'i-mdi-loading' : 'i-mdi-refresh'}
      variant="outline"
      iconProps={{ class: cn(loading.current && 'animate-spin') }}
      size="icon"
      onclick={() => entryLoader?.reset()}
    />
  </DevContent>
  {#if !disableNewEntry}
    <PrimaryNewEntryButton onclick={handleNewEntry} shortForm />
  {/if}
</FabContainer>

<div class="flex-1 h-full" role="table">
  {#if entryLoader?.error}
    <div class="flex items-center justify-center h-full text-muted-foreground gap-2">
      <Icon icon="i-mdi-alert-circle-outline" />
      <p>{$t`Failed to load entries`}</p>
    </div>
  {:else}
    <div class="h-full">
      {#if entryLoader?.totalCount === 0}
        <div class="flex items-center justify-center h-full text-muted-foreground">
          <p>{$t`No entries found`}</p>
        </div>
      {/if}

      <VList bind:this={vList}
            data={indexArray}
            class="h-full p-0.5 md:pr-3 after:h-12 after:block"
            getKey={(index: number) => `${entryLoader?.generation ?? EntryLoaderService.DEFAULT_GENERATION}-${index}`}
            bufferSize={450}>
        {#snippet children(index: number)}
          {#key entryLoader?.generation ?? EntryLoaderService.DEFAULT_GENERATION}
            <Delayed
              getCached={() => entryLoader?.getCachedEntryByIndex(index)}
              load={() => entryLoader?.getOrLoadEntryByIndex(index)}
              delay={250}
            >
              {#snippet children(state)}
                {#if state.loading || !state.current}
                  <!-- we want the initial loading state and the first loading entries
                  to share the same skeletons, so there's no flicker -->
                  <EntryRow class="mb-2" skeleton={true} />
                {:else}
                  {@const entry = state.current}
                  <EntryMenu {entry} contextMenu>
                    <EntryRow {entry}
                              class="mb-2"
                              selected={selectedEntryId === entry.id}
                              onclick={() => onSelectEntry(entry)}
                              {previewDictionary}/>
                  </EntryMenu>
                {/if}
              {/snippet}
            </Delayed>
          {/key}
        {/snippet}
      </VList>
    </div>
  {/if}
</div>
