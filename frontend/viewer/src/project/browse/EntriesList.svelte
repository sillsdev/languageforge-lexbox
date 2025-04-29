<script lang="ts">
  import type { IEntry } from '$lib/dotnet-types';
  import type { IQueryOptions } from '$lib/dotnet-types/generated-types/MiniLcm/IQueryOptions';
  import { SortField } from '$lib/dotnet-types/generated-types/MiniLcm/SortField';
  import { Debounced, resource } from 'runed';
  import { useMiniLcmApi } from '$lib/services/service-provider';
  import EntryRow from './EntryRow.svelte';
  import Button from '$lib/components/ui/button/button.svelte';
  import { cn } from '$lib/utils';
  import { t } from 'svelte-i18n-lingui';
  import {ScrollArea} from '$lib/components/ui/scroll-area';
  import DevContent from '$lib/layout/DevContent.svelte';
  import NewEntryButton from '../NewEntryButton.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {useProjectEventBus} from '$lib/services/event-bus';
  import * as ContextMenu from '$lib/components/ui/context-menu';
  import {Icon} from '$lib/components/ui/icon';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';

  const {
    search = '',
    selectedEntry = undefined,
    sortDirection = 'asc',
    onSelectEntry,
    gridifyFilter = undefined,
  }: {
    search?: string;
    selectedEntry?: IEntry;
    sortDirection: 'asc' | 'desc';
    onSelectEntry: (entry?: IEntry) => void;
    gridifyFilter?: string;
  } = $props();
  const miniLcmApi = useMiniLcmApi();
  const dialogsService = useDialogsService();
  const writingSystemService = useWritingSystemService();
  const projectEventBus = useProjectEventBus();
  projectEventBus.onEntryDeleted(entryId => {
    if (entriesResource.loading || !entries.some(e => e.id === entryId)) return;
    entriesResource.refetch();
  });
  projectEventBus.onEntryUpdated(entry => {
    if (entriesResource.loading) return;
    entriesResource.refetch();
  });


  const entriesResource = resource(
    () => ({ search, sortDirection, gridifyFilter }),
    async ({ search, sortDirection, gridifyFilter }) => {
      const queryOptions: IQueryOptions = {
        count: 100,
        offset: 0,
        filter: {
          gridifyFilter: gridifyFilter ? gridifyFilter : undefined,
        },
        order: {
          field: SortField.Headword,
          writingSystem: 'default',
          ascending: sortDirection === 'asc',
        },
      };

      if (search) {
        return miniLcmApi.searchEntries(search, queryOptions);
      }
      return miniLcmApi.getEntries(queryOptions);
    },
  );
  const entries = $derived(entriesResource.current ?? []);
  const loading = new Debounced(() => entriesResource.loading, 50);

  // Generate a random number of skeleton rows between 3 and 7
  const skeletonRowCount = Math.floor(Math.random() * 5) + 3;

  async function handleNewEntry() {
    const entry = await dialogsService.createNewEntry();
    if (!entry) return;
    onSelectEntry(entry);
  }

  async function handleDeleteEntry(entry: IEntry) {
    const headword = writingSystemService.headword(entry);
    const entryId = entry.id;
    if (!await dialogsService.promptDelete($t`Entry: ${headword}`)) return;
    await miniLcmApi.deleteEntry(entryId);
    projectEventBus.notifyEntryDeleted(entryId);
    if (selectedEntry?.id === entryId) onSelectEntry(undefined);
  }
</script>

<div class="absolute bottom-0 right-0 m-4 flex flex-col items-end z-10">
  <DevContent>
    <Button
      icon="i-mdi-refresh"
      variant="secondary"
      iconProps={{ class: cn(loading.current && 'animate-spin') }}
      size="icon"
      class="mt-4 mb-6"
      onclick={() => entriesResource.refetch()}
    />
  </DevContent>
  <NewEntryButton onclick={handleNewEntry} shortForm />
</div>

<ScrollArea class="md:pr-5 flex-1" role="table">
  {#if entriesResource.error}
    <div class="flex items-center justify-center h-full text-muted-foreground">
      <p>{$t`Failed to load entries`}</p>
      <p>{entriesResource.error.message}</p>
    </div>
  {:else}
    <div class="space-y-2 p-0.5 pb-12">
      {#if loading.current}
        <!-- Show skeleton rows while loading -->
        {#each { length: skeletonRowCount }, _index}
          <EntryRow skeleton={true} />
        {/each}
      {:else}
        {#each entries as entry}
          <ContextMenu.Root>
            <ContextMenu.Trigger>
              <EntryRow {entry} isSelected={selectedEntry?.id === entry.id} onclick={() => onSelectEntry(entry)}/>
            </ContextMenu.Trigger>
            <ContextMenu.Content>
              <ContextMenu.Item class="cursor-pointer" onclick={() => handleDeleteEntry(entry)}>
                <Icon icon="i-mdi-delete" class="mr-2"/>
                {$t`Delete Entry`}
              </ContextMenu.Item>
            </ContextMenu.Content>
          </ContextMenu.Root>
        {:else}
          <div class="flex items-center justify-center h-full text-muted-foreground">
            <p>{$t`No entries found`}</p>
          </div>
        {/each}
      {/if}
    </div>
  {/if}
</ScrollArea>
