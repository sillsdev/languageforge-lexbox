<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import type { IEntry } from '$lib/dotnet-types';
  import EntryEditor from '$lib/entry-editor/object-editors/EntryEditor.svelte';
  import { useViewSettings } from '$lib/views/view-service';
  import { resource, Debounced } from 'runed';
  import { useMiniLcmApi } from '$lib/services/service-provider';
  import { fade } from 'svelte/transition';
  import ViewPicker from './ViewPicker.svelte';
  import EntryMenu from './EntryMenu.svelte';
  import Button from '$lib/components/ui/button/button.svelte';
  import {ScrollArea} from '$lib/components/ui/scroll-area';
  import {cn} from '$lib/utils';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import {t} from 'svelte-i18n-lingui';

  const viewSettings = useViewSettings();
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
      await new Promise((resolve) => setTimeout(resolve, 500));
      return miniLcmApi.getEntry(id);
    },
  );
  const entry = $derived(entryResource.current ?? undefined);
  const loadingDebounced = new Debounced(() => entryResource.loading, 50);

  const writingSystemService = useWritingSystemService();

  function handleDelete() {
    // TODO: Implement delete functionality
    console.log('Delete entry:', entryId);
  }
</script>

<div class="h-full flex flex-col md:px-6 pt-2 relative">
  {#if entry}
    <header class="mb-4 flex">
      {#if showClose && onClose}
        <Button icon="i-mdi-close" onclick={onClose} variant="ghost" size="icon"></Button>
      {/if}
      <h2 class="ml-4 text-2xl font-semibold mb-2 inline">{writingSystemService.headword(entry) || $t`Untitled`}</h2>
      <div class="flex-1"></div>
      <ViewPicker/>
      <EntryMenu onDelete={handleDelete} />
    </header>
    <ScrollArea class={cn('grow md:pr-5', !$viewSettings.showEmptyFields && 'hide-unused')}>
      <EntryEditor {entry} disablePortalButtons />
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
