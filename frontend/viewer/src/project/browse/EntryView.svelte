<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import EntryEditor from '$lib/entry-editor/object-editors/EntryEditor.svelte';
  import { useViewSettings } from '$lib/views/view-service';
  import { resource, Debounced } from 'runed';
  import { useMiniLcmApi } from '$lib/services/service-provider';
  import { fade } from 'svelte/transition';
  import ViewPicker from './ViewPicker.svelte';
  import EntryMenu from './EntryMenu.svelte';
  import {ScrollArea} from '$lib/components/ui/scroll-area';
  import {cn} from '$lib/utils';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import {t} from 'svelte-i18n-lingui';
  import {XButton} from '$lib/components/ui/button';

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
      await new Promise((resolve) => setTimeout(resolve, 500));
      return miniLcmApi.getEntry(id);
    },
  );
  const entry = $derived(entryResource.current ?? undefined);
  const loadingDebounced = new Debounced(() => entryResource.loading, 50);
  const headword = $derived((entry && writingSystemService.headword(entry)) || $t`Untitled`);
</script>

<div class="h-full flex flex-col relative">
  {#if entry}
    <header class="mb-4 flex justify-between">
      <div>
        {#if showClose && onClose}
          <XButton onclick={onClose} size="icon" />
        {/if}
        <h2 class="ml-4 text-2xl font-semibold mb-2 inline">{headword}</h2>
      </div>
      <div class="flex">
        <ViewPicker/>
        <EntryMenu {entry} />
      </div>
    </header>
    <ScrollArea class={cn('grow md:pr-4', !$viewSettings.showEmptyFields && 'hide-unused')}>
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
