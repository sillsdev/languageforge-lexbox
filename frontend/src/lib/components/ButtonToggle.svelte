<script lang="ts">
  export let icon1: string;
  export let icon2: string;
  export let text1: string;
  export let text2: string;
  export let theme: string;
  let isIconOne = true;

  function handleClick(): void {
    isIconOne = !isIconOne;
  }
</script>

<button on:click|preventDefault={handleClick} class="btn btn-{theme} flex items-center">
  {#if isIconOne}
    <span>{text1} </span>
    <span class="{icon1} text-lg"></span>
  {:else}
    <span>{text2} </span>
    <span class="{icon2} text-lg"></span>
  {/if}
</button>
