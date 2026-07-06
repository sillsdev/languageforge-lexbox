<script lang="ts" module>
  export type EntryTemplate = Partial<Pick<IEntry, 'publishIn'>>;
  // should not include partOfSpeechId, because the editor doesn't read that it only sets it
  export type SenseTemplate = Partial<Pick<ISense, 'partOfSpeech' | 'semanticDomains'>>;
</script>

<script lang="ts">
  import type {IEntry, ISense} from '$lib/dotnet-types';
  import {createEntryOptions} from '$lib/create-entry-options';
  import {untrack} from 'svelte';
  import {t} from 'svelte-i18n-lingui';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import {useSaveHandler} from '../services/save-event-service.svelte';
  import {useLexboxApi} from '../services/service-provider';
  import {defaultEntry, defaultSense} from '../utils';
  import OverrideFields from '$lib/views/OverrideFields.svelte';
  import {useWritingSystemService, usePublications} from '$project/data';
  import {useDialogsService} from '$lib/services/dialogs-service.js';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {pt} from '$lib/views/view-text';
  import * as Editor from '$lib/components/editor';
  import Icon from '$lib/components/ui/icon/icon.svelte';
  import DuplicateCheck from './DuplicateCheck.svelte';
  import EntryEditorPrimitive from './object-editors/EntryEditorPrimitive.svelte';
  import ObjectHeader from './object-editors/ObjectHeader.svelte';
  import SenseEditorPrimitive from './object-editors/SenseEditorPrimitive.svelte';
  import AddSenseButton from './object-editors/AddSenseButton.svelte';

  let open = $state(false);
  useBackHandler({addToStack: () => open, onBack: () => open = false, key: 'new-entry-dialog'});
  let loading = $state(false);
  let duplicateActionBusy = $state(false);

  let entry = $state(defaultEntry());
  let sense = $state<ISense | undefined>(untrack(() => defaultSense(entry.id)));

  const viewService = useViewService();
  const writingSystemService = useWritingSystemService();
  const publicationService = usePublications();
  const dialogsService = useDialogsService();
  dialogsService.invokeNewEntryDialog = openWithValue;
  const lexboxApi = useLexboxApi();
  const saveHandler = useSaveHandler();
  let requester: {
    resolve: (entry: IEntry | undefined) => void
  } | undefined;
  let addMainPublicationPromise: Promise<void> | undefined;

  // Watch for changes in the open state to detect when the dialog is closed
  $effect(() => {
    if (!open) {
      onClosing();
    }
  });

  let editor = $state<Editor.Root>();

  async function createEntry(e: Event) {
    e.preventDefault();
    e.stopPropagation();
    // loading: double-Enter must not create twice, so it flips before the first await
    // duplicateActionBusy: a pending add-sense already consumes the typed meaning
    if (loading || duplicateActionBusy) return;
    if (!requester) throw new Error('No requester');

    loading = true;
    try {
      await editor?.commit();
      await addMainPublicationPromise; // make sure the main publication landed before we snapshot the entry
      entry.senses = sense ? [sense] : [];
      if (!validateEntry()) return;

      const entrySnapshot = $state.snapshot(entry);
      // The dialog pre-populates publishIn (main publication + any active filter), so always create the entry as-is.
      await saveHandler.handleSave(() => lexboxApi.createEntry(entrySnapshot, createEntryOptions.asIs));
      requester.resolve(entry);
      requester = undefined;
      open = false;
    } finally {
      loading = false;
    }
  }

  let errors: string[] = $state([]);
  function validateEntry(): boolean {
    errors = [];
    // Allow entries with only an audio lexeme-form or citation-form to be created
    if (!(writingSystemService.first(entry.lexemeForm) ?? writingSystemService.first(entry.citationForm))) {
      errors.push(pt($t`Lexeme form or Citation form is required`, $t`Word or Display as is required`, viewService.currentView));
    }
    return errors.length === 0;
  }

  let entryTemplate = $state<Partial<IEntry>>();
  let senseTemplate = $state<Partial<Omit<ISense, 'partOfSpeechId'>>>();
  let publishInIsFromTemplate = $state<boolean>();
  $effect(() => {
    if (publishInIsFromTemplate !== false) { // never overwrite once set to false
      publishInIsFromTemplate = Boolean(entryTemplate?.publishIn?.length &&
        entryTemplate.publishIn.some(p => entry?.publishIn?.some(_p => _p.id === p.id)));
    }
  });
  let partOfSpeechIsFromTemplate = $state<boolean>();
  $effect(() => {
    if (partOfSpeechIsFromTemplate !== false) { // never overwrite once set to false
      partOfSpeechIsFromTemplate = Boolean(senseTemplate?.partOfSpeech?.id && senseTemplate.partOfSpeech.id === sense?.partOfSpeech?.id);
    }
  });
  let semanticDomainIsFromTemplate = $state<boolean>();
  $effect(() => {
    if (semanticDomainIsFromTemplate !== false) { // never overwrite once set to false
      semanticDomainIsFromTemplate = Boolean(senseTemplate?.semanticDomains?.length &&
        senseTemplate.semanticDomains.some(sd => sense?.semanticDomains?.some(_sd => _sd.id === sd.id)));
    }
  });

  function openWithValue(newEntry: Partial<IEntry>, newSense?: Partial<Omit<ISense, 'partOfSpeechId'>>): Promise<IEntry | undefined> {
    return new Promise<IEntry | undefined>((resolve) => {
      if (requester) requester.resolve(undefined);
      requester = { resolve };

      entryTemplate = newEntry;
      senseTemplate = newSense;

      const tmpEntry = defaultEntry();
      publishInIsFromTemplate = undefined;
      entry = {...tmpEntry, ...newEntry, senses: [], id: tmpEntry.id};
      addMainPublicationPromise = addMainPublication(entry.id);
      addSense();

      errors = [];
      open = true;
    });
  }

  // Add the project's main publication to a new entry; the user can remove it when the publish-in field is shown.
  // Gate on `loaded`, not on reading `mainPublication`: reading it lazily kicks off a second getPublications fetch,
  // and that superseded fetch resolves with stale empty data — so the main publication would silently never be added.
  async function addMainPublication(entryId: string) {
    if (!publicationService.loaded) await publicationService.refetch();
    const main = publicationService.mainPublication;
    if (!main || entry.id !== entryId) return; // dialog moved on while we awaited
    if (!entry.publishIn.some(p => p.id === main.id)) {
      entry.publishIn = [...entry.publishIn, main];
    }
  }

  function addSense() {
      partOfSpeechIsFromTemplate = undefined;
      semanticDomainIsFromTemplate = undefined;
      const tmpSense = defaultSense(entry.id);
      const semanticDomains = [...senseTemplate?.semanticDomains ?? []];
      sense = {...tmpSense, ...senseTemplate, semanticDomains, id: tmpSense.id, entryId: entry.id};
      entry.senses = [sense];
  }

  function onClosing() {
    if (requester) {
      requester.resolve(undefined);
      requester = undefined;
    }
    entry = defaultEntry();
    addMainPublicationPromise = undefined;
  }

  function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !IsMobile.value) {
      void createEntry(event);
    }
  }
</script>

{#if open}
{#snippet fromActiveFilter()}
  <span class="text-sm text-primary/85 mt-0.5 inline-flex items-center gap-1">
    {$t`From active filter`}
    <Icon icon="i-mdi-filter-outline" class="size-4" />
  </span>
{/snippet}

{#snippet publishInNote()}
  {#if publishInIsFromTemplate}{@render fromActiveFilter()}{/if}
{/snippet}

<Dialog.Root bind:open={open}>
  <Dialog.DialogContent onkeydown={handleKeydown} class="sm:min-h-[min(calc(100%-16px),30rem)] max-md:px-2">
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{pt($t`New Entry`, $t`New Word`, viewService.currentView)}</Dialog.DialogTitle>
    </Dialog.DialogHeader>
    <div>
      <OverrideFields shownFields={[
        'lexemeForm', 'citationForm',
        'gloss', 'definition', 'partOfSpeechId',
        /* only show semantic domains and publications if the "template" set it */
        ...(entryTemplate?.publishIn?.length ? ['publishIn'] as const : []),
        ...(senseTemplate?.semanticDomains?.length ? ['semanticDomains'] as const : [])
        ]}>
        <Editor.Root bind:this={editor}>
          <Editor.Grid>
            <EntryEditorPrimitive bind:entry autofocus modalMode
              publishInDescription={entryTemplate?.publishIn?.length ? publishInNote : undefined} />
            {#if sense}
              <Editor.SubGrid>
                <ObjectHeader type="sense">
                  <Button onclick={() => sense = undefined} size="icon" variant="secondary" icon="i-mdi-trash-can" />
                </ObjectHeader>
                <SenseEditorPrimitive bind:sense
                  partOfSpeechDescription={partOfSpeechIsFromTemplate ? fromActiveFilter : undefined}
                  semanticDomainsDescription={semanticDomainIsFromTemplate ? fromActiveFilter : undefined} />
              </Editor.SubGrid>
            {:else}
              <div class="col-span-full flex justify-end">
                <AddSenseButton onclick={addSense} />
              </div>
            {/if}
          </Editor.Grid>
        </Editor.Root>
      </OverrideFields>
      <div class="mt-3">
        <DuplicateCheck {entry} {sense} bind:busy={duplicateActionBusy} onNavigateToEntry={() => open = false} />
      </div>
    </div>
    {#if errors.length}
      <div class="text-end space-y-2">
        {#each errors as error (error)}
          <p class="text-destructive">{error}</p>
        {/each}
      </div>
    {/if}
    <Dialog.DialogFooter>
      <Button onclick={() => open = false} variant="secondary">{$t`Cancel`}</Button>
      <Button onclick={e => createEntry(e)} disabled={loading || duplicateActionBusy} {loading}>
        {pt($t`Create Entry`, $t`Add Word`, viewService.currentView)}
      </Button>
    </Dialog.DialogFooter>
  </Dialog.DialogContent>
</Dialog.Root>
{/if}
