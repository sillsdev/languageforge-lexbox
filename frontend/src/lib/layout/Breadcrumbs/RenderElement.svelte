<script lang="ts">
  export let el: Element;
  export let tag = 'div';
  let target: Element;
  $: {
    if (target && el)
      target.replaceChildren(el);
  }
</script>
<svelte:element this={tag} bind:this={target}/>
