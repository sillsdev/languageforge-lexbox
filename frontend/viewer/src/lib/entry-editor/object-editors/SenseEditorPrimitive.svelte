<script lang="ts">
  import type {ISense} from '$lib/dotnet-types';
  import {useSemanticDomains} from '$lib/semantic-domains';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import {usePartsOfSpeech} from '$lib/parts-of-speech.svelte';
  import {useCurrentView, objectTemplateAreas} from '$lib/views/view-service';
  import * as Editor from '$lib/components/editor';
  import {t} from 'svelte-i18n-lingui';
  import {vt} from '$lib/views/view-text';
  import {MultiSelect, MultiWsInput, Select} from '$lib/components/field-editors';
  import {fieldData, type FieldId} from '../field-data';
  import {cn} from '$lib/utils';
  import type {EditorSubGridProps} from '$lib/components/editor/editor-sub-grid.svelte';
  import {mergeProps} from 'bits-ui';

  interface Props extends Omit<EditorSubGridProps, 'onchange'> {
    sense: ISense;
    readonly?: boolean;
    onchange?: (sense: ISense, field: FieldId) => void;
  };

  const {
    sense = $bindable(),
    readonly = false,
    onchange,
    ...rest
  }: Props = $props();

  const writingSystemService = useWritingSystemService();
  const partsOfSpeech = usePartsOfSpeech();
  const semanticDomains = useSemanticDomains();
  const currentView = useCurrentView();

  function onFieldChanged(field: FieldId) {
    onchange?.(sense, field);
  }
</script>

<Editor.SubGrid {...mergeProps(rest, { class: 'gap-2', style: { gridTemplateAreas: objectTemplateAreas($currentView, sense) } })}>
  <Editor.Field.Root style="grid-area: gloss" class={cn($currentView.fields.gloss.show || 'hidden')}>
    <Editor.Field.Title name={$t`Gloss`} helpId={fieldData.gloss.helpId} />
    <Editor.Field.Body subGrid>
      <MultiWsInput
          onchange={() => onFieldChanged('gloss')}
          bind:value={sense.gloss}
          {readonly}
          writingSystems={writingSystemService.analysis} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root style="grid-area: definition" class={cn($currentView.fields.definition.show || 'hidden')}>
    <Editor.Field.Title name={$t`Definition`} helpId={fieldData.definition.helpId} />
    <Editor.Field.Body subGrid>
      <MultiWsInput
          onchange={() => onFieldChanged('definition')}
          bind:value={sense.definition}
          {readonly}
          writingSystems={writingSystemService.analysis} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root style="grid-area: partOfSpeechId" class={cn($currentView.fields.partOfSpeechId.show || 'hidden')}>
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
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root style="grid-area: semanticDomains" class={cn($currentView.fields.semanticDomains.show || 'hidden')}>
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
    </Editor.Field.Body>
  </Editor.Field.Root>
</Editor.SubGrid>
