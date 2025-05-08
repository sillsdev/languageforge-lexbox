<script lang="ts">
  interface Props {
    el: Element;
    tag?: string;
  }

  const { el, tag = 'div' }: Props = $props();
  let target: Element | undefined = $state();
  // TODO: Once PageBreadcrumb stores snippets, just render them instead of manipulating DOM ourselves
  $effect(() => {
    if (el)
      // eslint-disable-next-line svelte/no-dom-manipulating
      target?.replaceChildren(el);
  });
</script>

<svelte:element this={tag} bind:this={target} />
