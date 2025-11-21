<script lang="ts">
  import type {IExampleSentence} from '$lib/dotnet-types';
  import {objectTemplateAreas, useCurrentView} from '$lib/views/view-service';
  import * as Editor from '$lib/components/editor';
  import {asString, useWritingSystemService} from '$project/data';
  import {fieldData, type FieldId} from '../field-data';
  import {cn, draftTranslation, isDraft} from '$lib/utils';
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

  <Editor.Field.Root fieldId="translations" class={cn($currentView.fields.translations.show || 'hidden', 'space-y-2 items-center')}>
    {#each (example.translations.length ? example.translations : [draftTranslation(example)]) as translation, i (translation.id)}
      {@const title = example.translations.length > 1 ? vt($t`Translation ${i + 1}`) : vt($t`Translation`)}
      <Editor.SubGrid class="items-baseline">
        <Editor.Field.Title name={title} helpId={fieldData.translations.helpId}/>
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
            writingSystems={writingSystemService.viewAnalysis($currentView)}/>
        </Editor.Field.Body>
      </Editor.SubGrid>
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
