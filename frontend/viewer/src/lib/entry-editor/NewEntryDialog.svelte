<script lang="ts">
  import type {IEntry} from '$lib/dotnet-types';
  import {fieldName} from '$lib/i18n';
  import {useCurrentView} from '$lib/views/view-service';
  import {getContext} from 'svelte';
  import {Button} from 'svelte-ux';
  import * as Dialog from '$lib/components/ui/dialog';
  import type {SaveHandler} from '../services/save-event-service';
  import {useLexboxApi} from '../services/service-provider';
  import {defaultEntry, defaultSense} from '../utils';
  import EntryEditor from './object-editors/EntryEditor.svelte';
  import OverrideFields from '$lib/OverrideFields.svelte';
  import {useWritingSystemService} from '$lib/writing-system-service';
  import {initFeatures} from '$lib/services/feature-service';

  let open = false;
  let loading = false;
  let entry: IEntry = defaultEntry();
  let previousOpenState = false;

  const currentView = useCurrentView();
  const writingSystemService = useWritingSystemService();
  const lexboxApi = useLexboxApi();
  const saveHandler = getContext<SaveHandler>('saveHandler');
  let requester: {
    resolve: (entry: IEntry | undefined) => void
  } | undefined;

  // Watch for changes in the open state to detect when the dialog is closed
  $: if (previousOpenState && !open) {
    onClosing();
  }
  $: previousOpenState = open;

  async function createEntry(e: Event) {
    e.preventDefault();
    e.stopPropagation();
    if (!requester) throw new Error('No requester');
    if (!validateEntry()) return;
    loading = true;
    console.debug('Creating entry', entry);
    await saveHandler(() => lexboxApi.createEntry(entry));
    requester.resolve(entry);
    requester = undefined;
    loading = false;
    open = false;
  }

  let errors: string[] = [];
  function validateEntry(): boolean {
    errors = [];
    if (!writingSystemService.headword(entry)) errors.push('Lexeme form is required');
    if (entry.senses.length > 0 && !writingSystemService.firstDefOrGlossVal(entry.senses[0])) errors.push('Definition or gloss is required');
    return errors.length === 0;
  }

  export function openWithValue(newEntry: Partial<IEntry>): Promise<IEntry | undefined> {
    return new Promise<IEntry | undefined>((resolve) => {
      if (requester) requester.resolve(undefined);
      requester = { resolve };
      const tmpEntry = defaultEntry();
      open = true;
      entry = {...tmpEntry, ...newEntry, id: tmpEntry.id};
      if (entry.senses.length === 0) {
        entry.senses.push(defaultSense(entry.id));
      }
      errors = [];
    });
  }

  function onClosing() {
    if (requester) {
      requester.resolve(undefined);
      requester = undefined;
    }
    entry = defaultEntry();
  }

  initFeatures({ write: true }); // hide history buttons
</script>

<Dialog.Root bind:open>
  <Dialog.DialogContent class="sm:max-w-[425px]">
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>New {fieldName({id: 'entry'}, $currentView.i18nKey)}</Dialog.DialogTitle>
    </Dialog.DialogHeader>
    <div class="m-6">
      <OverrideFields shownFields={['lexemeForm', 'citationForm', 'gloss', 'definition']}>
        <EntryEditor bind:entry={entry} modalMode canAddSense={false} canAddExample={false} />
      </OverrideFields>
    </div>
    <div class="flex-grow"></div>
    <div class="self-end m-4">
      {#each errors as error}
        <p class="text-danger p-2">{error}</p>
      {/each}
    </div>
    <Dialog.DialogFooter>
      <Button on:click={() => open = false}>Cancel</Button>
      <Button variant="fill-light" color="success" on:click={e => createEntry(e)} disabled={loading}>
        {#if loading}
          <span class="loading loading-spinner loading-xs mr-2"></span>
        {/if}
        Create {fieldName({id: 'entry'}, $currentView.i18nKey)}
      </Button>
    </Dialog.DialogFooter>
  </Dialog.DialogContent>
</Dialog.Root>
