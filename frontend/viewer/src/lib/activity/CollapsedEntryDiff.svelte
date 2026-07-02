<script lang="ts">
  import * as Editor from '$lib/components/editor';
  import type {IEntry} from '$lib/dotnet-types';
  import {hasVisibleFields, useViewService} from '$lib/views/view-service.svelte';
  import DiffEntryPrimitive from '$lib/entry-editor/diff-view/DiffEntryPrimitive.svelte';
  import DiffSensePrimitive from '$lib/entry-editor/diff-view/DiffSensePrimitive.svelte';
  import DiffExamplePrimitive from '$lib/entry-editor/diff-view/DiffExamplePrimitive.svelte';
  import ObjectHeader from '$lib/entry-editor/object-editors/ObjectHeader.svelte';

  // A whole created entry rendered through the diff components (not the real editor), so it shares the
  // visual language of every other activity preview. `before` is left undefined on each primitive, so a
  // create reads as all-added — matching how a lone create-entry change already renders. Layout mirrors
  // EntryEditor's sense/example nesting.
  let {entry}: {entry: IEntry} = $props();

  const viewService = useViewService();
  const showExamples = $derived(hasVisibleFields(viewService.currentView.exampleFields));
  const showSenses = $derived(showExamples || hasVisibleFields(viewService.currentView.senseFields));
</script>

<Editor.Root>
  <Editor.Grid>
    <DiffEntryPrimitive after={entry} />

    {#if showSenses}
      {#each entry.senses as sense, i (sense.id)}
        <Editor.SubGrid>
          <ObjectHeader type="sense" index={i + 1} />
          <DiffSensePrimitive after={sense} />

          {#if showExamples && sense.exampleSentences.length}
            <Editor.SubGrid class="border-l border-dashed pl-4 mt-4 space-y-4 rounded-lg">
              {#each sense.exampleSentences as example, j (example.id)}
                <Editor.SubGrid>
                  <ObjectHeader type="example" index={j + 1} />
                  <DiffExamplePrimitive after={example} />
                </Editor.SubGrid>
              {/each}
            </Editor.SubGrid>
          {/if}
        </Editor.SubGrid>
      {/each}
    {/if}
  </Editor.Grid>
</Editor.Root>
