<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import type {IEntry} from '$lib/dotnet-types';
  import {objectTemplateAreas, useCurrentView} from '$lib/views/view-service';
  import {pt, vt} from '$lib/views/view-text';
  import {t} from 'svelte-i18n-lingui';
  import {fieldData, type FieldId} from '../field-data';
  import {cn} from '$lib/utils';
  import {useComplexFormTypes, usePublications, useWritingSystemService} from '$project/data';
  import {MultiSelect, MultiWsInput, RichMultiWsInput} from '$lib/components/field-editors';
  import ComplexFormComponents from '../field-editors/ComplexFormComponents.svelte';
  import ComplexForms from '../field-editors/ComplexForms.svelte';
  import type {EditorSubGridProps} from '$lib/components/editor/editor-sub-grid.svelte';
  import {mergeProps} from 'bits-ui';
  import {initSubjectContext} from '$lib/entry-editor/object-editors/subject-context';
  import type {Snippet} from 'svelte';

  interface Props extends Omit<EditorSubGridProps, 'onchange'> {
    entry: IEntry;
    readonly?: boolean;
    autofocus?: boolean;
    modalMode?: boolean;
    onchange?: (entry: IEntry, field: FieldId) => void;
    publishInDescription?: Snippet;
  }

  const {
    entry = $bindable(),
    onchange,
    readonly = false,
    autofocus = false,
    modalMode = false,
    publishInDescription,
    ...rest
  }: Props = $props();

  const writingSystemService = useWritingSystemService();
  const complexFormTypes = useComplexFormTypes();
  const publications = usePublications();
  const currentView = useCurrentView();
  initSubjectContext(() => entry);

  function onFieldChanged(field: FieldId) {
    onchange?.(entry, field);
  }
</script>

<Editor.SubGrid {...mergeProps(rest, { class: 'gap-2', style: { gridTemplateAreas: objectTemplateAreas($currentView, entry) } })}>
  <Editor.Field.Root fieldId="lexemeForm" class={cn($currentView.fields.lexemeForm.show || 'hidden')}>
    <Editor.Field.Title name={vt($t`Lexeme form`, $t`Word`)} helpId={fieldData.lexemeForm.helpId} />
    <Editor.Field.Body subGrid>
      <MultiWsInput
          onchange={() => onFieldChanged('lexemeForm')}
          bind:value={entry.lexemeForm}
          {readonly}
          {autofocus}
          writingSystems={writingSystemService.viewVernacular($currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="citationForm" class={cn($currentView.fields.citationForm.show || 'hidden')}>
    <Editor.Field.Title name={vt($t`Citation form`, $t`Display as`)} helpId={fieldData.citationForm.helpId} />
    <Editor.Field.Body subGrid>
      <MultiWsInput
          onchange={() => onFieldChanged('citationForm')}
          bind:value={entry.citationForm}
          {readonly}
          writingSystems={writingSystemService.viewVernacular($currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  {#if !modalMode}
    <Editor.Field.Root fieldId="complexForms" class={cn($currentView.fields.complexForms.show || 'hidden')}>
      <Editor.Field.Title name={vt($t`Complex forms`, $t`Part of`)} helpId={fieldData.complexForms.helpId} />
      <Editor.Field.Body>
        <ComplexForms onchange={() => onFieldChanged('complexForms')}
                      bind:value={entry.complexForms}
                      {readonly}
                      {entry} />
      </Editor.Field.Body>
    </Editor.Field.Root>

    <Editor.Field.Root fieldId="components" class={cn($currentView.fields.components.show || 'hidden')}>
      <Editor.Field.Title name={$t`Components`} helpId={fieldData.components.helpId} />
      <Editor.Field.Body>
        <ComplexFormComponents
          onchange={() => onFieldChanged('components')}
          bind:value={entry.components}
          {readonly}
          {entry} />
      </Editor.Field.Body>
    </Editor.Field.Root>

    <Editor.Field.Root fieldId="complexFormTypes" class={cn($currentView.fields.complexFormTypes.show || 'hidden')}>
      <Editor.Field.Title name={vt($t`Complex form types`, $t`Uses components as`)} helpId={fieldData.complexFormTypes.helpId} />
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

  <Editor.Field.Root fieldId="literalMeaning" class={cn($currentView.fields.literalMeaning.show || 'hidden')}>
    <Editor.Field.Title name={vt($t`Literal meaning`)} helpId={fieldData.literalMeaning.helpId} />
    <Editor.Field.Body subGrid>
      <RichMultiWsInput
          onchange={() => onFieldChanged('literalMeaning')}
          bind:value={entry.literalMeaning}
          {readonly}
          writingSystems={writingSystemService.viewAnalysis($currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="note" class={cn($currentView.fields.note.show || 'hidden')}>
    <Editor.Field.Title name={vt($t`Note`)} helpId={fieldData.note.helpId} />
    <Editor.Field.Body subGrid>
      <RichMultiWsInput
          onchange={() => onFieldChanged('note')}
          bind:value={entry.note}
          {readonly}
          writingSystems={writingSystemService.viewAnalysis($currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="publishIn" class={cn($currentView.fields.publishIn.show || 'hidden')}>
    <Editor.Field.Title name={$t`Publish ${pt($t`Entry`, $t`Word`, $currentView)} in`} helpId={fieldData.publishIn.helpId} />
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
