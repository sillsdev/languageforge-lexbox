<script lang="ts">
  interface Props {
    children?: import('svelte').Snippet;
  }

  let { children }: Props = $props();
</script>

<main class="max-w-none px-2 md:px-6 py-8 flex-grow flex flex-col">
  {@render children?.()}
</main>
