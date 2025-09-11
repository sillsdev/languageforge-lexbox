<script lang="ts">
  import type {IExampleSentence} from '$lib/dotnet-types';
  import {objectTemplateAreas, useCurrentView} from '$lib/views/view-service';
  import * as Editor from '$lib/components/editor';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import {fieldData, type FieldId} from '../field-data';
  import {cn} from '$lib/utils';
  import {vt} from '$lib/views/view-text';
  import {t} from 'svelte-i18n-lingui';
  import {RichMultiWsInput, RichWsInput} from '$lib/components/field-editors';
  import type {EditorSubGridProps} from '$lib/components/editor/editor-sub-grid.svelte';
  import {mergeProps} from 'bits-ui';
  import {initSubjectContext} from '$lib/entry-editor/object-editors/subject-context';

  interface Props extends Omit<EditorSubGridProps, 'onchange'> {
    example: IExampleSentence;
    readonly?: boolean;
    onchange?: (sense: IExampleSentence, field: FieldId) => void;
  }

  const {
    example = $bindable(),
    readonly = false,
    onchange,
    ...rest
  }: Props = $props();

  const writingSystemService = useWritingSystemService();
  const currentView = useCurrentView();
  initSubjectContext(() => example);

  function onFieldChanged(field: FieldId) {
    onchange?.(example, field);
  }
</script>

<Editor.SubGrid {...mergeProps(rest, { class: 'gap-2', style: { gridTemplateAreas: objectTemplateAreas($currentView, example) } })}>
  <Editor.Field.Root fieldId="sentence" class={cn($currentView.fields.sentence.show || 'hidden')}>
    <Editor.Field.Title name={vt($t`Sentence`)} helpId={fieldData.sentence.helpId} />
    <Editor.Field.Body subGrid>
      <RichMultiWsInput
          onchange={() => onFieldChanged('sentence')}
          bind:value={example.sentence}
          {readonly}
          writingSystems={writingSystemService.viewVernacular($currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="translation" class={cn($currentView.fields.translation.show || 'hidden')}>
    {#each example.translations as translation, i (translation.id)}

      <Editor.Field.Title name={vt($t`Translation ${i + 1}`)} helpId={fieldData.translation.helpId}/>
      <Editor.Field.Body subGrid>
        <RichMultiWsInput
          onchange={() => onFieldChanged('translation')}
          bind:value={translation.text}
          {readonly}
          writingSystems={writingSystemService.viewAnalysis($currentView)}/>
      </Editor.Field.Body>
    {/each}
  </Editor.Field.Root>

  {#if writingSystemService.defaultAnalysis}
    <Editor.Field.Root fieldId="reference" class={cn($currentView.fields.reference.show || 'hidden')}>
      <Editor.Field.Title name={vt($t`Reference`)} helpId={fieldData.reference.helpId} />
      <Editor.Field.Body>
        <RichWsInput
            onchange={() => onFieldChanged('reference')}
            bind:value={example.reference}
            {readonly}
            writingSystem={writingSystemService.defaultAnalysis} />
      </Editor.Field.Body>
    </Editor.Field.Root>
  {/if}
</Editor.SubGrid>
