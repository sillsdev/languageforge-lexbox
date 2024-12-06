<script lang="ts">
  import ProjectView from './ProjectView.svelte';
  import {onMount} from 'svelte';
  import {DotnetService} from './lib/dotnet-types';

  export let projectName: string;
  let serviceLoaded = false;
  onMount(() => {
    //check for minilcm in on a timeout
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    let timer: any;
    timer = setInterval(() => {
      if (window.lexbox.DotNetServiceProvider?.getService(DotnetService.MiniLcmApi)) {
        clearTimeout(timer);
        serviceLoaded = true;
      }
    }, 200);
  });
</script>
{#if serviceLoaded}
  <ProjectView {projectName} isConnected></ProjectView>
{/if}
