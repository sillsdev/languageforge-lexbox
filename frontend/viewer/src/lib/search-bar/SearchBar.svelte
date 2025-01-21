<script lang="ts">
  import {mdiArrowLeft, mdiBookPlusOutline, mdiBookSearchOutline, mdiMagnify, mdiMagnifyRemoveOutline} from '@mdi/js';
  import {Button, Dialog, Field, Icon, ListItem, ProgressCircle, TextField} from 'svelte-ux';
  import {firstDefOrGlossVal, headword} from '../utils';
  import {useLexboxApi} from '../services/service-provider';
  import {derived, type Writable} from 'svelte/store';
  import {deriveAsync} from '../utils/time';
  import {createEventDispatcher, getContext, onDestroy} from 'svelte';
  import {type IEntry, SortField} from '$lib/dotnet-types';
  import {useSearch} from './search';
  import {useCurrentView} from '$lib/services/view-service';
  import {fieldName} from '$lib/i18n';

  const {search, showSearchDialog} = useSearch();
  const dispatch = createEventDispatcher<{
    entrySelected: {entry: IEntry, search: string};
    createNew: string
  }>();

  export let createNew: boolean;

  let waitingForSecondShift = false;
  let waitingForSecondShiftTimeout: ReturnType<typeof setTimeout>;
  const abortController = new AbortController();
  document.addEventListener('keydown', (e) => {
    if (e.key !== 'Shift') {
      //cancel, user pressed shift and typed another letter
      waitingForSecondShift = false;
      clearTimeout(waitingForSecondShiftTimeout);
      return;
    }
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
    const entries = await lexboxApi.searchEntries(s ?? '', {
      offset: 0,
      count: fetchCount,
      order: {field: SortField.Headword, writingSystem: 'default', ascending: true},
    });
    return { entries, search: s};
  }, {entries: [], search: undefined}, 200);
  const displayedEntries = derived(result, (result) => {
    return result?.entries.slice(0, 5) ?? [];
  });

  const listSearch = getContext<Writable<string | undefined>>('listSearch');
  const selectedIndexExamplar = getContext<Writable<string | undefined>>('selectedIndexExamplar');
  const currentView = useCurrentView();

  function selectEntry(entry: IEntry) {
    dispatch('entrySelected', {entry, search: $search});
    $showSearchDialog = false;
  }

  function trimPastedText(e: ClipboardEvent) {
    console.log(e);
    if (e.clipboardData) {
      e.preventDefault();
      const text = e.clipboardData.getData('text');
      if (text !== null && text !== undefined) {
        $search = text.trim();
      }
    }
  }
  let searchElement: HTMLInputElement | null | undefined;
  $: if (searchElement) searchElement.addEventListener('paste', trimPastedText);
</script>

<button class="w-full cursor-pointer opacity-80 hover:opacity-100" on:click={() => ($showSearchDialog = true)}>
  <Field
    classes={{ input: 'my-1 justify-center opacity-60' }}
    class="cursor-pointer">
    <div class="hidden lg-view:contents whitespace-nowrap">
      Find {fieldName({id: 'entry'}, $currentView.i18nKey).toLowerCase()}...
      <span class="ml-2"><Icon data={mdiMagnify} /></span>
      <span class="ml-4"><span class="key">Shift</span>+<span class="key">Shift</span></span>
    </div>
    <div class="contents lg-view:hidden">
      <Icon data={mdiBookSearchOutline} />
    </div>
  </Field>
</button>

<Dialog bind:open={$showSearchDialog} on:close={() => $search = ''} classes={{root: 'items-start', title: 'px-2 py-0 max-md:pl-0'}}>
  <div slot="title" class="flex items-center h-12">
    <div class="hidden max-md:contents">
      <Button on:click={() => $showSearchDialog = false} icon={mdiArrowLeft} rounded="full"></Button>
    </div>
    <TextField
      bind:inputEl={searchElement}
      autofocus
      clearable
      bind:value={$search}
      on:keypress={(e) => {
        if (e.key === 'Enter' && $displayedEntries.length > 0) {
          e.preventDefault();
          selectEntry($displayedEntries[0]);
        }
      }}
      placeholder={`Find ${fieldName({id: 'entry'}, $currentView.i18nKey).toLowerCase()}...`}
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
    <div class="p-0.5">
      {#each $displayedEntries as entry}
        <ListItem
          title={headword(entry).padStart(1, '–')}
          subheading={firstDefOrGlossVal(entry.senses[0])}
          noShadow
          on:click={() => selectEntry(entry)}
        />
      {/each}
    </div>
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
    {#if $result.search && createNew}
      <ListItem
        title="Create new Entry..."
        icon={mdiBookPlusOutline}
        classes={{root: 'text-success py-4 border-none rounded m-0.5 hover:bg-success-900/25'}}
        noShadow
        on:click={() => {
            dispatch('createNew', $result.search ?? '');
            $showSearchDialog = false;
          }}
      />
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
