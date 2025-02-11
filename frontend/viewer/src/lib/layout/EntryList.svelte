<script lang="ts">
  import { Button, Icon, InfiniteScroll, ListItem, ProgressCircle, TextField } from 'svelte-ux';
  import type { IEntry } from '$lib/dotnet-types';
  import { mdiArrowCollapseLeft, mdiArrowExpandRight, mdiBookOpenVariantOutline, mdiBookSearchOutline, mdiClose, mdiFormatListText } from '@mdi/js';
  import IndexCharacters from './IndexCharacters.svelte';
  import type { Writable } from 'svelte/store';
  import { createEventDispatcher, getContext } from 'svelte';
  import DictionaryEntry from '../DictionaryEntry.svelte';
  import {useCurrentView} from '$lib/views/view-service';
  import {fieldName} from '$lib/i18n';
  import {useWritingSystemService} from '$lib/writing-system-service';

  const dispatch = createEventDispatcher<{
    entrySelected: IEntry;
  }>();

  export let entries: IEntry[] | undefined;
  export let loading: boolean;
  export let search: string;
  export let expand: boolean;

  const writingSystemService = useWritingSystemService();

  const selectedEntry = getContext<Writable<IEntry | undefined>>('selectedEntry');
  let lastScrolledTo: string | undefined = undefined;

  $: {
    const selectedId = $selectedEntry?.id;
    const selectedDifferentEntry = selectedId && selectedId !== lastScrolledTo;

    // wait until the new entries have been rendered
    setTimeout(() => {
      const selectedEntryElem = scrollContainerElem?.querySelector('.selected-entry');
      if (selectedEntryElem) {
        if (selectedDifferentEntry) {
          lastScrolledTo = selectedId;
          selectedEntryElem?.scrollIntoView({block: 'nearest'});
        }
      }
    });
  }

  let scrollContainerElem: HTMLDivElement;

  const standardPageSize = 50;
  $: perPage = (!$selectedEntry || !entries)
    ? standardPageSize
    : Math.max(50, entries.indexOf($selectedEntry) + Math.ceil(standardPageSize / 2));

  function selectEntry(entry: IEntry) {
    $selectedEntry = entry;
    expand = false;
    dispatch('entrySelected', entry);
  }

  let dictionaryMode = false;

  const selectedCharacter = getContext<Writable<string | undefined>>('selectedIndexExamplar');
  const currentView = useCurrentView();
</script>

<div class="entry-list lg-view:flex flex-col gap-4 w-full justify-self-center">
  <div class="flex gap-3 w-full self-center sm-view:p-2">
    <IndexCharacters />
    <div class="grow">
      <TextField
        bind:value={search}
        placeholder={`Filter ${fieldName({id: 'entries'}, $currentView.i18nKey).toLowerCase()}...`}
        clearable
        classes={{ append: 'flex-row-reverse' }}
        icon={mdiBookSearchOutline}>
        <div slot="append" class="flex p-1">
          {#if loading}
            <ProgressCircle size={20} width={2} />
          {/if}
        </div>
      </TextField>
    </div>
    <Button icon={dictionaryMode ? mdiFormatListText : mdiBookOpenVariantOutline} variant="outline"
      class="text-field-sibling-button"
      rounded
      title={dictionaryMode ? 'Switch to list view' : 'Switch to dictionary view'}
      on:click={() => dictionaryMode = !dictionaryMode}>
    </Button>
    <div class="hidden lg-view:contents">
      <Button icon={expand ? mdiArrowCollapseLeft : mdiArrowExpandRight} variant="outline" iconOnly
        class="text-field-sibling-button"
        rounded
        title={expand ? 'Collapse list' : 'Expand list'}
        on:click={() => expand = !expand}>
      </Button>
    </div>
  </div>
  <div class="lg-view:border lg-view:rounded-md overflow-hidden flex">
    <div class="lg-view:overflow-auto w-full" bind:this={scrollContainerElem}>
      {#if !entries || entries.length == 0}
        <div class="p-4 text-center opacity-75">
          No entries found
          {#if $selectedCharacter}
            in
            <Button
              fullWidth
              class="border mb-2 w-auto inline ml-0.5"
              on:click={() => $selectedCharacter = undefined}
              size="sm">
              {$selectedCharacter}
              <Icon data={mdiClose} />
            </Button>
          {/if}
        </div>
      {:else}
        <InfiniteScroll {perPage} items={entries} let:visibleItems>
          {#each visibleItems as entry (entry.id)}
            <div class="entry" class:selected-entry={$selectedEntry == entry}>
              {#if dictionaryMode}
                <button class="p-2 w-full text-left"
                  on:click={() => selectEntry(entry)}>
                  <DictionaryEntry entry={entry} />
                </button>
              {:else}
                  <ListItem
                    title={writingSystemService.headword(entry).padStart(1, '–')}
                    subheading={writingSystemService.firstDefOrGlossVal(entry.senses[0]).padStart(1, '–')}
                    on:click={() => selectEntry(entry)}
                    noShadow
                    class="!rounded-none"
                  />
              {/if}
            </div>
          {/each}
        </InfiniteScroll>
      {/if}
    </div>
  </div>
</div>

<style lang="postcss">
  .entry {
    cursor: pointer;

    &.selected-entry > :global(*) {
      @apply lg-view:bg-surface-200;
    }

    &:hover > :global(*) {
      @apply bg-surface-300;
    }
  }
</style>
