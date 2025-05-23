<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import type {IEntry} from '$lib/dotnet-types';
  import {objectTemplateAreas, useCurrentView} from '$lib/views/view-service';
  import {vt} from '$lib/views/view-text';
  import {t} from 'svelte-i18n-lingui';
  import {fieldData, type FieldId} from '../field-data';
  import {cn} from '$lib/utils';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import {MultiSelect, MultiWsInput, RichMultiWsInput} from '$lib/components/field-editors';
  import {useComplexFormTypes} from '$lib/complex-form-types';
  import ComplexFormComponents from '../field-editors/ComplexFormComponents.svelte';
  import ComplexForms from '../field-editors/ComplexForms.svelte';
  import type {EditorSubGridProps} from '$lib/components/editor/editor-sub-grid.svelte';
  import {mergeProps} from 'bits-ui';

  interface Props extends Omit<EditorSubGridProps, 'onchange'> {
    entry: IEntry;
    readonly?: boolean;
    modalMode?: boolean;
    onchange?: (entry: IEntry, field: FieldId) => void;
  }

  const {
    entry = $bindable(),
    onchange,
    readonly = false,
    modalMode = false,
    ...rest
  }: Props = $props();

  const writingSystemService = useWritingSystemService();
  const complexFormTypes = useComplexFormTypes();
  const currentView = useCurrentView();

  function onFieldChanged(field: FieldId) {
    onchange?.(entry, field);
  }
</script>

<Editor.SubGrid {...mergeProps(rest, { class: 'gap-2', style: { gridTemplateAreas: objectTemplateAreas($currentView, entry) } })}>
  <Editor.Field.Root style="grid-area: lexemeForm" class={cn($currentView.fields.lexemeForm.show || 'hidden')}>
    <Editor.Field.Title name={vt($t`Lexeme form`, $t`Word`)} helpId={fieldData.lexemeForm.helpId} />
    <Editor.Field.Body subGrid>
      <MultiWsInput
          onchange={() => onFieldChanged('lexemeForm')}
          bind:value={entry.lexemeForm}
          {readonly}
          autofocus={modalMode}
          writingSystems={writingSystemService.vernacular} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root style="grid-area: citationForm" class={cn($currentView.fields.citationForm.show || 'hidden')}>
    <Editor.Field.Title name={vt($t`Citation form`, $t`Display as`)} helpId={fieldData.citationForm.helpId} />
    <Editor.Field.Body subGrid>
      <MultiWsInput
          onchange={() => onFieldChanged('citationForm')}
          bind:value={entry.citationForm}
          {readonly}
          writingSystems={writingSystemService.vernacular} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  {#if !modalMode}
    <Editor.Field.Root style="grid-area: complexForms" class={cn($currentView.fields.complexForms.show || 'hidden')}>
      <Editor.Field.Title name={vt($t`Complex forms`, $t`Part of`)} helpId={fieldData.complexForms.helpId} />
      <Editor.Field.Body>
        <ComplexForms onchange={() => onFieldChanged('complexForms')}
                      bind:value={entry.complexForms}
                      {readonly}
                      {entry} />
      </Editor.Field.Body>
    </Editor.Field.Root>

    <Editor.Field.Root style="grid-area: complexFormTypes" class={cn($currentView.fields.complexFormTypes.show || 'hidden')}>
      <Editor.Field.Title name={vt($t`Complex form types`)} helpId={fieldData.complexFormTypes.helpId} />
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

    <Editor.Field.Root style="grid-area: components" class={cn($currentView.fields.components.show || 'hidden')}>
      <Editor.Field.Title name={vt($t`Components`, $t`Made of`)} helpId={fieldData.components.helpId} />
      <Editor.Field.Body>
        <ComplexFormComponents
          onchange={() => onFieldChanged('components')}
          bind:value={entry.components}
          {readonly}
          {entry} />
      </Editor.Field.Body>
    </Editor.Field.Root>
  {/if}

  <Editor.Field.Root style="grid-area: literalMeaning" class={cn($currentView.fields.literalMeaning.show || 'hidden')}>
    <Editor.Field.Title name={vt($t`Literal meaning`)} helpId={fieldData.literalMeaning.helpId} />
    <Editor.Field.Body subGrid>
      <RichMultiWsInput
          onchange={() => onFieldChanged('literalMeaning')}
          bind:value={entry.literalMeaning}
          {readonly}
          writingSystems={writingSystemService.analysis} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root style="grid-area: note" class={cn($currentView.fields.note.show || 'hidden')}>
    <Editor.Field.Title name={vt($t`Note`)} helpId={fieldData.note.helpId} />
    <Editor.Field.Body subGrid>
      <RichMultiWsInput
          onchange={() => onFieldChanged('note')}
          bind:value={entry.note}
          {readonly}
          writingSystems={writingSystemService.analysis} />
    </Editor.Field.Body>
  </Editor.Field.Root>
</Editor.SubGrid>
