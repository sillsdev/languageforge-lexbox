<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import type {IEntry, MorphTypeKind} from '$lib/dotnet-types';
  import {objectTemplateAreas, useViewService} from '$lib/views/view-service.svelte';
  import {fieldRecord} from '$lib/views/view-data';
  import {entityConfig} from '$lib/views/entity-config';
  import {tvt} from '$lib/views/view-text';
  import {cn} from '$lib/utils';
  import {usePublications, useWritingSystemService} from '$project/data';
  import {useMorphTypesService} from '$project/data/morph-types.svelte';
  import {t} from 'svelte-i18n-lingui';
  import DiffMultiString from './DiffMultiString.svelte';
  import DiffRichText from './DiffRichText.svelte';
  import DiffMultiSelect from './DiffMultiSelect.svelte';
  import DiffSelect from './DiffSelect.svelte';
  import DiffAnchorRow from './DiffAnchorRow.svelte';
  import DiffNoVisibleChanges from './DiffNoVisibleChanges.svelte';
  import {entryAnchor} from './anchor-text';
  import DiffCollapsed from './DiffCollapsed.svelte';
  import {changedFieldIds, changesOnly, showChangedFields, type DiffMode} from './changed-fields';
  import {entryCompareAs} from './compare-as';

  // `anchor: false` when the caller names the entity itself, e.g. the activity preview's change header.
  // `noVisibleChangesNote: false` when the caller already says what changed, e.g. that the entity moved.
  let {before, after, mode = 'full', anchor = true, noVisibleChangesNote = true}: {
    before?: IEntry; after?: IEntry; mode?: DiffMode; anchor?: boolean; noVisibleChangesNote?: boolean;
  } = $props();

  const writingSystemService = useWritingSystemService();
  const publications = usePublications();
  const morphTypes = useMorphTypesService();
  const viewService = useViewService();

  const compareAs = entryCompareAs;
  const viewFields = $derived(showChangedFields(viewService.currentView.entryFields, before, after, compareAs));
  const changed = $derived(changedFieldIds(viewFields, before, after, compareAs));

  // The editor has no morph type field yet. A change to it shows right under the lexeme form, like in FLEx.
  const morphTypeChanged = $derived(before?.morphType !== after?.morphType);
  // Shown by the anchor, here or in the caller's header
  const homographChanged = $derived((before?.homographNumber ?? 0) !== (after?.homographNumber ?? 0));
  const anyChange = $derived(changed.size > 0 || morphTypeChanged || homographChanged);

  // Without a visible change there's nothing to focus on, so the whole entry shows.
  const focused = $derived(mode === 'changes' && anyChange);
  let expanded = $state(false);
  const focus = $derived(changesOnly(viewFields, changed, after, compareAs));
  const entryFields = $derived(focused && !expanded ? focus.fields : viewFields);
  const fields = $derived(fieldRecord(entryFields));
  const layoutFields = $derived(entryFields.flatMap((field) =>
    field.fieldId === 'lexemeForm' ? [field, {fieldId: 'morphType', show: morphTypeChanged}] : [field]));

  function morphTypeLabel(kind: MorphTypeKind): string {
    const morphType = morphTypes.current.find((m) => m.kind === kind);
    return (morphType && writingSystemService.pickBestAlternative(morphType.name, 'analysis')) || kind;
  }
</script>

{#if anchor}
  <DiffAnchorRow {...entryAnchor(writingSystemService, viewService.currentView, before, after)} />
{/if}
{#if noVisibleChangesNote && mode === 'changes' && before && after && !anyChange}
  <DiffNoVisibleChanges />
{/if}

<!-- Hidden when empty, or its grid gap shows -->
<Editor.SubGrid class={cn('gap-2', layoutFields.some((field) => field.show) || 'hidden')} style="grid-template-areas: {objectTemplateAreas(layoutFields)}">
  <Editor.Field.Root fieldId="lexemeForm" class={cn(fields.lexemeForm?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.lexemeForm.label)} helpId={entityConfig.entry.lexemeForm.helpId} />
    <Editor.Field.Body subGrid>
      <DiffMultiString before={before?.lexemeForm} after={after?.lexemeForm}
                       writingSystems={writingSystemService.viewVernacular(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root style="grid-area: morphType" class={cn(morphTypeChanged || 'hidden')}>
    <Editor.Field.Title name={$t`Morph type`} />
    <Editor.Field.Body>
      <DiffSelect before={before?.morphType} after={after?.morphType} idSelector={(kind) => kind} labelSelector={morphTypeLabel} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="citationForm" class={cn(fields.citationForm?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.citationForm.label)} helpId={entityConfig.entry.citationForm.helpId} />
    <Editor.Field.Body subGrid>
      <DiffMultiString before={before?.citationForm} after={after?.citationForm}
                       writingSystems={writingSystemService.viewVernacular(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="complexFormTypes" class={cn(fields.complexFormTypes?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.complexFormTypes.label)} helpId={entityConfig.entry.complexFormTypes.helpId} />
    <Editor.Field.Body>
      <DiffMultiSelect before={before?.complexFormTypes} after={after?.complexFormTypes}
                       idSelector={(cft) => cft.id}
                       labelSelector={(cft) => writingSystemService.pickBestAlternative(cft.name, 'analysis')}
                       sortValuesBy="selectionOrder" />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="literalMeaning" class={cn(fields.literalMeaning?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.literalMeaning.label)} helpId={entityConfig.entry.literalMeaning.helpId} />
    <Editor.Field.Body subGrid>
      <DiffRichText before={before?.literalMeaning} after={after?.literalMeaning}
                    writingSystems={writingSystemService.viewAnalysis(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="note" class={cn(fields.note?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.note.label)} helpId={entityConfig.entry.note.helpId} />
    <Editor.Field.Body subGrid>
      <DiffRichText before={before?.note} after={after?.note}
                    writingSystems={writingSystemService.viewAnalysis(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="publishIn" class={cn(fields.publishIn?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.publishIn.label)} helpId={entityConfig.entry.publishIn.helpId} />
    <Editor.Field.Body>
      <DiffMultiSelect before={before?.publishIn} after={after?.publishIn}
                       idSelector={(pub) => pub.id}
                       labelSelector={(pub) => publications.getLabel(pub)} />
    </Editor.Field.Body>
  </Editor.Field.Root>
</Editor.SubGrid>
{#if focused}
  <DiffCollapsed count={focus.unchanged} bind:expanded />
{/if}
