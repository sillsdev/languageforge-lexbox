<script module lang="ts">
  /** Fake service calls still running, so a play function can wait until the screen has settled. */
  export const fakeCalls = {pending: 0};
</script>

<script lang="ts">
  // Renders the real home screen on top of fake project/auth services, so it can be shown (and
  // screenshotted for the docs) without a .NET host or a Lexbox login.
  import HomeView from '../../home/HomeView.svelte';
  import {Router} from 'svelte-routing';
  import {DotnetService, LoginResult} from '$lib/dotnet-types';
  import type {IAuthService, ILexboxServer, IProjectModel, IServerStatus} from '$lib/dotnet-types';
  import type {ICombinedProjectsService} from '$lib/dotnet-types/generated-types/FwLiteShared/Projects/ICombinedProjectsService';
  import type {IImportFwdataService} from '$lib/dotnet-types/generated-types/FwLiteShared/Projects/IImportFwdataService';
  import {DownloadProjectByCodeResult} from '$lib/dotnet-types/generated-types/FwLiteShared/Projects/DownloadProjectByCodeResult';
  import {ProjectRole} from '$lib/dotnet-types/generated-types/LexCore/Entities/ProjectRole';
  import {ProjectDataFormat} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ProjectDataFormat';

  interface Props {
    /** Logged in to the Lexbox server, with projects to download. */
    loggedIn?: boolean;
    /** Projects already on this device. */
    localProjects?: IProjectModel[];
    /** Adds a second server that isn't logged in. */
    otherServer?: boolean;
  }

  const server: ILexboxServer = {id: 'lexbox', displayName: 'Lexbox', authority: 'https://lexbox.org'};

  function project(name: string, code: string, role = ProjectRole.Editor): IProjectModel {
    return {name, code, id: `${code}-id`, crdt: true, fwdata: false, lexbox: true, role, server};
  }

  export const demoLocalProjects: IProjectModel[] = [project('Sena 3', 'qaa-sena-flex')];
  export const demoRemoteProjects: IProjectModel[] = [
    ...demoLocalProjects,
    project('Example Dictionary', 'qaa-flex'),
    project('Training Project', 'qaa-train-flex', ProjectRole.Observer),
  ];

  const {loggedIn = true, localProjects = demoLocalProjects, otherServer = false}: Props = $props();

  // Props are read once on purpose: the services are registered when the story mounts.
  // svelte-ignore state_referenced_locally
  const status: IServerStatus = loggedIn
    ? {server, loggedIn: true, loggedInAs: 'pat@example.com', displayName: 'Pat Example'}
    : {server, loggedIn: false, displayName: 'Lexbox'};
  const other: ILexboxServer = {id: 'other', displayName: 'Other', authority: 'https://other.example.org'};
  // svelte-ignore state_referenced_locally
  const statuses: IServerStatus[] = otherServer ? [status, {server: other, loggedIn: false, displayName: 'Other'}] : [status];

  // Like the real JSInterop services: async, and every call returns new objects.
  async function respond<T>(value: () => T, ms = 0): Promise<T> {
    fakeCalls.pending++;
    try {
      await new Promise(resolve => setTimeout(resolve, ms));
      return value();
    } finally {
      // Late, so the gap before a caller's next chained call doesn't look settled.
      setTimeout(() => fakeCalls.pending--);
    }
  }
  function copy(projects: IProjectModel[]) {
    return projects.map(p => ({...p}));
  }

  // svelte-ignore state_referenced_locally
  let downloaded = [...localProjects];
  function serverProjects({server, loggedIn}: IServerStatus) {
    return {server, projects: loggedIn ? copy(demoRemoteProjects) : [], canDownloadByCode: false};
  }

  const authService: IAuthService = {
    servers: () => respond(() => statuses.map(s => ({...s}))),
    signInWebView: () => Promise.resolve(LoginResult.Success),
    useSystemWebView: () => Promise.resolve(true),
    logout: () => Promise.resolve(),
  };
  const projectsService: ICombinedProjectsService = {
    supportsFwData: () => Promise.resolve(false),
    localProjects: () => respond(() => copy(downloaded)),
    remoteProjects: () => respond(() => statuses.map(serverProjects)),
    serverProjects: (id) => respond(() => serverProjects(statuses.find(s => s.server.id === id)!)),
    downloadProject: (project) => respond(() => {
      downloaded = [...downloaded, project];
    }, 200),
    downloadProjectByCode: () => Promise.resolve(DownloadProjectByCodeResult.Success),
    createProject: () => Promise.resolve(),
    createDemoProject: () => Promise.resolve(),
    deleteProject: (code) => respond(() => {
      downloaded = downloaded.filter(p => p.code !== code);
    }),
    openFromFolder: () => Promise.reject(new Error('Folder sync is not part of this story')),
    syncProjectWithFolder: () => Promise.reject(new Error('Folder sync is not part of this story')),
  };
  const importFwdataService: IImportFwdataService = {
    import: (projectName: string) => Promise.resolve({name: projectName, dataFormat: ProjectDataFormat.Harmony}),
  };

  window.lexbox.ServiceProvider.setService(DotnetService.AuthService, authService);
  window.lexbox.ServiceProvider.setService(DotnetService.CombinedProjectsService, projectsService);
  window.lexbox.ServiceProvider.setService(DotnetService.ImportFwdataService, importFwdataService);
</script>

<Router>
  <HomeView />
</Router>
