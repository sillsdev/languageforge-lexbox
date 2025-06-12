<script lang="ts">
  import type { Snippet } from 'svelte';
  import * as Editor from '$lib/components/editor';
  import BreakpointMarkers from '../../decorators/BreakpointMarkers.svelte';

  let { children }: { children: Snippet } = $props();

</script>

<Editor.Root>
  <BreakpointMarkers />
  <Editor.Grid>
    {@render children()}
  </Editor.Grid>
</Editor.Root>
