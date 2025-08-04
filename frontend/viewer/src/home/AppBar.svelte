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
  class="AppBar flex items-center z-50 gap-2 justify-between px-4 max-md:px-1 min-h-12 shadow-md m-3 rounded sticky top-3">
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
