<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import type {IExampleSentence} from '$lib/dotnet-types';
  import {objectTemplateAreas, useViewService} from '$lib/views/view-service.svelte';
  import {fieldRecord} from '$lib/views/view-data';
  import {entityConfig} from '$lib/views/entity-config';
  import {tvt} from '$lib/views/view-text';
  import {cn} from '$lib/utils';
  import {asString, useWritingSystemService} from '$project/data';
  import {t} from 'svelte-i18n-lingui';
  import DiffRichText from './DiffRichText.svelte';
  import DiffText from './DiffText.svelte';

  let {before, after}: {before?: IExampleSentence; after?: IExampleSentence} = $props();

  const writingSystemService = useWritingSystemService();
  const viewService = useViewService();

  const exampleFields = $derived(viewService.currentView.exampleFields);
  const fields = $derived(fieldRecord(exampleFields));

  const translationIds = $derived([
    ...new Set([...(before?.translations ?? []), ...(after?.translations ?? [])].map((tr) => tr.id)),
  ]);
</script>

<Editor.SubGrid class="gap-2" style="grid-template-areas: {objectTemplateAreas(exampleFields)}">
  <Editor.Field.Root fieldId="sentence" class={cn(fields.sentence?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.example.sentence.label)} helpId={entityConfig.example.sentence.helpId} />
    <Editor.Field.Body subGrid>
      <DiffRichText before={before?.sentence} after={after?.sentence}
                    writingSystems={writingSystemService.viewVernacular(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="translations" class={cn(fields.translations?.show || 'hidden', 'space-y-2')}>
    {#each translationIds as id, i (id)}
      {@const beforeTr = before?.translations.find((tr) => tr.id === id)}
      {@const afterTr = after?.translations.find((tr) => tr.id === id)}
      {@const title = translationIds.length > 1 ? $t`Translation ${i + 1}` : $t`Translation`}
      <Editor.SubGrid class="items-baseline">
        <Editor.Field.Title name={title} helpId={entityConfig.example.translations.helpId} />
        <Editor.Field.Body subGrid>
          <DiffRichText before={beforeTr?.text} after={afterTr?.text}
                        writingSystems={writingSystemService.viewAnalysis(viewService.currentView)} />
        </Editor.Field.Body>
      </Editor.SubGrid>
    {/each}
  </Editor.Field.Root>

  {#if writingSystemService.defaultAnalysis}
    <Editor.Field.Root fieldId="reference" class={cn(fields.reference?.show || 'hidden')}>
      <Editor.Field.Title name={$tvt(entityConfig.example.reference.label)} helpId={entityConfig.example.reference.helpId} />
      <Editor.Field.Body>
        <DiffText before={asString(before?.reference)} after={asString(after?.reference)} />
      </Editor.Field.Body>
    </Editor.Field.Root>
  {/if}
</Editor.SubGrid>
