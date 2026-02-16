<script lang="ts">
  import type {Snippet} from 'svelte';
  import * as Editor from '$lib/components/editor';
  import {fieldData} from '$lib/views/fields';
  import EntityEditorPrimitiveDecorator from '../entity-primitives/EntityEditorPrimitiveDecorator.svelte';

  let { children, subGrid = false }: { children: Snippet; subGrid: boolean } = $props();
</script>

<EntityEditorPrimitiveDecorator>
  <Editor.Field.Root>
    <Editor.Field.Title name="Demo field" helpId={fieldData.sense.definition.helpId} />
    <Editor.Field.Body {subGrid}>
      {@render children()}
    </Editor.Field.Body>
  </Editor.Field.Root>
</EntityEditorPrimitiveDecorator>
