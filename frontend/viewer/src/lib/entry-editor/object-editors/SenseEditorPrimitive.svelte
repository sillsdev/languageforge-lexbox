<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import type {EditorSubGridProps} from '$lib/components/editor/editor-sub-grid.svelte';
  import {MultiSelect, MultiWsInput, RichMultiWsInput, Select} from '$lib/components/field-editors';
  import type {ISense} from '$lib/dotnet-types';
  import {initSubjectContext} from '$lib/entry-editor/object-editors/subject-context';
  import {cn} from '$lib/utils';
  import {objectTemplateAreas, useViewService} from '$lib/views/view-service.svelte';
  import {fieldRecord} from '$lib/views/view-data';
  import {usePartsOfSpeech, useSemanticDomains, useWritingSystemService} from '$project/data';
  import {mergeProps} from 'bits-ui';
  import type {Snippet} from 'svelte';
  import {entityConfig, type SenseFieldId} from '../../views/entity-config';
  import {tvt} from '$lib/views/view-text';

  interface Props extends Omit<EditorSubGridProps, 'onchange'> {
    sense: ISense;
    readonly?: boolean;
    onchange?: (sense: ISense, field: SenseFieldId) => void;
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
  const viewService = useViewService();
  initSubjectContext(() => sense);
  function onFieldChanged(field: SenseFieldId) {
    onchange?.(sense, field);
  }

  const senseFields = $derived(viewService.currentView.senseFields);
  const fields = $derived(fieldRecord(senseFields));
</script>

<Editor.SubGrid {...mergeProps(rest, { class: 'gap-2', style: { gridTemplateAreas: objectTemplateAreas(senseFields) } })}>
  <Editor.Field.Root fieldId="gloss" class={cn(fields.gloss?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.sense.gloss.label)} helpId={entityConfig.sense.gloss.helpId} />
    <Editor.Field.Body subGrid>
      <MultiWsInput
          onchange={() => onFieldChanged('gloss')}
          bind:value={sense.gloss}
          {readonly}
          writingSystems={writingSystemService.viewAnalysis(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="definition" class={cn(fields.definition?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.sense.definition.label)} helpId={entityConfig.sense.definition.helpId} />
    <Editor.Field.Body subGrid>
      <RichMultiWsInput
          onchange={() => onFieldChanged('definition')}
          bind:value={sense.definition}
          {readonly}
          writingSystems={writingSystemService.viewAnalysis(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="partOfSpeechId" class={cn(fields.partOfSpeechId?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.sense.partOfSpeechId.label)} helpId={entityConfig.sense.partOfSpeechId.helpId}/>
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

  <Editor.Field.Root fieldId="semanticDomains" class={cn(fields.semanticDomains?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.sense.semanticDomains.label)} helpId={entityConfig.sense.semanticDomains.helpId} />
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
