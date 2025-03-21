<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import type { IEntry } from '$lib/dotnet-types';
  import EntryEditor from '$lib/entry-editor/object-editors/EntryEditor.svelte';
  import { useWritingSystemRunes } from '$lib/writing-system-runes.svelte';
  import { useViewSettings } from '$lib/views/view-service';
  import { resource, Debounced } from 'runed';
  import { useMiniLcmApi } from '$lib/services/service-provider';
  import { slide, fade, blur } from 'svelte/transition';

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

  const writingSystemService = $derived(useWritingSystemRunes());
</script>

<div class="h-full p-6 overflow-y-auto relative">
  {#if entry}
    <div class="mb-4">
      {#if showClose && onClose}
        <Icon icon="i-mdi-close" onclick={onClose} class="cursor-pointer"></Icon>
      {/if}
      <h2 class="ml-4 text-2xl font-semibold mb-2 inline">{writingSystemService.headword(entry) || 'Untitled'}</h2>
    </div>
    <div class:hide-unused={!$viewSettings.showEmptyFields}>
      <EntryEditor modalMode {entry} />
    </div>
  {/if}
  {#if loadingDebounced.current}
    <div
      class="absolute inset-0 opacity-50 bg-background z-10"
      transition:fade={{ duration: 150 }}>
      <Icon icon="i-mdi-loading" class="absolute inset-0 animate-spin m-auto size-12"></Icon>
    </div>
  {/if}
</div>
