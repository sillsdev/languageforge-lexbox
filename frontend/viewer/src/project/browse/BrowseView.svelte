<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import { ResizableHandle, ResizablePane, ResizablePaneGroup } from '$lib/components/ui/resizable';
  import { useMiniLcmApi } from '$lib/services/service-provider';
  import type { IEntry } from '$lib/dotnet-types';
  import { useWritingSystemRunes } from '$lib/writing-system-runes.svelte';
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import { t } from 'svelte-i18n-lingui';
  import {SidebarTrigger} from '$lib/components/ui/sidebar';
  import EntryRow from './EntryRow.svelte';

  const miniLcmApi = useMiniLcmApi();
  //todo not the best way to do this, but it works until we can make writingSystems a state field in the ws service
  const writingSystemService = $derived(useWritingSystemRunes());

  let entries = $state<IEntry[]>([]);
  let selectedEntry = $state<IEntry | undefined>(undefined);
  let loading = $state(true);
  const defaultLayout = [30, 70]; // Default split: 30% for list, 70% for details
  const isMobile = new IsMobile();

  $effect(() => {
    async function loadEntries() {
      try {
        entries = await miniLcmApi.getEntries(undefined);
        if (!isMobile.current) selectedEntry = entries[0];
      } catch (error) {
        console.error('Failed to load entries:', error);
      } finally {
        loading = false;
      }
    }
    void loadEntries();
  });
</script>

<div class="flex flex-col h-full p-4">
  <header class="flex items-center gap-2 mb-4 flex-none pl-8">
    <Icon icon="i-mdi-book-open-variant" class="size-5" />
    <h1 class="text-xl font-semibold">{$t`Dictionary Browser`}</h1>
  </header>

  <ResizablePaneGroup direction="horizontal" class="flex-1 min-h-0 overflow-hidden">
    {#if !isMobile.current || !selectedEntry}
      <ResizablePane defaultSize={defaultLayout[0]} collapsible collapsedSize={0} minSize={15} class="min-h-0 flex flex-col">
        <div class="overflow-y-auto flex-1 pr-4">
          <div class="space-y-2">
            {#if loading}
              <div class="flex items-center justify-center h-full text-muted-foreground">
                <p>Loading entries...</p>
              </div>
            {:else if entries.length === 0}
              <div class="flex items-center justify-center h-full text-muted-foreground">
                <p>No entries found</p>
              </div>
            {:else}
              {#each entries as entry}
                <EntryRow
                  entry={entry}
                  isSelected={selectedEntry === entry}
                  onclick={() => (selectedEntry = entry)}
                />
              {/each}
            {/if}
          </div>
        </div>
      </ResizablePane>
    {/if}
    {#if !isMobile.current}
      <ResizableHandle withHandle />
    {/if}
    {#if !isMobile.current || selectedEntry}
      <ResizablePane defaultSize={defaultLayout[1]} collapsible collapsedSize={0} minSize={15}>
        <div class="h-full p-6">
          {#if selectedEntry}
            <div class="mb-4">
              {#if isMobile.current}
                <Icon icon="i-mdi-close" onclick={() => (selectedEntry = undefined)}></Icon>
              {/if}
              <h2 class="text-2xl font-semibold mb-2">{writingSystemService.headword(selectedEntry) || 'Untitled'}</h2>
            </div>
            {#if selectedEntry.senses?.length > 0}
              {#each selectedEntry.senses as sense}
                {@const firstDef = writingSystemService.firstDef(sense)}
                {@const firstGloss = writingSystemService.firstGloss(sense)}
                <div class="mb-4">
                  {#if firstGloss}
                    <div class="text-sm text-muted-foreground">{firstGloss}</div>
                  {/if}
                  {#if firstDef}
                    <p class="mt-1">{firstDef}</p>
                  {/if}
                </div>
              {/each}
            {:else}
              <div class="text-muted-foreground">No senses defined</div>
            {/if}
          {:else}
            <div class="flex items-center justify-center h-full text-muted-foreground">
              <p>Select an entry to view details</p>
            </div>
          {/if}
        </div>
      </ResizablePane>
    {/if}
  </ResizablePaneGroup>
</div>
