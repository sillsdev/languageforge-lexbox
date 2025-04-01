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
  import ThemeSyncer from '$lib/ThemeSyncer.svelte';

  const projectServicesProvider = useProjectServicesProvider();

  const {code, type}: {
    code: string; // Code for CRDTs, project-name for FWData
    type: 'fwdata' | 'crdt'
  } = $props();


  let projectName = $state<string>(code);
  let projectScope: IProjectScope;
  let serviceLoaded = $state(false);
  let destroyed = false;
  onMount(async () => {
    console.debug('ProjectView mounted');
    if (type === 'crdt') {
      const projectData = await projectServicesProvider.getCrdtProjectData(code);
      projectName = projectData.name;
      projectScope = await projectServicesProvider.openCrdtProject(code);
    } else {
      projectName = code;
      projectScope = await projectServicesProvider.openFwDataProject(code);
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

<!-- Keeps Svelte-UX and Shadcn theme/mode options in sync. Will die with Svelte-UX -->
<ThemeSyncer />

<ProjectLoader readyToLoadProject={serviceLoaded} {projectName} let:onProjectLoaded>
  <ProjectView {projectName} isConnected onloaded={onProjectLoaded}></ProjectView>
</ProjectLoader>
