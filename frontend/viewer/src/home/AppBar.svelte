<script lang="ts">
  import type {Snippet} from 'svelte';

  interface Props {
    title: Snippet;
    tabTitle?: string;
    actions: Snippet;
  }

  let {title, actions, tabTitle}: Props = $props();
  $effect(() => {
    if (tabTitle) {
      document.title = tabTitle;
    }
  });
</script>
<header
  class="AppBar flex items-center z-50 space-x-2 justify-between pr-1 md:pr-3 pl-3 min-h-12 shadow-lg m-3 mb-0 rounded sticky top-3">
  {@render title()}
  <div class="grow-0"></div>
  {@render actions()}
</header>
<style>
  .AppBar {
    --shading: 1.5;
    background-color: hsl(from hsl(var(--primary)) h s calc(l * var(--shading)));
  }

  :global(.dark) .AppBar {
    --shading: 0.5;
  }

</style>
