<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import type {IPartOfSpeech, ISense} from '$lib/dotnet-types';
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
  import DiffPictures from './DiffPictures.svelte';
  import {showChangedFields, sortedIds} from './changed-fields';

  let {before, after}: {before?: ISense; after?: ISense} = $props();

  const writingSystemService = useWritingSystemService();
  const partsOfSpeech = usePartsOfSpeech();
  const viewService = useViewService();

  const senseFields = $derived(showChangedFields(viewService.currentView.senseFields, before, after, {
    semanticDomains: sortedIds,
    pictures: (pictures) => pictures.map(({id, mediaUri, caption}) => ({id, mediaUri, caption})),
  }));
  const fields = $derived(fieldRecord(senseFields));

  // The id is the source of truth: a legacy snapshot can carry an id without the object.
  function partOfSpeech(sense: ISense | undefined): IPartOfSpeech | undefined {
    const id = sense?.partOfSpeechId;
    if (!id) return undefined;
    if (sense.partOfSpeech?.id === id) return sense.partOfSpeech;
    return partsOfSpeech.current.find((pos) => pos.id === id) ?? {id, name: {}, predefined: false};
  }
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
      <DiffSelect before={partOfSpeech(before)} after={partOfSpeech(after)}
                  idSelector={(pos) => pos.id}
                  labelSelector={(pos) => partsOfSpeech.getLabel(pos)} />
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

  <Editor.Field.Root fieldId="pictures" class={cn(fields.pictures?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.sense.pictures.label)} helpId={entityConfig.sense.pictures.helpId} />
    <Editor.Field.Body>
      <DiffPictures before={before?.pictures} after={after?.pictures} />
    </Editor.Field.Body>
  </Editor.Field.Root>
</Editor.SubGrid>
