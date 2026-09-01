<script lang="ts">
  import type {Snippet} from 'svelte';
  import {hasFlag, type FeatureFlag} from './feature-flags.svelte';

  interface Props {
    flag: FeatureFlag;
    invisible?: boolean;
    children?: Snippet;
  }

  const {flag, invisible = false, children}: Props = $props();
</script>

{#if hasFlag(flag)}
  {@render children?.()}
{:else if invisible}
  <div class="invisible">
    {@render children?.()}
  </div>
{/if}
