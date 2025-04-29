<script lang="ts">
  import { run } from 'svelte/legacy';

  interface Props {
    el: Element;
    tag?: string;
  }

  let { el, tag = 'div' }: Props = $props();
  let target: Element = $state();
  run(() => {
    if (target && el)
      // eslint-disable-next-line svelte/no-dom-manipulating
      target.replaceChildren(el);
  });
</script>
<svelte:element this={tag} bind:this={target}/>
