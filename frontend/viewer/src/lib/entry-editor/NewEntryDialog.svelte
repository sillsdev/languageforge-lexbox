<script lang="ts" module>
  export type EntryTemplate = Partial<Pick<IEntry, 'publishIn'>>;
  // should not include partOfSpeechId, because the editor doesn't read that it only sets it
  export type SenseTemplate = Partial<Pick<ISense, 'partOfSpeech' | 'semanticDomains'>>;
</script>

<script lang="ts">
  import {MorphTypeKind, ViewBase, type IEntry, type IMorphType, type ISense} from '$lib/dotnet-types';
  import {createEntryOptions} from '$lib/create-entry-options';
  import {untrack} from 'svelte';
  import {t} from 'svelte-i18n-lingui';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import {useSaveHandler} from '../services/save-event-service.svelte';
  import {useMiniLcmApi} from '$lib/services/service-provider';
  import {defaultEntry, defaultSense} from '../utils';
  import OverrideFields from '$lib/views/OverrideFields.svelte';
  import {useWritingSystemService, usePublications} from '$project/data';
  import {useDialogsService} from '$lib/services/dialogs-service.js';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {isSubmitEnter} from '$lib/utils/submit-on-enter';
  import {pt} from '$lib/views/view-text';
  import * as Editor from '$lib/components/editor';
  import Icon from '$lib/components/ui/icon/icon.svelte';
  import DuplicateCheckSection from './duplicate-check/DuplicateCheckSection.svelte';
  import EntryEditorPrimitive from './object-editors/EntryEditorPrimitive.svelte';
  import ObjectHeader from './object-editors/ObjectHeader.svelte';
  import SenseEditorPrimitive from './object-editors/SenseEditorPrimitive.svelte';
  import AddSenseButton from './object-editors/AddSenseButton.svelte';
  import {Select} from '$lib/components/field-editors';
  import {useMorphTypesService} from '$project/data/morph-types.svelte';
  import {findMorphType, stripMarkers} from './morph-type-markers';
  import type {ReadonlyDeep} from 'type-fest';

  let open = $state(false);
  useBackHandler({addToStack: () => open, onBack: () => open = false, key: 'new-entry-dialog'});
  let loading = $state(false);

  let entry = $state(defaultEntry());
  let sense = $state<ISense | undefined>(untrack(() => defaultSense(entry.id)));

  const viewService = useViewService();
  const writingSystemService = useWritingSystemService();
  const publicationService = usePublications();
  const dialogsService = useDialogsService();
  const morphTypes = useMorphTypesService();
  dialogsService.invokeNewEntryDialog = openWithValue;
  const api = useMiniLcmApi();
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

  // Classic only: like FLEx, affix markers typed in the lexeme form pick the morph type and are stripped on create
  const pickMorphType = $derived(viewService.currentView.base === ViewBase.FieldWorks);
  const morphTypeOptions = $derived(morphTypes.current
    .filter(mt => mt.kind !== MorphTypeKind.Unknown)
    .toSorted((a, b) => morphTypeLabel(a).localeCompare(morphTypeLabel(b))));
  const selectedMorphType = $derived(morphTypes.current.find(mt => mt.kind === entry.morphType));
  const detectedMorphType = $derived(findMorphType(writingSystemService.first(entry.lexemeForm, writingSystemService.vernacularNoAudio) ?? '', morphTypes.current));
  // Only a change in what the markers say overrides the picker, so picking Root and then typing on keeps Root
  let lastDetectedKind: MorphTypeKind | undefined;
  let morphTypeAnnouncement = $state('');
  $effect(() => {
    if (!pickMorphType || !detectedMorphType.valid) return;
    const kind = detectedMorphType.kind;
    if (kind === lastDetectedKind) return;
    lastDetectedKind = kind;
    if (entry.morphType === kind) return;
    entry.morphType = kind;
    morphTypeAnnouncement = untrack(() => selectedMorphType && morphTypeLabel(selectedMorphType)) ?? '';
  });

  function morphTypeLabel(morphType: ReadonlyDeep<IMorphType>): string {
    return writingSystemService.pickBestAlternative(morphType.name, 'analysis') || morphType.kind;
  }

  function onMorphTypePicked(morphType: ReadonlyDeep<IMorphType> | undefined) {
    if (!morphType) return;
    entry.morphType = morphType.kind;
    if (morphType.kind === MorphTypeKind.Circumfix) return; // FLEx leaves the form alone: circumfix markers vary
    for (const ws of writingSystemService.vernacularNoAudio) {
      const form = entry.lexemeForm[ws.wsId];
      if (!form) continue;
      const bare = stripMarkers(form, findMorphType(form, morphTypes.current));
      entry.lexemeForm[ws.wsId] = `${morphType.prefix ?? ''}${bare}${morphType.postfix ?? ''}`;
    }
    lastDetectedKind = detectedMorphType.valid ? detectedMorphType.kind : undefined;
  }

  // The entry as it will be stored: markers stripped from the lexeme form
  function toStoredEntry(entry: IEntry): IEntry {
    if (!pickMorphType) return {...entry, morphType: MorphTypeKind.Stem};
    const lexemeForm = {...entry.lexemeForm};
    for (const ws of writingSystemService.vernacularNoAudio) {
      const form = lexemeForm[ws.wsId];
      if (form) lexemeForm[ws.wsId] = stripMarkers(form, findMorphType(form, morphTypes.current));
    }
    return {...entry, lexemeForm};
  }
  const storedEntry = $derived(toStoredEntry(entry));

  async function createEntry(e: Event) {
    e.preventDefault();
    e.stopPropagation();
    // we might already be creating something
    if (loading) return;
    if (!requester) throw new Error('No requester');

    loading = true;
    try {
      await editor?.commit();
      await addMainPublicationPromise; // make sure the main publication landed before we snapshot the entry
      entry.senses = sense ? [sense] : [];
      if (!validateEntry()) return;

      const entrySnapshot = toStoredEntry($state.snapshot(entry));
      // The dialog pre-populates publishIn (main publication + any active filter), so always create the entry as-is.
      await saveHandler.handleSave(() => api.createEntry(entrySnapshot, createEntryOptions.asIs));
      requester.resolve(entrySnapshot);
      requester = undefined;
      open = false;
    } finally {
      loading = false;
    }
  }

  let errors: string[] = $state([]);
  function validateEntry(): boolean {
    errors = [];
    if (pickMorphType) {
      for (const ws of writingSystemService.vernacularNoAudio) {
        const form = entry.lexemeForm[ws.wsId];
        if (form && !findMorphType(form, morphTypes.current).valid) {
          errors.push($t`FieldWorks doesn't recognize the affix markers in "${form}". Remove them or choose a morph type.`);
        }
      }
    }
    // Allow entries with only an audio lexeme-form or citation-form to be created
    if (!(writingSystemService.first(storedEntry.lexemeForm) ?? writingSystemService.first(entry.citationForm))) {
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
      lastDetectedKind = undefined;
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
    if (isSubmitEnter(event)) void createEntry(event);
  }
</script>

{#if open}
{#snippet fromActiveFilter()}
  <span class="text-sm text-primary/85 mt-0.5 inline-flex items-center gap-1">
    {$t`From active filter`}
    <Icon icon="i-mdi-filter-outline" class="size-4" />
  </span>
{/snippet}

{#snippet morphTypeField()}
  <Select
    value={selectedMorphType}
    onchange={onMorphTypePicked}
    options={morphTypeOptions}
    labelSelector={morphTypeLabel}
    idSelector="id" />
  <span class="sr-only" aria-live="polite">{morphTypeAnnouncement}</span>
{/snippet}

{#snippet publishInNote()}
  {#if publishInIsFromTemplate}{@render fromActiveFilter()}{/if}
{/snippet}

<Dialog.Root bind:open={open}>
  <!-- Fixed width (not min/max): the duplicate check adds and reshapes content while the
    dialog is open, and a content-sized dialog jumps around with every keystroke -->
  <Dialog.DialogContent onkeydown={handleKeydown}
    class="sm:min-h-[min(calc(100%-16px),30rem)] sm:w-[min(calc(100%-32px),50rem)] max-md:px-2">
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{pt($t`New Entry`, $t`New Word`, viewService.currentView)}</Dialog.DialogTitle>
    </Dialog.DialogHeader>
    <!-- min-w-0: as a grid item this div defaults to min-width:auto, letting long duplicate
      headword lists widen the dialog instead of truncating -->
    <div class="min-w-0">
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
              publishInDescription={entryTemplate?.publishIn?.length ? publishInNote : undefined}
              morphTypeField={pickMorphType ? morphTypeField : undefined} />
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
      <DuplicateCheckSection entry={storedEntry} {sense}
        onNavigateToEntry={() => open = false} />
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
      <Button onclick={e => createEntry(e)} {loading}>
        {pt($t`Create Entry`, $t`Add Word`, viewService.currentView)}
      </Button>
    </Dialog.DialogFooter>
  </Dialog.DialogContent>
</Dialog.Root>
{/if}
