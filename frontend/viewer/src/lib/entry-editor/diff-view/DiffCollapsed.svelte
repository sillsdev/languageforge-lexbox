<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {plural, t} from 'svelte-i18n-lingui';

  // Not persisted: each diff starts focused on its changes.
  let {count, expanded = $bindable(false)}: {count: number; expanded?: boolean} = $props();
</script>

{#if count > 0}
  <div class="col-span-full">
    <Button variant="ghost" size="sm" class="text-muted-foreground"
            icon={expanded ? 'i-mdi-chevron-up' : 'i-mdi-chevron-down'}
            aria-expanded={expanded}
            onclick={() => (expanded = !expanded)}>
      {expanded
        ? $t`Hide unchanged fields`
        : $plural(count, {one: 'Show # unchanged field', other: 'Show # unchanged fields'})}
    </Button>
  </div>
{/if}
