<script lang="ts">
  import type {IEntry} from '$lib/dotnet-types';
  import {fieldName} from '$lib/i18n';
  import {useCurrentView} from '$lib/views/view-service';
  import {getContext} from 'svelte';
  import {Button, Dialog} from 'svelte-ux';
  import type {SaveHandler} from '../services/save-event-service';
  import {useLexboxApi} from '../services/service-provider';
  import {defaultEntry, defaultSense} from '../utils';
  import EntryEditor from './object-editors/EntryEditor.svelte';
  import OverrideFields from '$lib/OverrideFields.svelte';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import {initFeatures} from '$lib/services/feature-service';

  let open = false;
  let loading = false;
  let entry: IEntry = defaultEntry();

  const currentView = useCurrentView();
  const writingSystemService = useWritingSystemService();
  const lexboxApi = useLexboxApi();
  const saveHandler = getContext<SaveHandler>('saveHandler');
  let requester: {
    resolve: (entry: IEntry | undefined) => void
  } | undefined;

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

<Dialog bind:open on:close={onClosing} {loading} persistent={loading}>
  <div slot="title">New {fieldName({id: 'entry'}, $currentView.i18nKey)}</div>
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
  <div slot="actions">
    <Button>Cancel</Button>
    <Button variant="fill-light" color="success" on:click={e => createEntry(e)}>Create {fieldName({id: 'entry'}, $currentView.i18nKey)}</Button>
  </div>
</Dialog>
