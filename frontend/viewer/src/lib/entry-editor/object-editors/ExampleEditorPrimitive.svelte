<script lang="ts">
  import type {IExampleSentence} from '$lib/dotnet-types';
  import {objectTemplateAreas, useViewService} from '$lib/views/view-service.svelte';
  import {fieldRecord} from '$lib/views/view-data';
  import * as Editor from '$lib/components/editor';
  import {asString, useWritingSystemService} from '$project/data';
  import {entityConfig, type ExampleFieldId} from '../../views/entity-config';
  import {cn, draftTranslation, isDraft} from '$lib/utils';
  import {tvt} from '$lib/views/view-text';
  import {RichMultiWsInput, RichWsInput} from '$lib/components/field-editors';
  import type {EditorSubGridProps} from '$lib/components/editor/editor-sub-grid.svelte';
  import {mergeProps} from 'bits-ui';
  import {initSubjectContext} from '$lib/entry-editor/object-editors/subject-context';
  import {t} from 'svelte-i18n-lingui';

  interface Props extends Omit<EditorSubGridProps, 'onchange'> {
    example: IExampleSentence;
    readonly?: boolean;
    onchange?: (example: IExampleSentence, field: ExampleFieldId) => void;
  }

  const {
    example = $bindable(),
    readonly = false,
    onchange,
    ...rest
  }: Props = $props();

  const writingSystemService = useWritingSystemService();
  const viewService = useViewService();
  initSubjectContext(() => example);

  function onFieldChanged(field: ExampleFieldId) {
    onchange?.(example, field);
  }

  const exampleFields = $derived(viewService.currentView.exampleFields);
  const fields = $derived(fieldRecord(exampleFields));
</script>

<Editor.SubGrid {...mergeProps(rest, { class: 'gap-2', style: { gridTemplateAreas: objectTemplateAreas(exampleFields) } })}>
  <Editor.Field.Root fieldId="sentence" class={cn(fields.sentence?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.example.sentence.label)} helpId={entityConfig.example.sentence.helpId} />
    <Editor.Field.Body subGrid>
      <RichMultiWsInput
          onchange={() => onFieldChanged('sentence')}
          bind:value={example.sentence}
          {readonly}
          writingSystems={writingSystemService.viewVernacular(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="translations" class={cn(fields.translations?.show || 'hidden', 'space-y-2 items-center')}>
    {#each (example.translations.length ? example.translations : [draftTranslation(example)]) as translation, i (translation.id)}
      {@const title = example.translations.length > 1 ? $t`Translation ${i + 1}` : $t`Translation`}
      <Editor.SubGrid class="items-baseline">
        <Editor.Field.Title name={title} helpId={entityConfig.example.translations.helpId}/>
        <Editor.Field.Body subGrid>
          <RichMultiWsInput
            onchange={(_, value) => {
              if (isDraft(translation) && asString(value)) {
                translation.saveDraft();
              }
              onFieldChanged('translations');
            }}
            bind:value={translation.text}
            {readonly}
            writingSystems={writingSystemService.viewAnalysis(viewService.currentView)}/>
        </Editor.Field.Body>
      </Editor.SubGrid>
    {/each}
  </Editor.Field.Root>

  {#if writingSystemService.defaultAnalysis}
    <Editor.Field.Root fieldId="reference" class={cn(fields.reference?.show || 'hidden')}>
      <Editor.Field.Title name={$tvt(entityConfig.example.reference.label)} helpId={entityConfig.example.reference.helpId} />
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
