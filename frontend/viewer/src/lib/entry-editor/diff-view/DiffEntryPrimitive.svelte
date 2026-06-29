<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import type {IEntry} from '$lib/dotnet-types';
  import {objectTemplateAreas, useViewService} from '$lib/views/view-service.svelte';
  import {fieldRecord} from '$lib/views/view-data';
  import {entityConfig} from '$lib/views/entity-config';
  import {tvt} from '$lib/views/view-text';
  import {cn} from '$lib/utils';
  import {usePublications, useWritingSystemService} from '$project/data';
  import DiffMultiString from './DiffMultiString.svelte';
  import DiffRichText from './DiffRichText.svelte';
  import DiffMultiSelect from './DiffMultiSelect.svelte';

  let {before, after}: {before?: IEntry; after?: IEntry} = $props();

  const writingSystemService = useWritingSystemService();
  const publications = usePublications();
  const viewService = useViewService();

  const entryFields = $derived(viewService.currentView.entryFields);
  const fields = $derived(fieldRecord(entryFields));
</script>

<Editor.SubGrid class="gap-2" style="grid-template-areas: {objectTemplateAreas(entryFields)}">
  <Editor.Field.Root fieldId="lexemeForm" class={cn(fields.lexemeForm?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.lexemeForm.label)} helpId={entityConfig.entry.lexemeForm.helpId} />
    <Editor.Field.Body subGrid>
      <DiffMultiString before={before?.lexemeForm} after={after?.lexemeForm}
                       writingSystems={writingSystemService.viewVernacular(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="citationForm" class={cn(fields.citationForm?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.citationForm.label)} helpId={entityConfig.entry.citationForm.helpId} />
    <Editor.Field.Body subGrid>
      <DiffMultiString before={before?.citationForm} after={after?.citationForm}
                       writingSystems={writingSystemService.viewVernacular(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="complexFormTypes" class={cn(fields.complexFormTypes?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.complexFormTypes.label)} helpId={entityConfig.entry.complexFormTypes.helpId} />
    <Editor.Field.Body>
      <DiffMultiSelect before={before?.complexFormTypes} after={after?.complexFormTypes}
                       idSelector={(cft) => cft.id}
                       labelSelector={(cft) => writingSystemService.pickBestAlternative(cft.name, 'analysis')} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="literalMeaning" class={cn(fields.literalMeaning?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.literalMeaning.label)} helpId={entityConfig.entry.literalMeaning.helpId} />
    <Editor.Field.Body subGrid>
      <DiffRichText before={before?.literalMeaning} after={after?.literalMeaning}
                    writingSystems={writingSystemService.viewAnalysis(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="note" class={cn(fields.note?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.note.label)} helpId={entityConfig.entry.note.helpId} />
    <Editor.Field.Body subGrid>
      <DiffRichText before={before?.note} after={after?.note}
                    writingSystems={writingSystemService.viewAnalysis(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="publishIn" class={cn(fields.publishIn?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.entry.publishIn.label)} helpId={entityConfig.entry.publishIn.helpId} />
    <Editor.Field.Body>
      <DiffMultiSelect before={before?.publishIn} after={after?.publishIn}
                       idSelector={(pub) => pub.id}
                       labelSelector={(pub) => publications.getLabel(pub)} />
    </Editor.Field.Body>
  </Editor.Field.Root>
</Editor.SubGrid>
