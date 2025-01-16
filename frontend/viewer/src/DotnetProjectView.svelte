<script lang="ts">
  import ProjectView from './ProjectView.svelte';
  import {onDestroy, onMount} from 'svelte';
  import {DotnetService, type IMiniLcmJsInvokable} from '$lib/dotnet-types';
  import {useProjectServicesProvider} from '$lib/services/service-provider';
  import {wrapInProxy} from '$lib/services/service-provider-dotnet';
  import type {IProjectScope} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IProjectScope';

  const projectServicesProvider = useProjectServicesProvider();
  export let projectName: string;
  export let type: 'fwdata' | 'crdt';
  let projectScope: IProjectScope;
  let serviceLoaded = false;
  onMount(async () => {
    console.debug('ProjectView mounted');
    if (type === 'crdt') {
      projectScope = await projectServicesProvider.openCrdtProject(projectName);
    } else {
      projectScope = await projectServicesProvider.openFwDataProject(projectName);
    }
    //todo also history service
    window.lexbox.ServiceProvider.setService(DotnetService.MiniLcmApi, wrapInProxy(projectScope.miniLcm) as IMiniLcmJsInvokable);
    serviceLoaded = true;
  });
  onDestroy(() => {
    if (serviceLoaded) {
      window.lexbox.ServiceProvider.removeService(DotnetService.MiniLcmApi);
      if (projectScope.cleanup)
        void projectServicesProvider.disposeService(projectScope.cleanup);
    }
  });
</script>
{#if serviceLoaded}
  <ProjectView {projectName} isConnected></ProjectView>
{/if}
