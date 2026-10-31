<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {tick} from 'svelte';
  import {t} from 'svelte-i18n-lingui';

  // Not persisted: each diff starts focused on its changes.
  let {count, expanded = $bindable(false)}: {count: number; expanded?: boolean} = $props();
  let element = $state<HTMLElement>();

  async function toggle() {
    expanded = !expanded;
    if (expanded) return;
    await tick();
    // Collapsing shrinks the diff above the button, which can carry it out of view
    element?.scrollIntoView({block: 'nearest'});
  }
</script>

{#if count > 0}
  <div class="col-span-full" bind:this={element}>
    <Button variant="ghost" size="sm" class="text-muted-foreground h-auto whitespace-normal"
            icon={expanded ? 'i-mdi-chevron-up' : 'i-mdi-chevron-down'}
            aria-expanded={expanded}
            onclick={toggle}>
      {expanded ? $t`Hide unchanged fields` : $t`Show unchanged fields`}
    </Button>
  </div>
{/if}
