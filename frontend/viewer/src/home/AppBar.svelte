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
  class="flex items-center z-10 space-x-2 justify-between pr-2 px-4 min-h-14 shadow-lg m-3 rounded sticky top-3">
  {@render title()}
  <div class="grow-0"></div>
  {@render actions()}
</header>

<style>
  header {
    /* Stack background colors, so alpha-value only affects the color, not the transparency. */
    background:
      /*
      linear-gradient is just a way to use a solid color and work around: "Only the last background can include a background color."
      See: https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_backgrounds_and_borders/Using_multiple_backgrounds
      */
      linear-gradient(hsl(var(--primary) / 0.4)),
      hsl(var(--background));
  }
</style>
