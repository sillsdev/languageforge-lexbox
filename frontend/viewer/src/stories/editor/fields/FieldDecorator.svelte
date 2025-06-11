<script lang="ts">
  import type { Snippet } from 'svelte';
  import * as Editor from '$lib/components/editor';
  import BreakpointMarkers from '../../decorators/BreakpointMarkers.svelte';
  import {fieldData} from '$lib/entry-editor/field-data';

  let { children, subGrid = false }: { children: Snippet, subGrid: boolean } = $props();

</script>

<Editor.Root>
  <BreakpointMarkers />
  <Editor.Grid>
    <Editor.Field.Root>
      <Editor.Field.Title
        name="Demo field"
        helpId={fieldData.definition.helpId}
      />
      <Editor.Field.Body {subGrid}>
        {@render children()}
      </Editor.Field.Body>
    </Editor.Field.Root>
  </Editor.Grid>
</Editor.Root>
