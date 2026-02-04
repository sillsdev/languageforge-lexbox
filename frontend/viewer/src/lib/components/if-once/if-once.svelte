<script lang="ts">
  import {type Snippet, untrack} from 'svelte';

  const {show, children}: {show: boolean; children: Snippet} = $props();
  // svelte-ignore state_referenced_locally
  let render = $state(show);
  $effect(() => {
    //this should cleanup the effect so it only runs once
    if (untrack(() => render)) return;
    render = show;
  });
</script>

{#if render}
  <div class="contents" class:hidden={!show}>
    {@render children()}
  </div>
{/if}
