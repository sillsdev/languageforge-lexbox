<script lang="ts">
  import DiffText from './DiffText.svelte';
  import DiffShell from './DiffShell.svelte';

  let {before, after, format}: {
    before?: unknown;
    after?: unknown;
    format?: (value: unknown) => string;
  } = $props();

  function fmt(value: unknown): string {
    if (value === undefined || value === null) return '';
    if (format) return format(value);
    if (typeof value === 'string') return value;
    return JSON.stringify(value);
  }
</script>

<DiffShell>
  <DiffText before={fmt(before)} after={fmt(after)} />
</DiffShell>
