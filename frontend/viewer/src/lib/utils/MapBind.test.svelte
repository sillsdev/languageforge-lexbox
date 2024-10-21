<script lang="ts">
  import type { Writable } from 'svelte/store';
  import MapBind from './MapBind.svelte';

  export let a: Writable<string | undefined>;
  export let b: Writable<{ id: string } | undefined>;
</script>

<MapBind
  bind:in={$a}
  bind:out={$b}
  map={(_a) => (_a ? { id: _a } : undefined)}
  unmap={(_b) => (_b ? _b.id : undefined)}
/>
