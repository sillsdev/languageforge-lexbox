<script lang="ts" module>
  // should not include partOfSpeechId, because the editor doesn't read that it only sets it
  export type SenseTemplate = Partial<Pick<ISense, 'partOfSpeech' | 'semanticDomains'>>;
</script>

<script lang="ts">
  import type {IEntry, ISense} from '$lib/dotnet-types';
  import {t} from 'svelte-i18n-lingui';
  import {useCurrentView} from '$lib/views/view-service';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import {useSaveHandler} from '../services/save-event-service.svelte';
  import {useLexboxApi} from '../services/service-provider';
  import {defaultEntry, defaultSense} from '../utils';
  import OverrideFields from '$lib/views/OverrideFields.svelte';
  import {useWritingSystemService} from '$project/data';
  import {useDialogsService} from '$lib/services/dialogs-service.js';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {pt} from '$lib/views/view-text';
  import * as Editor from '$lib/components/editor';
  import Icon from '$lib/components/ui/icon/icon.svelte';
  import EntryEditorPrimitive from './object-editors/EntryEditorPrimitive.svelte';
  import ObjectHeader from './object-editors/ObjectHeader.svelte';
  import SenseEditorPrimitive from './object-editors/SenseEditorPrimitive.svelte';
  import AddSenseButton from './object-editors/AddSenseButton.svelte';

  let open = $state(false);
  useBackHandler({addToStack: () => open, onBack: () => open = false, key: 'new-entry-dialog'});
  let loading = $state(false);

  let entry = $state(defaultEntry());
  // svelte-ignore state_referenced_locally
  let sense = $state<ISense | undefined>(defaultSense(entry.id));

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

    entry.senses = sense ? [sense] : [];
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
    // Allow entries with only an audio lexeme-form or citation-form to be created
    if (!(writingSystemService.first(entry.lexemeForm) ?? writingSystemService.first(entry.citationForm))) {
      errors.push(`${pt('Lexeme or citation form', 'Word', $currentView)} is required`);
    }
    if (entry.senses.length > 0 && !writingSystemService.firstDefOrGlossVal(entry.senses[0])) errors.push('Definition or gloss is required');
    return errors.length === 0;
  }

  let senseTemplate = $state<Partial<Omit<ISense, 'partOfSpeechId'>>>();
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

      senseTemplate = newSense;

      const tmpEntry = defaultEntry();
      entry = {...tmpEntry, ...newEntry, senses: [], id: tmpEntry.id};
      addSense();

      errors = [];
      open = true;
    });
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
  }

  function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !IsMobile.value) {
      void createEntry(event);
    }
  }
</script>

{#if open}
<Dialog.Root bind:open={open}>
  <Dialog.DialogContent onkeydown={handleKeydown} class="sm:min-h-[min(calc(100%-16px),30rem)] max-md:px-2">
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{pt($t`New Entry`, $t`New Word`, $currentView)}</Dialog.DialogTitle>
    </Dialog.DialogHeader>
    <div>
      <OverrideFields shownFields={[
        'lexemeForm', 'citationForm',
        'gloss', 'definition', 'partOfSpeechId',
        /* only show semantic domains if the "template" set it */
        ...(senseTemplate?.semanticDomains?.length ? ['semanticDomains'] as const : [])
        ]}>
        <Editor.Root>
          <Editor.Grid>
            <EntryEditorPrimitive bind:entry autofocus modalMode />
            {#if sense}
              <Editor.SubGrid>
                <ObjectHeader type="sense">
                  <Button onclick={() => sense = undefined} size="icon" variant="secondary" icon="i-mdi-trash-can" />
                </ObjectHeader>
                <SenseEditorPrimitive bind:sense>
                  {#snippet partOfSpeechDescription()}
                    {#if partOfSpeechIsFromTemplate}
                      <span class="text-sm text-primary/85 mt-0.5 inline-flex items-center gap-1">
                        {$t`From active filter`}
                        <Icon icon="i-mdi-filter-outline" class="size-4" />
                      </span>
                    {/if}
                  {/snippet}
                  {#snippet semanticDomainsDescription()}
                    {#if semanticDomainIsFromTemplate}
                      <span class="text-sm text-primary/85 mt-0.5 inline-flex items-center gap-1">
                        {$t`From active filter`}
                        <Icon icon="i-mdi-filter-outline" class="size-4" />
                      </span>
                    {/if}
                  {/snippet}
                </SenseEditorPrimitive>
              </Editor.SubGrid>
            {:else}
              <div class="col-span-full flex justify-end">
                <AddSenseButton onclick={addSense} />
              </div>
            {/if}
          </Editor.Grid>
        </Editor.Root>
      </OverrideFields>
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
      <Button onclick={e => createEntry(e)} disabled={loading} {loading}>
        {pt($t`Create Entry`, $t`Add Word`, $currentView)}
      </Button>
    </Dialog.DialogFooter>
  </Dialog.DialogContent>
</Dialog.Root>
{/if}
