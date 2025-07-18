<script lang="ts">
  import {fade} from 'svelte/transition';
  import Loading from '$lib/components/Loading.svelte';
  import type {Snippet} from 'svelte';

  interface Props {
    readyToLoadProject?: boolean;
    projectName: string;
    loading: boolean;
    children: Snippet;
  }

  let {readyToLoadProject = true, projectName, children, loading}: Props = $props();
</script>

{#if loading}
  <div class="absolute w-full h-full z-10 flex grow items-center justify-center" out:fade={{duration: 800}}>
    <div class="inline-flex flex-col items-center text-4xl gap-4 opacity-75 m-auto p-4 text-center">
      <span>Loading <span class="text-primary">{projectName}</span>...</span>
      <Loading class="size-14"/>
    </div>
  </div>
{/if}

{#if readyToLoadProject}
  <div class:hidden={loading} class:contents={!loading}>
    {@render children()}
  </div>
{/if}
