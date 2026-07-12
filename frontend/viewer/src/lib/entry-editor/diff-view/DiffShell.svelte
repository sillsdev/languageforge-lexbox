<script lang="ts">
  import InputShell from '$lib/components/ui/input/input-shell.svelte';
  import {cn} from '$lib/utils';
  import type {Snippet} from 'svelte';

  let {class: className, children}: {class?: string; children?: Snippet} = $props();
</script>

<!-- Read-only field box that mirrors the editor's inputs so the diff reads as fields, not bare text.
     Grows vertically (unlike the editor's fixed-height inputs) so longer values aren't clipped. -->
<InputShell class={cn('h-auto min-h-9 items-start justify-start px-3 py-1.5', className)}>
  {@render children?.()}
</InputShell>
