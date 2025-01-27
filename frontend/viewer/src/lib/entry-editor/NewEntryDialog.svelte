<script lang="ts">
  import type {IEntry} from '$lib/dotnet-types';
  import {fieldName} from '$lib/i18n';
  import {useCurrentView} from '$lib/services/view-service';
  import {getContext} from 'svelte';
  import {Button, Dialog} from 'svelte-ux';
  import type {SaveHandler} from '../services/save-event-service';
  import {useLexboxApi} from '../services/service-provider';
  import {defaultEntry} from '../utils';
  import EntryEditor from './object-editors/EntryEditor.svelte';

  let open = false;
  let loading = false;
  let entry: IEntry = defaultEntry();

  const currentView = useCurrentView();
  const lexboxApi = useLexboxApi();
  const saveHandler = getContext<SaveHandler>('saveHandler');
  let requester: {
    resolve: (entry: IEntry | undefined) => void
  } | undefined;

  async function createEntry(e: Event) {
    e.preventDefault();
    if (!requester) throw new Error('No requester');
    loading = true;
    console.debug('Creating entry', entry);
    await saveHandler(() => lexboxApi.createEntry(entry));
    requester.resolve(entry);
    requester = undefined;
    loading = false;
    open = false;
  }

  export function openWithValue(newEntry: Partial<IEntry>): Promise<IEntry | undefined> {
    return new Promise<IEntry | undefined>((resolve) => {
      if (requester) requester.resolve(undefined);
      requester = { resolve };
      const tmpEntry = defaultEntry();
      open = true;
      entry = {...tmpEntry, ...newEntry, id: tmpEntry.id};
    });
  }

  function onClosing() {
    if (requester) {
      requester.resolve(undefined);
      requester = undefined;
    }
    entry = defaultEntry();
  }
</script>

<Dialog bind:open on:close={onClosing} {loading} persistent={loading}>
  <div slot="title">New {fieldName({id: 'entry'}, $currentView.i18nKey)}</div>
  <div class="m-6">
    <EntryEditor bind:entry={entry} modalMode/>
  </div>
  <div class="flex-grow"></div>
  <div slot="actions">
    <Button>Cancel</Button>
    <Button variant="fill-light" color="success" on:click={e => createEntry(e)}>Create {fieldName({id: 'entry'}, $currentView.i18nKey)}</Button>
  </div>
</Dialog>
