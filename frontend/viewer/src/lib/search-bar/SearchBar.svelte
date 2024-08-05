<script lang="ts">
  import { mdiBookSearchOutline, mdiMagnify, mdiMagnifyRemoveOutline } from '@mdi/js';
  import { Button, Dialog, Field, Icon, ListItem, ProgressCircle, TextField, cls } from 'svelte-ux';
  import { firstDefOrGlossVal, headword } from '../utils';
  import { useLexboxApi } from '../services/service-provider';
  import { derived, writable, type Writable } from 'svelte/store';
  import { deriveAsync } from '../utils/time';
  import { createEventDispatcher, getContext, onDestroy } from 'svelte';
  import type { IEntry } from '../mini-lcm';
  import {useSearch} from './search';

  const {search, showSearchDialog} = useSearch();
  const dispatch = createEventDispatcher<{
    entrySelected: IEntry;
  }>();

  let waitingForSecondShift = false;
  let waitingForSecondShiftTimeout: ReturnType<typeof setTimeout>;
  const abortController = new AbortController();
  document.addEventListener('keydown', (e) => {
    if (e.key !== 'Shift') return;
    if (waitingForSecondShift) {
      waitingForSecondShift = false;
      clearTimeout(waitingForSecondShiftTimeout);
      $showSearchDialog = true;
    } else {
      waitingForSecondShift = true;
      waitingForSecondShiftTimeout = setTimeout(() => {
        waitingForSecondShift = false;
      }, 500);
    }
  }, { signal: abortController.signal });

  onDestroy(() => {
    abortController.abort();
  });

  const lexboxApi = useLexboxApi();
  const fetchCount = 105;
  const { value: result, loading } = deriveAsync(search, async (s) => {
    if (!s) return Promise.resolve({ entries: [], search: undefined });
    const entries = await lexboxApi.SearchEntries(s ?? '', {
      offset: 0,
      count: fetchCount,
      order: {field: 'headword', writingSystem: 'default'},
    });
    return { entries, search: s};
  }, {entries: [], search: undefined}, 200);
  const displayedEntries = derived(result, (result) => {
    return result?.entries.slice(0, 5) ?? [];
  });

  const listSearch = getContext<Writable<string | undefined>>('listSearch');
  const selectedIndexExamplar = getContext<Writable<string | undefined>>('selectedIndexExamplar');
</script>

<Field
  classes={{ input: 'my-1 justify-center opacity-60' }}
  on:click={() => ($showSearchDialog = true)}
  class="cursor-pointer opacity-80 hover:opacity-100">
  <div class="hidden lg:contents">
    Find entry...
    <span class="ml-2"><Icon data={mdiMagnify} /></span>
    <span class="ml-4"><span class="key">Shift</span>+<span class="key">Shift</span></span>
  </div>
  <div class="contents lg:hidden">
    <Icon data={mdiBookSearchOutline} />
  </div>
</Field>

<Dialog bind:open={$showSearchDialog} on:close={() => $search = ''} class="w-[700px]" classes={{root: 'items-start', title: 'p-2'}}>
  <div slot="title">
    <TextField
      autofocus
      clearable
      bind:value={$search}
      placeholder="Find entry..."
      class="flex-grow-[2] cursor-pointer opacity-80 hover:opacity-100"
      classes={{ prepend: 'text-sm', append: 'flex-row-reverse'}}
      icon={mdiBookSearchOutline}>
      <div slot="append" class="flex p-1">
        {#if $loading}
          <ProgressCircle size={20} width={2} />
        {/if}
      </div>
    </TextField>
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
          $showSearchDialog = false;
        }}
      />
    {/each}
    {#if $displayedEntries.length === 0}
      <div class="p-4 text-center opacity-75">
        {#if $result.search}
          No entries found <Icon data={mdiMagnifyRemoveOutline} />
        {:else}
          {#if $loading}
            <ProgressCircle size={30} />
          {:else}
            Search for an entry <Icon data={mdiBookSearchOutline} />
          {/if}
        {/if}
      </div>
    {/if}
    {#if $result.entries.length > $displayedEntries.length}
      <div class="p-4 text-center opacity-75 flex items-center">
        <span>{$result.entries.length - $displayedEntries.length}</span>
        {#if $result.entries.length === fetchCount}<span>+</span>{/if}
        <div class="ml-1 flex justify-between items-center gap-2">
          <span>more matching entries...</span>
          <Button
            fullWidth
            icon={mdiBookSearchOutline}
            on:click={() => {
              $listSearch = $search;
              $selectedIndexExamplar = undefined;
              $showSearchDialog = false;
            }}
            class="border w-auto inline ml-0.5">
            Filter list
          </Button>
        </div>
      </div>
    {/if}
  </div>
</Dialog>
