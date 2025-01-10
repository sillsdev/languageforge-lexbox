<script lang="ts">
  import ProjectView from './ProjectView.svelte';
  import {onDestroy, onMount} from 'svelte';
  import {DotnetService, type IMiniLcmJsInvokable} from '$lib/dotnet-types';
  import {useMiniLcmApiProvider} from '$lib/services/service-provider';
  import {wrapInProxy} from '$lib/services/service-provider-dotnet';

  const miniLcmApiProvider = useMiniLcmApiProvider();

  export let projectName: string;
  let serviceLoaded = false;
  onMount(async () => {
    const miniLcmApi = await miniLcmApiProvider.getMiniLcmApi();
    window.lexbox.ServiceProvider.setService(DotnetService.MiniLcmApi, wrapInProxy(miniLcmApi) as IMiniLcmJsInvokable);
    serviceLoaded = true;
  });
  onDestroy(() => {
    if (serviceLoaded) {
      window.lexbox.ServiceProvider.removeService(DotnetService.MiniLcmApi);
      void miniLcmApiProvider.clearMiniLcmApi();
    }
  });
</script>
{#if serviceLoaded}
  <ProjectView {projectName} isConnected></ProjectView>
{/if}
