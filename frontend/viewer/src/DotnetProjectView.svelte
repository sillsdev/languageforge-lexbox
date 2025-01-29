<script lang="ts">
  import ProjectView from './ProjectView.svelte';
  import {onDestroy, onMount} from 'svelte';
  import {DotnetService, type IMiniLcmJsInvokable} from '$lib/dotnet-types';
  import {useProjectServicesProvider} from '$lib/services/service-provider';
  import {wrapInProxy} from '$lib/services/service-provider-dotnet';
  import type {IProjectScope} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IProjectScope';
  import type {
    IHistoryServiceJsInvokable
  } from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IHistoryServiceJsInvokable';
  import ProjectLoader from './ProjectLoader.svelte';

  const projectServicesProvider = useProjectServicesProvider();
  export let projectName: string;
  export let type: 'fwdata' | 'crdt';
  let projectScope: IProjectScope;
  let serviceLoaded = false;
  let destroyed = false;
  onMount(async () => {
    console.debug('ProjectView mounted');
    if (type === 'crdt') {
      projectScope = await projectServicesProvider.openCrdtProject(projectName);
    } else {
      projectScope = await projectServicesProvider.openFwDataProject(projectName);
    }
    if (destroyed) {
      cleanup();
      return;
    }
    if (projectScope.historyService) {
      window.lexbox.ServiceProvider.setService(DotnetService.HistoryService, wrapInProxy(projectScope.historyService, 'HistoryService') as IHistoryServiceJsInvokable);
    }
    window.lexbox.ServiceProvider.setService(DotnetService.MiniLcmApi, wrapInProxy(projectScope.miniLcm, 'MiniLcmApi') as IMiniLcmJsInvokable);
    serviceLoaded = true;
  });
  onDestroy(() => {
    destroyed = true;
    if (serviceLoaded) {
      window.lexbox.ServiceProvider.removeService(DotnetService.MiniLcmApi);
      cleanup();
    }
  });

  function cleanup() {
    setTimeout(() => {
      if (projectScope.cleanup)
        void projectServicesProvider.disposeService(projectScope.cleanup);
    }, 1000);
  }
</script>

<ProjectLoader readyToLoadProject={serviceLoaded} {projectName} let:onProjectLoaded>
  <ProjectView {projectName} isConnected on:loaded={e => onProjectLoaded(e.detail)}></ProjectView>
</ProjectLoader>
