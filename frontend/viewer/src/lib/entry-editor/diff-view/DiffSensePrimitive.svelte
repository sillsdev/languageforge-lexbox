<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import type {ISense} from '$lib/dotnet-types';
  import {objectTemplateAreas, useViewService} from '$lib/views/view-service.svelte';
  import {fieldRecord} from '$lib/views/view-data';
  import {entityConfig} from '$lib/views/entity-config';
  import {tvt} from '$lib/views/view-text';
  import {cn} from '$lib/utils';
  import {usePartsOfSpeech, useWritingSystemService} from '$project/data';
  import DiffMultiString from './DiffMultiString.svelte';
  import DiffRichText from './DiffRichText.svelte';
  import DiffSelect from './DiffSelect.svelte';
  import DiffMultiSelect from './DiffMultiSelect.svelte';

  let {before, after}: {before?: ISense; after?: ISense} = $props();

  const writingSystemService = useWritingSystemService();
  const partsOfSpeech = usePartsOfSpeech();
  const viewService = useViewService();

  const senseFields = $derived(viewService.currentView.senseFields);
  const fields = $derived(fieldRecord(senseFields));

  const posLabel = (sense?: ISense) => (sense?.partOfSpeech ? partsOfSpeech.getLabel(sense.partOfSpeech) : undefined);
</script>

<Editor.SubGrid class="gap-2" style="grid-template-areas: {objectTemplateAreas(senseFields)}">
  <Editor.Field.Root fieldId="gloss" class={cn(fields.gloss?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.sense.gloss.label)} helpId={entityConfig.sense.gloss.helpId} />
    <Editor.Field.Body subGrid>
      <DiffMultiString before={before?.gloss} after={after?.gloss}
                       writingSystems={writingSystemService.viewAnalysis(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="definition" class={cn(fields.definition?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.sense.definition.label)} helpId={entityConfig.sense.definition.helpId} />
    <Editor.Field.Body subGrid>
      <DiffRichText before={before?.definition} after={after?.definition}
                    writingSystems={writingSystemService.viewAnalysis(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="partOfSpeechId" class={cn(fields.partOfSpeechId?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.sense.partOfSpeechId.label)} helpId={entityConfig.sense.partOfSpeechId.helpId} />
    <Editor.Field.Body>
      <DiffSelect before={posLabel(before)} after={posLabel(after)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="semanticDomains" class={cn(fields.semanticDomains?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.sense.semanticDomains.label)} helpId={entityConfig.sense.semanticDomains.helpId} />
    <Editor.Field.Body>
      <DiffMultiSelect before={before?.semanticDomains} after={after?.semanticDomains}
                       idSelector={(sd) => sd.id}
                       labelSelector={(sd) => `${sd.code} ${writingSystemService.pickBestAlternative(sd.name, 'analysis')}`} />
    </Editor.Field.Body>
  </Editor.Field.Root>
</Editor.SubGrid>
