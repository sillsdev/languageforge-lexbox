<script lang="ts">
  import type {IEntry} from '$lib/dotnet-types';
  import {fieldName} from '$lib/i18n';
  import {t} from 'svelte-i18n-lingui';
  import {useCurrentView} from '$lib/views/view-service';
  import {getContext} from 'svelte';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import {useSaveHandler} from '../services/save-event-service.svelte';
  import {useLexboxApi} from '../services/service-provider';
  import {defaultEntry, defaultSense} from '../utils';
  import EntryEditor from './object-editors/EntryEditor.svelte';
  import OverrideFields from '$lib/OverrideFields.svelte';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service.js';

  let open = $state(false);
  let loading = $state(false);
  let entry: IEntry = $state(defaultEntry());

  const currentView = useCurrentView();
  const writingSystemService = useWritingSystemService();
  const dialogsService = useDialogsService();
  dialogsService.invokeNewEntryDialog = openWithValue;
  const lexboxApi = useLexboxApi();
  const saveHandler = useSaveHandler();
  let requester: {
    resolve: (entry: IEntry | undefined) => void
  } | undefined;

  // Watch for changes in the open state to detect when the dialog is closed
  $effect(() => {
    if (!open) {
      onClosing();
    }
  });

  async function createEntry(e: Event) {
    e.preventDefault();
    e.stopPropagation();
    if (!requester) throw new Error('No requester');
    if (!validateEntry()) return;
    loading = true;
    console.debug('Creating entry', entry);
    await saveHandler.handleSave(() => lexboxApi.createEntry(entry));
    requester.resolve(entry);
    requester = undefined;
    loading = false;
    open = false;
  }

  let errors: string[] = $state([]);
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

  let entryLabel = fieldName({id: 'entry'}, $currentView.i18nKey);
</script>

<Dialog.Root bind:open>
  <Dialog.DialogContent>
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{$t`New ${entryLabel}`}</Dialog.DialogTitle>
    </Dialog.DialogHeader>
    <OverrideFields shownFields={['lexemeForm', 'citationForm', 'gloss', 'definition']}>
      <EntryEditor bind:entry={entry} modalMode canAddSense={false} canAddExample={false} />
    </OverrideFields>
    {#if errors.length}
      <div class="self-end my-4">
        {#each errors as error}
          <p class="text-danger p-2">{error}</p>
        {/each}
      </div>
    {/if}
    <Dialog.DialogFooter>
      <Button onclick={() => open = false} variant="secondary">{$t`Cancel`}</Button>
      <Button onclick={e => createEntry(e)} disabled={loading} {loading}>
        {$t`Create ${entryLabel}`}
      </Button>
    </Dialog.DialogFooter>
  </Dialog.DialogContent>
</Dialog.Root>
