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

  const {loggedIn = true, localProjects = demoLocalProjects}: Props = $props();

  // Props are read once on purpose: the services are registered when the story mounts.
  // svelte-ignore state_referenced_locally
  const status: IServerStatus = loggedIn
    ? {server, loggedIn: true, loggedInAs: 'pat@example.com', displayName: 'Pat Example'}
    : {server, loggedIn: false, displayName: 'Lexbox'};

  const authService: IAuthService = {
    servers: () => Promise.resolve([status]),
    signInWebView: () => Promise.resolve(LoginResult.Success),
    useSystemWebView: () => Promise.resolve(true),
    logout: () => Promise.resolve(),
  };
  const projectsService: ICombinedProjectsService = {
    supportsFwData: () => Promise.resolve(false),
    localProjects: () => Promise.resolve(localProjects),
    remoteProjects: () => Promise.resolve(loggedIn ? [{server, projects: demoRemoteProjects, canDownloadByCode: false}] : []),
    serverProjects: () => Promise.resolve(loggedIn ? {server, projects: demoRemoteProjects, canDownloadByCode: false} : undefined),
    downloadProject: () => Promise.resolve(),
    downloadProjectByCode: () => Promise.resolve(DownloadProjectByCodeResult.Success),
    createProject: () => Promise.resolve(),
    createDemoProject: () => Promise.resolve(),
    deleteProject: () => Promise.resolve(),
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
