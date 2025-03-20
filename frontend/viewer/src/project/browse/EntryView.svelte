<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import type { IEntry } from '$lib/dotnet-types';
  import EntryEditor from '$lib/entry-editor/object-editors/EntryEditor.svelte';
  import { useWritingSystemRunes } from '$lib/writing-system-runes.svelte';
  import { useViewSettings } from '$lib/views/view-service';

  const viewSettings = useViewSettings();
  const { entry, onClose, showClose = false }: {
    entry: IEntry | undefined;
    onClose?: () => void;
    showClose?: boolean;
  } = $props();

  const writingSystemService = $derived(useWritingSystemRunes());
</script>

<div class="h-full p-6 overflow-y-auto">
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
  {:else}
    <div class="flex items-center justify-center h-full text-muted-foreground">
      <p>Select an entry to view details</p>
    </div>
  {/if}
</div>
