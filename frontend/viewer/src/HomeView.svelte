<script lang="ts">
  import {
    mdiBookArrowDownOutline,
    mdiBookArrowLeftOutline,
    mdiBookEditOutline,
    mdiBookPlusOutline,
    mdiBookSyncOutline, mdiChevronRight, mdiCloud, mdiCloudSync,
    mdiTestTube,
  } from '@mdi/js';
  import {
    Button,
    ListItem,
    AppBar
  } from 'svelte-ux';
  import flexLogo from './lib/assets/flex-logo.png';
  import DevContent, {isDev} from './lib/layout/DevContent.svelte';
  import {type Project} from './lib/services/projects-service';
  import {onMount} from 'svelte';
  import {useAuthService, useImportFwdataService, useProjectsService} from './lib/services/service-provider';
  import type {ILexboxServer, IServerStatus} from '$lib/dotnet-types';
  import LoginButton from '$lib/auth/LoginButton.svelte';

  const projectsService = useProjectsService();
  const authService = useAuthService();
  const importFwdataService = useImportFwdataService();
  const exampleProjectName = 'Example-Project';

  async function createProject(projectName: string) {
    await projectsService.createProject(projectName);
    await refreshProjects();
  }

  let importing = '';

  async function importFwDataProject(name: string) {
    if (importing) return;
    importing = name;
    try {
      await importFwdataService.import(name);
      await refreshProjects();
    } finally {
      importing = '';
    }
  }

  let downloading = '';

  async function downloadCrdtProject(project: Project, server: ILexboxServer) {
    downloading = project.name;
    if (project.id == null) throw new Error('Project id is null');
    try {
      await projectsService.downloadProject(project.id, project.name, server);
      await refreshProjects();
    } finally {
      downloading = '';
    }
  }

  let projectsPromise = projectsService.localProjects().then(p => projects = p.sort((p1, p2) => p1.name.localeCompare(p2.name)));
  let projects: Project[] = [];

  async function refreshProjects() {
    let promise = projectsService.localProjects().then(p => p.sort((p1, p2) => p1.name.localeCompare(p2.name)));
    projects = await promise;//avoids clearing out the list until the new list is fetched
    projectsPromise = promise;
  }

  let remoteProjects: { [server: string]: Project[] } = {};
  let loadingRemoteProjects = false;
  async function fetchRemoteProjects(): Promise<void> {
    loadingRemoteProjects = true;
    try {
      let result = await projectsService.remoteProjects();
      for (let serverProjects of result) {
        remoteProjects[serverProjects.server.authority] = serverProjects.projects;
      }
      remoteProjects = remoteProjects;
    } finally {
      loadingRemoteProjects = false;
    }
  }

  fetchRemoteProjects().catch((error) => {
    console.error(`Failed to fetch remote projects`, error);
    throw error;
  });

  async function refreshProjectsAndServers() {
    await fetchRemoteProjects();
    serversStatus = await authService.servers();
  }


  let serversStatus: IServerStatus[] = [];
  onMount(async () => {
    serversStatus = await authService.servers();
  });


  function matchesProject(projects: Project[], project: Project): Project | undefined {
    let matches: Project | undefined = undefined;
    if (project.id) {
      matches = projects.find(p => p.id == project.id && p.serverAuthority == project.serverAuthority);
    }
    return matches;
  }

  function syncedServer(serversProjects: { [server: string]: Project[] }, project: Project, serversStatus: IServerStatus[]): ILexboxServer | undefined {
    //this may be null, even if the project is synced, when the project info isn't cached on the server yet.
    if (project.serverAuthority) {
      return serversStatus.find(s => s.server.id == project.serverAuthority)?.server ?? {
        displayName: 'Unknown server ' + project.serverAuthority,
        authority: project.serverAuthority,
        id: project.serverAuthority
      } satisfies ILexboxServer;
    }
    let authority =  Object.entries(serversProjects)
      .find(([_server, projects]) => matchesProject(projects, project))?.[0];
    return authority ? serversStatus.find(s => s.server.authority == authority)?.server : undefined;
  }
</script>
<AppBar title="FieldWorks Lite" class="bg-secondary min-h-12 shadow-md" menuIcon={null}>
</AppBar>
<div class="contents md:flex md:flex-col md:w-3/4 lg:w-2/4 md:mx-auto md:my-4 md:min-h-full">
  <div class="flex-grow hidden md:block"></div>
  <div class="project-list md:border md:rounded md:p-4">
    {#await projectsPromise}
      <p>loading...</p>
    {:then projects}
      <div>
        {#each projects.filter(p => p.crdt) as project (project.id)}
          {@const server = syncedServer(remoteProjects, project, serversStatus)}
          <a href={`/project/${project.name}`} class="contents">
            <ListItem title={project.name}
                      icon={mdiBookEditOutline}
                      subheading={!server ? 'Local only' : ('Sync with ' + server.displayName)} }>
              <div slot="actions">
                <Button icon={mdiChevronRight} class="p-2"/>
              </div>
            </ListItem>
          </a>
        {/each}
        <a href={`/testing/project-view`} class="contents">
          <ListItem title="Test Project" icon={mdiTestTube}>
            <div slot="actions">
              <Button icon={mdiChevronRight} class="p-2"/>
            </div>
          </ListItem>
        </a>
        {#if !projects.some(p => p.name === exampleProjectName)}
          <ListItem title="Create Example Project" on:click={() => createProject(exampleProjectName)}>
            <div slot="actions">
              <Button icon={mdiBookPlusOutline} class="p-2"/>
            </div>
          </ListItem>
        {/if}
        {#each serversStatus as status}
          {@const server = status.server}
          <div class="flex flex-row items-center py-1 mr-2 mt-2">
            <p class="pl-2">{server.displayName}</p>
            <div class="flex-grow"></div>
            {#if status.loggedInAs}
              <p class="mr-2 px-2 py-1 text-sm border rounded-full">{status.loggedInAs}</p>
            {/if}
            <LoginButton {server} isLoggedIn={status.loggedIn} on:status={() => refreshProjectsAndServers()}/>
          </div>
          {@const serverProjects = remoteProjects[server.authority]?.filter(p => p.crdt) ?? []}
          {#each serverProjects as project}
            {@const localProject = matchesProject(projects, project)}
            <ListItem icon={mdiCloud}
                      title={project.name}
                      on:click={() =>{ if (!localProject?.crdt) {void downloadCrdtProject(project, server);} }}
                      loading={downloading === project.name}>
              <div slot="actions">
                <Button icon={localProject?.crdt ? mdiBookSyncOutline : mdiBookArrowDownOutline} class="p-2">
                  {localProject?.crdt ? 'Synced' : 'Download'}
                </Button>
              </div>
            </ListItem>
          {/each}
        {/each}

        {#if projects.some(p => p.fwdata)}
          <p class="mt-2 pl-2">FieldWorks projects</p>
          {#each projects.filter(p => p.fwdata) as project (project.id ?? project.name)}
            <a href={`/fwdata/${project.name}`} class="contents">
              <ListItem title={project.name}>
                <img slot="avatar" src={flexLogo} alt="FieldWorks logo" class="h-6"/>
                <div slot="actions">
                  <DevContent>
                    <Button
                      loading={importing === project.name}
                      icon={mdiBookArrowLeftOutline}
                      title="Import"
                      disabled={!!importing}
                      on:click={() => importFwDataProject(project.name)}>
                    </Button>
                  </DevContent>
                </div>
              </ListItem>
            </a>
          {/each}
        {/if}
      </div>
    {:catch error}
      <p>Error: {error.message}</p>
    {/await}
  </div>

  <div class="md:flex-grow-[2]"></div>
</div>

<style lang="postcss">
  .project-list {
    display: flex;
    flex-direction: column;
  }
</style>
