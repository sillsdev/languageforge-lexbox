<script lang="ts">
  import type {IEntry, IExampleSentence, ISense} from '$lib/dotnet-types';
  import {isEntry, isExample, isSense} from '$lib/utils';
  import EntryEditorPrimitive from '$lib/entry-editor/object-editors/EntryEditorPrimitive.svelte';
  import SenseEditorPrimitive from '$lib/entry-editor/object-editors/SenseEditorPrimitive.svelte';
  import ExampleEditorPrimitive from '$lib/entry-editor/object-editors/ExampleEditorPrimitive.svelte';

  let {
    object
  }: {
    object: IEntry | ISense | IExampleSentence | undefined
  } = $props();
</script>
{#if isEntry(object)}
  <EntryEditorPrimitive entry={object} readonly />
{:else if isSense(object)}
  <SenseEditorPrimitive sense={object} readonly />
{:else if isExample(object)}
  <ExampleEditorPrimitive example={object} readonly />
{/if}
