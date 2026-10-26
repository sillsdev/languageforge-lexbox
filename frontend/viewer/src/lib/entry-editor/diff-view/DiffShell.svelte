<script lang="ts">
  import InputShell from '$lib/components/ui/input/input-shell.svelte';
  import {cn} from '$lib/utils';
  import type {Snippet} from 'svelte';

  let {class: className, children}: {class?: string; children?: Snippet} = $props();
</script>

<!-- Looks like the editor's inputs so values read as fields, but grows to fit instead of clipping. -->
<InputShell class={cn('h-auto min-h-9 items-start justify-start px-3 py-1.5', className)}>
  {@render children?.()}
</InputShell>
