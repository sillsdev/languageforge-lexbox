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
  import DiffShell from './DiffShell.svelte';

  let {before, after}: {before?: IExampleSentence; after?: IExampleSentence} = $props();

  const writingSystemService = useWritingSystemService();
  const viewService = useViewService();

  const exampleFields = $derived(viewService.currentView.exampleFields);
  const fields = $derived(fieldRecord(exampleFields));

  const translationIds = $derived([
    ...new Set([...(before?.translations ?? []), ...(after?.translations ?? [])].map((tr) => tr.id)),
  ]);

  // In a preview scope every field should be visible. A view configuration that predates a field addition
  // returns undefined from fieldRecord — treat that as "show" so translations/reference never disappear
  // just because the current view doesn't list them. Explicit `show: false` is still respected.
  function shouldShow(field: {show: boolean} | undefined): boolean {
    return field ? field.show : true;
  }
</script>

<Editor.SubGrid class="gap-2" style="grid-template-areas: {objectTemplateAreas(exampleFields)}">
  <Editor.Field.Root fieldId="sentence" class={cn(shouldShow(fields.sentence) || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.example.sentence.label)} helpId={entityConfig.example.sentence.helpId} />
    <Editor.Field.Body subGrid>
      <DiffRichText before={before?.sentence} after={after?.sentence}
                    writingSystems={writingSystemService.viewVernacular(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="translations" class={cn(shouldShow(fields.translations) || 'hidden', 'space-y-2')}>
    {#if translationIds.length === 0}
      <!-- Render the label + empty state even when neither side has translations, so the field's presence is discoverable. -->
      <Editor.Field.Title name={$t`Translation`} helpId={entityConfig.example.translations.helpId} />
      <Editor.Field.Body>
        <DiffRichText before={undefined} after={undefined}
                      writingSystems={writingSystemService.viewAnalysis(viewService.currentView)} />
      </Editor.Field.Body>
    {:else}
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
    {/if}
  </Editor.Field.Root>

  <!-- Reference is a plain RichString? (no writing-system dimension), so it renders regardless of defaultAnalysis. -->
  <Editor.Field.Root fieldId="reference" class={cn(shouldShow(fields.reference) || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.example.reference.label)} helpId={entityConfig.example.reference.helpId} />
    <Editor.Field.Body>
      <!-- Reference is a single text field in the editor, so frame it like one (DiffShell) rather than bare text. -->
      <DiffShell>
        <DiffText before={asString(before?.reference)} after={asString(after?.reference)} />
      </DiffShell>
    </Editor.Field.Body>
  </Editor.Field.Root>
</Editor.SubGrid>
