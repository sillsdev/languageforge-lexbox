<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import type {EditorSubGridProps} from '$lib/components/editor/editor-sub-grid.svelte';
  import {MultiSelect, MultiWsInput, RichMultiWsInput, Select} from '$lib/components/field-editors';
  import type {ISense} from '$lib/dotnet-types';
  import {initSubjectContext} from '$lib/entry-editor/object-editors/subject-context';
  import {cn} from '$lib/utils';
  import {objectTemplateAreas, useCurrentView} from '$lib/views/view-service';
  import {vt} from '$lib/views/view-text';
  import {usePartsOfSpeech, useSemanticDomains, useWritingSystemService} from '$project/data';
  import {mergeProps} from 'bits-ui';
  import type {Snippet} from 'svelte';
  import {t} from 'svelte-i18n-lingui';
  import {fieldData, type FieldId} from '../field-data';

  interface Props extends Omit<EditorSubGridProps, 'onchange'> {
    sense: ISense;
    readonly?: boolean;
    onchange?: (sense: ISense, field: FieldId) => void;
    partOfSpeechDescription?: Snippet;
    semanticDomainsDescription?: Snippet;
  };

  const {
    sense = $bindable(),
    readonly = false,
    onchange,
    partOfSpeechDescription,
    semanticDomainsDescription,
    ...rest
  }: Props = $props();

  const writingSystemService = useWritingSystemService();
  const partsOfSpeech = usePartsOfSpeech();
  const semanticDomains = useSemanticDomains();
  const currentView = useCurrentView();
  initSubjectContext(() => sense);
  function onFieldChanged(field: FieldId) {
    onchange?.(sense, field);
  }
</script>

<Editor.SubGrid {...mergeProps(rest, { class: 'gap-2', style: { gridTemplateAreas: objectTemplateAreas($currentView, sense) } })}>
  <Editor.Field.Root fieldId="gloss" class={cn($currentView.fields.gloss.show || 'hidden')}>
    <Editor.Field.Title name={$t`Gloss`} helpId={fieldData.gloss.helpId} />
    <Editor.Field.Body subGrid>
      <MultiWsInput
          onchange={() => onFieldChanged('gloss')}
          bind:value={sense.gloss}
          {readonly}
          writingSystems={writingSystemService.viewAnalysis($currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="definition" class={cn($currentView.fields.definition.show || 'hidden')}>
    <Editor.Field.Title name={$t`Definition`} helpId={fieldData.definition.helpId} />
    <Editor.Field.Body subGrid>
      <RichMultiWsInput
          onchange={() => onFieldChanged('definition')}
          bind:value={sense.definition}
          {readonly}
          writingSystems={writingSystemService.viewAnalysis($currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="partOfSpeechId" class={cn($currentView.fields.partOfSpeechId.show || 'hidden')}>
    <Editor.Field.Title name={vt($t`Grammatical info.`, $t`Part of speech`)} helpId={fieldData.partOfSpeechId.helpId}/>
    <Editor.Field.Body>
      <Select
          onchange={() => {
            sense.partOfSpeechId = sense.partOfSpeech?.id;
            onFieldChanged('partOfSpeechId');
          }}
          bind:value={sense.partOfSpeech}
          options={partsOfSpeech.current}
          labelSelector={(pos) => partsOfSpeech.getLabel(pos)}
          idSelector="id"
          {readonly} />
      {@render partOfSpeechDescription?.()}
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="semanticDomains" class={cn($currentView.fields.semanticDomains.show || 'hidden')}>
    <Editor.Field.Title name={$t`Semantic domains`} helpId={fieldData.semanticDomains.helpId} />
    <Editor.Field.Body>
      <MultiSelect
          onchange={() => onFieldChanged('semanticDomains')}
          bind:values={sense.semanticDomains}
          options={semanticDomains.current}
          labelSelector={(sd) => `${sd.code} ${writingSystemService.pickBestAlternative(sd.name, 'analysis')}`}
          idSelector="id"
          sortValuesBy="optionOrder"
          {readonly} />
      {@render semanticDomainsDescription?.()}
    </Editor.Field.Body>
  </Editor.Field.Root>
</Editor.SubGrid>
