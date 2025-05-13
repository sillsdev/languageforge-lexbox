<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import EntryEditor from '$lib/entry-editor/object-editors/EntryEditor.svelte';
  import { useViewSettings } from '$lib/views/view-service';
  import {resource, Debounced} from 'runed';
  import { useMiniLcmApi } from '$lib/services/service-provider';
  import { fade } from 'svelte/transition';
  import ViewPicker from './ViewPicker.svelte';
  import EntryMenu from './EntryMenu.svelte';
  import {ScrollArea} from '$lib/components/ui/scroll-area';
  import {cn} from '$lib/utils';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import {t} from 'svelte-i18n-lingui';
  import DictionaryEntry from '$lib/DictionaryEntry.svelte';
  import {Toggle} from '$lib/components/ui/toggle';
  import {XButton} from '$lib/components/ui/button';
  import type {IEntry} from '$lib/dotnet-types';

  const viewSettings = useViewSettings();
  const writingSystemService = useWritingSystemService();
  const miniLcmApi = useMiniLcmApi();
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
      return miniLcmApi.getEntry(id);
    },
  );
  const entry = $derived(entryResource.current ?? undefined);
  const headword = $derived((entry && writingSystemService.headword(entry)) || $t`Untitled`);
  const loadingDebounced = new Debounced(() => entryResource.loading, 50);
  let dictionaryPreview: 'show' | 'hide' | 'sticky' = $state('show');
  const sticky = $derived.by(() => dictionaryPreview === 'sticky');

  let readonly = $state(false);
</script>

{#snippet preview(entry: IEntry)}
  <div class="md:pb-4">
    <DictionaryEntry {entry} showLinks class={cn('rounded bg-muted/80 dark:bg-muted/50 p-4')}>
      {#snippet actions()}
        <Toggle bind:pressed={() => sticky, (value) => dictionaryPreview = value ? 'sticky' : 'show'}
          aria-label={`Toggle pinned`} class="aspect-square" size="xs">
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
        <div class="md:pr-2">
          {@render preview(entry)}
        </div>
      {/if}
    </header>
    <ScrollArea class={cn('grow md:pr-2', !$viewSettings.showEmptyFields && 'hide-unused')}>
      {#if dictionaryPreview === 'show'}
        {@render preview(entry)}
      {/if}
      <div class="max-md:p-2 md:pr-2">
        <EntryEditor {entry} {readonly} />
      </div>
    </ScrollArea>
  {/if}
  {#if loadingDebounced.current}
    <div
      class="absolute inset-0 opacity-50 bg-background z-10"
      transition:fade={{ duration: 150 }}>
      <Icon icon="i-mdi-loading" class="absolute inset-0 animate-spin m-auto size-12"></Icon>
    </div>
  {/if}
</div>
