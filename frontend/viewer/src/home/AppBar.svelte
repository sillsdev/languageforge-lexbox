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
  })
</script>
<header class="flex items-center relative z-50 gap-2 justify-between px-4 max-md:px-1 min-h-12 bg-primary/50 dark:bg-primary/70 shadow-md m-3 rounded sticky top-3">
  {@render title()}
  <div class="grow-0"></div>
  {@render actions()}
</header>
