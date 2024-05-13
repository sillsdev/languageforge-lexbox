<script lang="ts">
  import { mdiBookSearchOutline, mdiMagnifyRemoveOutline } from '@mdi/js';
  import { Dialog, Field, Icon, ListItem, TextField, cls } from 'svelte-ux';
  import { firstDefOrGlossVal, headword } from '../utils';
  import { useLexboxApi } from '../services/service-provider';
  import { derived, writable } from 'svelte/store';
  import { deriveAsync } from '../utils/time';
  import { createEventDispatcher } from 'svelte';
  import type { IEntry } from '../mini-lcm';

  const dispatch = createEventDispatcher<{
    entrySelected: IEntry;
  }>();

  let showSearchDialog = false;

  const lexboxApi = useLexboxApi();
  const search = writable<string | undefined>(undefined);
  const fetchCount = 105;
  const result = deriveAsync(search, async (s) => {
    if (!s) return Promise.resolve({ entries: [], search: undefined });

    const exemplar = s.charAt(0);
    const entries = await lexboxApi.SearchEntries(s ?? '', {
      offset: 0,
      count: fetchCount,
      order: {field: 'headword', writingSystem: 'default'},
      exemplar: exemplar ? {value: exemplar, writingSystem: 'default'} : undefined
    });
    return { entries, search: s};
  }, {entries: [], search: undefined}, 200);
  const displayedEntries = derived(result, (result) => {
    return result?.entries.slice(0, 5) ?? [];
  });
</script>

<Field
  classes={{ input: 'my-1 justify-center opacity-60' }}
  on:click={() => (showSearchDialog = true)}
  class="cursor-pointer opacity-80 hover:opacity-100">
  <div class="hidden sm:contents">
    Find entry... 🚀
  </div>
  <div class="contents sm:hidden">
    <Icon data={mdiBookSearchOutline} />
  </div>
</Field>

<Dialog bind:open={showSearchDialog} class="w-[700px]" classes={{root: 'items-start mt-4', title: 'p-2'}}>
  <div slot="title">
    <TextField
      autofocus
      clearable
      bind:value={$search}
      placeholder="Find entry..."
      class="flex-grow-[2] cursor-pointer opacity-80 hover:opacity-100"
      classes={{ prepend: 'text-sm'}}
      icon={mdiBookSearchOutline}
    />
  </div>
  <div>
    {#each $displayedEntries as entry}
      <ListItem
        title={headword(entry)}
        subheading={firstDefOrGlossVal(entry.senses[0])}
        class={cls('cursor-pointer', 'hover:bg-surface-300')}
        noShadow
        on:click={() => {
          dispatch('entrySelected', entry);
          showSearchDialog = false;
          $search = undefined;
        }}
      />
    {/each}
    {#if $displayedEntries.length === 0}
      <div class="p-4 text-center opacity-75">
        {#if $result.search}
          No entries found <Icon data={mdiMagnifyRemoveOutline} />
        {:else}
          Search for an entry <Icon data={mdiBookSearchOutline} />
        {/if}
      </div>
    {/if}
    {#if $result.entries.length > $displayedEntries.length}
      <div class="p-4 text-center opacity-75">
        {$result.entries.length - $displayedEntries.length}
        {#if $result.entries.length === fetchCount}+{/if}
        more matching entries...
      </div>
    {/if}
  </div>
</Dialog>
