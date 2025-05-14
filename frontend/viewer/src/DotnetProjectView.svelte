<script lang="ts">
  import ProjectView from './ProjectView.svelte';
  import {onDestroy, onMount} from 'svelte';
  import {type IMiniLcmJsInvokable} from '$lib/dotnet-types';
  import {useProjectServicesProvider} from '$lib/services/service-provider';
  import {wrapInProxy} from '$lib/services/service-provider-dotnet';
  import type {IProjectScope} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IProjectScope';
  import type {
    IHistoryServiceJsInvokable
  } from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IHistoryServiceJsInvokable';
  import ProjectLoader from './ProjectLoader.svelte';
  import {initProjectContext} from '$lib/project-context.svelte';

  const projectServicesProvider = useProjectServicesProvider();
  const projectContext = initProjectContext();

  const {code, type: projectType}: {
    code: string; // Code for CRDTs, project-name for FWData
    type: 'fwdata' | 'crdt'
  } = $props();



  let projectName = $state<string>(code);
  let projectScope: IProjectScope;
  let serviceLoaded = $state(false);
  let destroyed = false;
  onMount(async () => {
    console.debug('ProjectView mounted');
    if (projectType === 'crdt') {
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
    let historyService: IHistoryServiceJsInvokable | undefined = undefined;
    if (projectScope.historyService) {
      historyService = wrapInProxy(projectScope.historyService, 'HistoryService') as IHistoryServiceJsInvokable;
    }
    const api = wrapInProxy(projectScope.miniLcm, 'MiniLcmApi') as IMiniLcmJsInvokable;
    projectContext.setup({api, historyService, projectName, projectType});
    serviceLoaded = true;
  });
  onDestroy(() => {
    destroyed = true;
    if (serviceLoaded) {
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
  <ProjectView isConnected onloaded={onProjectLoaded}></ProjectView>
</ProjectLoader>
