<script lang="ts">
  import { preventDefault } from 'svelte/legacy';

  interface Props {
    icon1: string;
    icon2: string;
    text1: string;
    text2: string;
    style: string;
  }

  const { icon1, icon2, text1, text2, style }: Props = $props();
  let isIconOne = $state(true);

  function handleClick(): void {
    isIconOne = !isIconOne;
  }
</script>

<button onclick={preventDefault(handleClick)} class="btn {style} flex items-center">
  {#if isIconOne}
    <span>{text1} </span>
    <span class="{icon1} text-lg"></span>
  {:else}
    <span>{text2} </span>
    <span class="{icon2} text-lg"></span>
  {/if}
</button>
