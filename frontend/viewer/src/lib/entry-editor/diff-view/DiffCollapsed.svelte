<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {tick} from 'svelte';
  import {t} from 'svelte-i18n-lingui';
  import type {DiffFocus} from './diff-focus.svelte';

  // `unchanged`: the unchanged fields with a value that collapsing hides
  let {focus, unchanged}: {focus: DiffFocus; unchanged: number} = $props();
  let element = $state<HTMLElement>();

  async function toggle() {
    focus.expanded = !focus.expanded;
    if (!focus.expanded) {
      await tick();
      // Collapsing shrinks the diff above the button, which can carry it out of view
      element?.scrollIntoView({block: 'nearest'});
    }
  }
</script>

{#if focus.active && unchanged + focus.unchangedValues > 0}
  <div class="col-span-full" bind:this={element}>
    <Button variant="ghost" size="sm" class="text-muted-foreground h-auto whitespace-normal"
            icon={focus.expanded ? 'i-mdi-chevron-up' : 'i-mdi-chevron-down'}
            aria-expanded={focus.expanded}
            onclick={toggle}>
      {focus.expanded ? $t`Hide unchanged fields` : $t`Show unchanged fields`}
    </Button>
  </div>
{/if}
