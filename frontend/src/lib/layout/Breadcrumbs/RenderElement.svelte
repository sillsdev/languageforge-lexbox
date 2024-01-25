<script lang="ts">
  export let el: Element;
  export let tag = 'div';
  let target: Element;
  $: {
    if (target && el)
      // eslint-disable-next-line svelte/no-dom-manipulating
      target.replaceChildren(el);
  }
</script>
<svelte:element this={tag} bind:this={target}/>
