<script lang="ts">
  import type {IEntry, IExampleSentence, ISense} from '$lib/dotnet-types';
  import {isEntry, isExample, isSense} from '$lib/utils';
  import EntryEditorPrimitive from '$lib/entry-editor/object-editors/EntryEditorPrimitive.svelte';
  import SenseEditorPrimitive from '$lib/entry-editor/object-editors/SenseEditorPrimitive.svelte';
  import ExampleEditorPrimitive from '$lib/entry-editor/object-editors/ExampleEditorPrimitive.svelte';
  import * as Editor from '$lib/components/editor';

  let {
    object
  }: {
    object: IEntry | ISense | IExampleSentence | undefined
  } = $props();
</script>

<Editor.Root>
  <Editor.Grid>
    {#if isEntry(object)}
      <EntryEditorPrimitive entry={object} readonly />
    {:else if isSense(object)}
      <SenseEditorPrimitive sense={object} readonly />
    {:else if isExample(object)}
      <ExampleEditorPrimitive example={object} readonly />
    {/if}
  </Editor.Grid>
</Editor.Root>
