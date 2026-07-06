<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import type {ISense} from '$lib/dotnet-types';
  import {hasVisibleFields, useViewService} from '$lib/views/view-service.svelte';
  import DiffSensePrimitive from '$lib/entry-editor/diff-view/DiffSensePrimitive.svelte';
  import DiffExamplePrimitive from '$lib/entry-editor/diff-view/DiffExamplePrimitive.svelte';
  import ObjectHeader from '$lib/entry-editor/object-editors/ObjectHeader.svelte';

  // A sense (with its examples) created in one commit, self-diffed so the fields render plainly (the card
  // header states the creation) — the sense-level counterpart of CollapsedEntryDiff. The list row already
  // names the parent entry ("headword · Added sense …"), so this shows just the sense subtree, starting
  // directly with the sense fields like a lone create-sense preview does (no root header; examples keep theirs).
  let {sense}: {sense: ISense} = $props();

  const viewService = useViewService();
  const showExamples = $derived(hasVisibleFields(viewService.currentView.exampleFields));
</script>

<Editor.Root>
  <Editor.Grid>
    <DiffSensePrimitive before={sense} after={sense} />

    {#if showExamples && sense.exampleSentences.length}
      <Editor.SubGrid class="border-l border-dashed pl-4 mt-4 space-y-4 rounded-lg">
        {#each sense.exampleSentences as example, j (example.id)}
          <Editor.SubGrid>
            <ObjectHeader type="example" index={j + 1} />
            <DiffExamplePrimitive before={example} after={example} />
          </Editor.SubGrid>
        {/each}
      </Editor.SubGrid>
    {/if}
  </Editor.Grid>
</Editor.Root>
