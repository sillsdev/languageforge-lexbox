<script lang="ts">
  import ProjectView from './ProjectView.svelte';
  import {onDestroy, onMount, untrack} from 'svelte';
  import {DotnetService} from '$lib/dotnet-types';
  import {useProjectServicesProvider} from '$lib/services/service-provider';
  import {wrapInProxy} from '$lib/services/service-provider-dotnet';
  import type {IProjectScope} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IProjectScope';
  import type {
    IHistoryServiceJsInvokable
  } from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IHistoryServiceJsInvokable';
  import type {
    ISyncServiceJsInvokable
  } from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
  import {initProjectContext} from '$project/project-context.svelte';
  import {initProjectStorage} from '$lib/storage';
  import {useEventBus} from '$lib/services/event-bus';
  import type {IMediaFilesServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMediaFilesServiceJsInvokable';
  import {AppNotification, type LoadingNotification} from '$lib/notifications/notifications';
  import {t} from 'svelte-i18n-lingui';

  const projectServicesProvider = useProjectServicesProvider();

  const {code: _code, type: projectType, paratext = false}: {
    code: string; // Code for CRDTs, project-name for FWData
    type: 'fwdata' | 'crdt';
    paratext?: boolean;
  } = $props();
  const projectContext = initProjectContext();
  const code = untrack(() => _code);
  projectContext.projectCode = code;
  projectContext.inParatext = untrack(() => paratext);
  // Before openCrdtProject (in onMount): opening triggers a sync that can change the role.
  projectContext.watchProjectRole(useEventBus());

  initProjectStorage(code);


  let projectName = $state<string>(code);
  let projectScope: IProjectScope;
  let serviceLoaded = $state(false);
  let destroyed = false;
  let endOpeningNotice: (() => void) | undefined;
  onMount(async () => {
    console.debug('ProjectView mounted');
    projectContext.projectCode = code;
    projectContext.projectType = projectType;
    if (projectType === 'crdt') {
      endOpeningNotice = showOpeningNotice();
      try {
        const maybeProjectName = await projectServicesProvider.tryGetCrdtProjectName(code);
        projectName = maybeProjectName ? maybeProjectName : code;
        projectScope = await projectServicesProvider.openCrdtProject(code);
      } finally {
        endOpeningNotice();
      }
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
      historyService = wrapInProxy(projectScope.historyService, DotnetService.HistoryService);
    }
    let syncService: ISyncServiceJsInvokable | undefined = undefined;
    if (projectScope.syncService) {
      syncService = wrapInProxy(projectScope.syncService, DotnetService.SyncService);
    }
    let mediaFilesService: IMediaFilesServiceJsInvokable | undefined = undefined;
    if (projectScope.mediaFilesService) {
      mediaFilesService = wrapInProxy(projectScope.mediaFilesService, DotnetService.MediaFilesService);
    }
    const api = wrapInProxy(projectScope.miniLcm, DotnetService.MiniLcmApi);
    projectContext.setup({
      api,
      historyService,
      syncService,
      mediaFilesService,
      projectName,
      projectCode: code,
      projectType,
      server: projectScope.server,
      projectData: projectScope.projectData
    });
    serviceLoaded = true;
  });
  onDestroy(() => {
    destroyed = true;
    endOpeningNotice?.();
    if (serviceLoaded) {
      cleanup();
    }
  });

  // Opening can rebuild the whole project after an update, which can look stuck without an explanation
  function showOpeningNotice(): () => void {
    let notice: LoadingNotification | undefined;
    const noticeTimer = setTimeout(() => notice = AppNotification.loading($t`Opening project...`), 1500);
    const slowTimer = setTimeout(() => notice?.update($t`Still opening the project...`,
      $t`Making sure the project is up to date can take a few minutes. Please keep the app open.`), 8000);
    return () => {
      clearTimeout(noticeTimer);
      clearTimeout(slowTimer);
      notice?.dismiss();
      notice = undefined;
    };
  }

  function cleanup() {
    setTimeout(() => {
      if (projectScope.cleanup)
        void projectServicesProvider.disposeService(projectScope.cleanup);
    }, 1000);
  }
</script>


<ProjectView data-paratext={paratext}/>


