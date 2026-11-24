<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import {MorphTypeKind, type IEntry} from '$lib/dotnet-types';
  import {objectTemplateAreas, useViewService} from '$lib/views/view-service.svelte';
  import {fieldRecord} from '$lib/views/view-data';
  import {entityConfig, type EntryFieldId} from '../../views/entity-config';
  import {cn} from '$lib/utils';
  import {useComplexFormTypes, usePublications, useWritingSystemService} from '$project/data';
  import {MultiSelect, MultiWsInput, RichMultiWsInput, Select, type WsInputNote} from '$lib/components/field-editors';
  import ComplexFormComponents from '../field-editors/ComplexFormComponents.svelte';
  import ComplexForms from '../field-editors/ComplexForms.svelte';
  import type {EditorSubGridProps} from '$lib/components/editor/editor-sub-grid.svelte';
  import {mergeProps} from 'bits-ui';
  import {initSubjectContext} from '$lib/entry-editor/object-editors/subject-context';
  import type {Snippet} from 'svelte';
  import {tvt} from '$lib/views/view-text';
  import {morphTypeLabel, useMorphTypesService} from '$project/data/morph-types.svelte';
  import {morphTypeGroup, morphTypeGroupIndex} from '$project/data/morph-type-groups';
  import {t} from 'svelte-i18n-lingui';

  interface Props extends Omit<EditorSubGridProps, 'onchange'> {
    entry: IEntry;
    readonly?: boolean;
    autofocus?: boolean;
    modalMode?: boolean;
    onchange?: (entry: IEntry, field: EntryFieldId) => void;
    publishInDescription?: Snippet;
    /** Changing the morph type later can leave the lexeme form's markers inconsistent, so only a new entry may set it */
    newEntry?: boolean;
    lexemeFormNotes?: Partial<Record<string, WsInputNote>>;
  }

  const {
    entry = $bindable(),
    onchange,
    readonly = false,
    autofocus = false,
    modalMode = false,
    publishInDescription,
    newEntry = false,
    lexemeFormNotes,
    ...rest
  }: Props = $props();

  const writingSystemService = useWritingSystemService();
  const complexFormTypes = useComplexFormTypes();
  const publications = usePublications();
  const viewService = useViewService();
  const morphTypes = useMorphTypesService();
  initSubjectContext(() => entry);

  function onFieldChanged(field: EntryFieldId) {
    onchange?.(entry, field);
  }

  const entryFields = $derived(viewService.currentView.entryFields);
  const fields = $derived(fieldRecord(entryFields));
  const morphTypeOptions = $derived(morphTypes.current
    .filter(mt => mt.kind !== MorphTypeKind.Unknown)
    .map(mt => ({mt, group: morphTypeGroupIndex(mt.kind), label: morphTypeLabel(mt, writingSystemService)}))
    .toSorted((a, b) => a.group - b.group || a.label.localeCompare(b.label))
    .map(({mt}) => mt));
</script>

<Editor.SubGrid {...mergeProps(rest, { class: 'gap-2', style: { gridTemplateAreas: objectTemplateAreas(entryFields) } })}>
  <Editor.Field.Root fieldId="lexemeForm" class={cn(fields.lexemeForm?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.lexemeForm.label)} helpId={entityConfig.entry.lexemeForm.helpId} />
    <Editor.Field.Body subGrid>
      <MultiWsInput
          onchange={() => onFieldChanged('lexemeForm')}
          bind:value={entry.lexemeForm}
          {readonly}
          {autofocus}
          notes={lexemeFormNotes}
          writingSystems={writingSystemService.viewVernacular(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="morphType" class={cn(fields.morphType?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.morphType.label)} helpId={entityConfig.entry.morphType.helpId} />
    <Editor.Field.Body>
      <Select
        value={morphTypes.byKind(entry.morphType)}
        options={morphTypeOptions}
        labelSelector={(mt) => morphTypeLabel(mt, writingSystemService)}
        idSelector="id"
        groupSelector={(mt) => $t(morphTypeGroup(mt.kind).label)}
        readonly={readonly || !newEntry}
        onchange={(mt) => {
          if (!mt) return;
          entry.morphType = mt.kind;
          onFieldChanged('morphType');
        }} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="citationForm" class={cn(fields.citationForm?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.citationForm.label)} helpId={entityConfig.entry.citationForm.helpId} />
    <Editor.Field.Body subGrid>
      <MultiWsInput
          onchange={() => onFieldChanged('citationForm')}
          bind:value={entry.citationForm}
          {readonly}
          writingSystems={writingSystemService.viewVernacular(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  {#if !modalMode}
    <Editor.Field.Root fieldId="complexForms" class={cn(fields.complexForms?.show || 'hidden')}>
      <Editor.Field.Title name={$tvt(entityConfig.entry.complexForms.label)} helpId={entityConfig.entry.complexForms.helpId} />
      <Editor.Field.Body>
        <ComplexForms onchange={() => onFieldChanged('complexForms')}
                      bind:value={entry.complexForms}
                      {readonly}
                      {entry} />
      </Editor.Field.Body>
    </Editor.Field.Root>

    <Editor.Field.Root fieldId="components" class={cn(fields.components?.show || 'hidden')}>
      <Editor.Field.Title name={$tvt(entityConfig.entry.components.label)} helpId={entityConfig.entry.components.helpId} />
      <Editor.Field.Body>
        <ComplexFormComponents
          onchange={() => onFieldChanged('components')}
          bind:value={entry.components}
          {readonly}
          {entry} />
      </Editor.Field.Body>
    </Editor.Field.Root>

    <Editor.Field.Root fieldId="complexFormTypes" class={cn(fields.complexFormTypes?.show || 'hidden')}>
      <Editor.Field.Title name={$tvt(entityConfig.entry.complexFormTypes.label)} helpId={entityConfig.entry.complexFormTypes.helpId} />
      <Editor.Field.Body>
        <MultiSelect
          onchange={() => onFieldChanged('complexFormTypes')}
          bind:values={entry.complexFormTypes}
          sortValuesBy="selectionOrder"
          options={complexFormTypes.current}
          labelSelector={(cft) => writingSystemService.pickBestAlternative(cft.name, 'analysis')}
          {readonly}
          idSelector="id" />
      </Editor.Field.Body>
    </Editor.Field.Root>
  {/if}

  <Editor.Field.Root fieldId="literalMeaning" class={cn(fields.literalMeaning?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.literalMeaning.label)} helpId={entityConfig.entry.literalMeaning.helpId} />
    <Editor.Field.Body subGrid>
      <RichMultiWsInput
          onchange={() => onFieldChanged('literalMeaning')}
          bind:value={entry.literalMeaning}
          {readonly}
          writingSystems={writingSystemService.viewAnalysis(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="note" class={cn(fields.note?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.note.label)} helpId={entityConfig.entry.note.helpId} />
    <Editor.Field.Body subGrid>
      <RichMultiWsInput
          onchange={() => onFieldChanged('note')}
          bind:value={entry.note}
          {readonly}
          writingSystems={writingSystemService.viewAnalysis(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="publishIn" class={cn(fields.publishIn?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.publishIn.label)} helpId={entityConfig.entry.publishIn.helpId} />
    <Editor.Field.Body>
      <MultiSelect
          onchange={() => onFieldChanged('publishIn')}
          bind:values={entry.publishIn}
          options={publications.current}
          labelSelector={(pub) => publications.getLabel(pub)}
          idSelector="id"
          sortValuesBy="optionOrder"
          {readonly} />
      {@render publishInDescription?.()}
    </Editor.Field.Body>
  </Editor.Field.Root>
</Editor.SubGrid>
