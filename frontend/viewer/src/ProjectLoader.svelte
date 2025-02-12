<script lang="ts">
  import {ProgressCircle} from 'svelte-ux';
  import {writable} from 'svelte/store';
  import {fade} from 'svelte/transition';

  export let readyToLoadProject = true;
  export let projectName: string;

  let projectLoaded = writable(false);
  $: loading = !readyToLoadProject || !$projectLoaded;
</script>

{#if loading}
  <div class="absolute w-full h-full z-10 bg-surface-100 flex grow items-center justify-center" out:fade={{duration: 800}}>
    <div class="inline-flex flex-col items-center text-4xl gap-4 opacity-75">
      <span>Loading <span class="text-primary-500">{projectName}</span>...</span><ProgressCircle class="text-surface-content" />
    </div>
  </div>
{/if}

{#if readyToLoadProject}
  <div class:hidden={!$projectLoaded} class:contents={$projectLoaded}>
    <slot onProjectLoaded={projectLoaded.set} />
  </div>
{/if}
