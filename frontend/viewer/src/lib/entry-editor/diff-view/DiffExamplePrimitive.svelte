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
  import DiffAnchorRow from './DiffAnchorRow.svelte';
  import DiffNoVisibleChanges from './DiffNoVisibleChanges.svelte';
  import {exampleAnchor} from './anchor-text';
  import DiffCollapsed from './DiffCollapsed.svelte';
  import {changedFieldIds, changesOnly, showChangedFields, type DiffMode} from './changed-fields';

  // `anchor: false` when the caller names the entity itself, e.g. the activity preview's change header.
  // `noVisibleChangesNote: false` when the caller already says what changed, e.g. that the entity moved.
  let {before, after, mode = 'full', anchor = true, noVisibleChangesNote = true}: {
    before?: IExampleSentence; after?: IExampleSentence; mode?: DiffMode; anchor?: boolean; noVisibleChangesNote?: boolean;
  } = $props();

  const writingSystemService = useWritingSystemService();
  const viewService = useViewService();

  const compareAs = {
    translations: (translations: IExampleSentence['translations']) => Object.fromEntries(translations.map((tr) => [tr.id, tr.text])),
  };
  const viewFields = $derived(showChangedFields(viewService.currentView.exampleFields, before, after, compareAs));
  const changed = $derived(changedFieldIds(viewFields, before, after, compareAs));

  // Without a visible change there's nothing to focus on, so the whole example shows.
  const focused = $derived(mode === 'changes' && changed.size > 0);
  let expanded = $state(false);
  const focus = $derived(changesOnly(viewFields, changed, after, compareAs));
  const exampleFields = $derived(focused && !expanded ? focus.fields : viewFields);
  const fields = $derived(fieldRecord(exampleFields));

  // Paired by id, current order first. Like the editor's draft translation, an example without any still gets one empty row.
  const translations = $derived.by(() => {
    const ids = [...new Set([...(after?.translations ?? []), ...(before?.translations ?? [])].map((tr) => tr.id))];
    if (!ids.length) return [{id: '', before: undefined, after: undefined}];
    return ids.map((id) => ({
      id,
      before: (before?.translations ?? []).find((tr) => tr.id === id)?.text,
      after: (after?.translations ?? []).find((tr) => tr.id === id)?.text,
    }));
  });
</script>

{#if anchor}
  <DiffAnchorRow {...exampleAnchor(writingSystemService, viewService.currentView, before, after)} />
{/if}
{#if noVisibleChangesNote && mode === 'changes' && before && after && !changed.size}
  <DiffNoVisibleChanges />
{/if}

<!-- Hidden when empty, or its grid gap shows -->
<Editor.SubGrid class={cn('gap-2', exampleFields.some((field) => field.show) || 'hidden')} style="grid-template-areas: {objectTemplateAreas(exampleFields)}">
  <Editor.Field.Root fieldId="sentence" class={cn(fields.sentence?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.example.sentence.label)} helpId={entityConfig.example.sentence.helpId} />
    <Editor.Field.Body subGrid>
      <DiffRichText before={before?.sentence} after={after?.sentence}
                    writingSystems={writingSystemService.viewVernacular(viewService.currentView)} />
    </Editor.Field.Body>
  </Editor.Field.Root>

  <Editor.Field.Root fieldId="translations" class={cn(fields.translations?.show || 'hidden', 'space-y-2')}>
    {#each translations as translation, i (translation.id)}
      <Editor.SubGrid class="items-baseline">
        <Editor.Field.Title name={translations.length > 1 ? $t`Translation ${i + 1}` : $t`Translation`}
                            helpId={entityConfig.example.translations.helpId} />
        <Editor.Field.Body subGrid>
          <DiffRichText before={translation.before} after={translation.after}
                        writingSystems={writingSystemService.viewAnalysis(viewService.currentView)} />
        </Editor.Field.Body>
      </Editor.SubGrid>
    {/each}
  </Editor.Field.Root>

  <!-- Unlike the editor, not hidden without a default analysis WS: that guard exists for its input, and a diff needs none. -->
  <Editor.Field.Root fieldId="reference" class={cn(fields.reference?.show || 'hidden')}>
    <Editor.Field.Title name={$tvt(entityConfig.example.reference.label)} helpId={entityConfig.example.reference.helpId} />
    <Editor.Field.Body>
      <DiffShell>
        <DiffText before={asString(before?.reference)} after={asString(after?.reference)} />
      </DiffShell>
    </Editor.Field.Body>
  </Editor.Field.Root>
</Editor.SubGrid>
{#if focused}
  <DiffCollapsed count={focus.unchanged} bind:expanded />
{/if}
