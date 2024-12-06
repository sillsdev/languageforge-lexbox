<script lang="ts">
  import {
    mdiBookArrowDownOutline,
    mdiBookArrowLeftOutline,
    mdiBookEditOutline,
    mdiBookPlusOutline,
    mdiBookSyncOutline, mdiCloudSync,
    mdiLogin,
    mdiLogout,
    mdiTestTube,
  } from '@mdi/js';
  import {links} from 'svelte-routing';
  import {Button, Card, type ColumnDef, Table, TextField, tableCell, Icon, ProgressCircle} from 'svelte-ux';
  import flexLogo from './lib/assets/flex-logo.png';
  import DevContent, {isDev} from './lib/layout/DevContent.svelte';
  import {type Project} from './lib/services/projects-service';
  import {onMount} from 'svelte';
  import {useAuthService, useImportFwdataService, useProjectsService} from './lib/services/service-provider';
  import type {ILexboxServer, IServerStatus} from '$lib/dotnet-types';

  const projectsService = useProjectsService();
  const authService = useAuthService();
  const importFwdataService = useImportFwdataService();

  let newProjectName = '';

  let createError: string;

  async function createProject() {
    await projectsService.createProject(newProjectName);
    newProjectName = '';
    await refreshProjects();
  }

  let importing = '';

  async function importFwDataProject(name: string) {
    if (importing) return;
    importing = name;
    await importFwdataService.import(name);
    await refreshProjects();
    importing = '';
  }

  let downloading = '';

  async function downloadCrdtProject(project: Project, server: ILexboxServer) {
    downloading = project.name;
    if (project.id == null) throw new Error('Project id is null');
    await projectsService.downloadProject(project.id, project.name, server);
    await refreshProjects();
    downloading = '';
  }

  let projectsPromise = projectsService.localProjects().then(p => projects = p);
  let projects: Project[] = [];

  async function refreshProjects() {
    let promise = projectsService.localProjects();
    projects = await promise;//avoids clearing out the list until the new list is fetched
    projectsPromise = promise;
  }

  let remoteProjects: { [server: string]: Project[] } = {};
  let loadingRemoteProjects = false;
  async function fetchRemoteProjects(): Promise<void> {
    loadingRemoteProjects = true;
    let result = await projectsService.remoteProjects();
    for (let serverProjects of result) {
      remoteProjects[serverProjects.server.authority] = serverProjects.projects;
    }
    loadingRemoteProjects = false;
  }

  fetchRemoteProjects().catch((error) => {
    console.error(`Failed to fetch remote projects`, error);
    throw error;
  });


  let serversStatus: IServerStatus[] = [];
  onMount(async () => serversStatus = await authService.servers());

  $: columns = [
    {
      name: 'name',
      header: 'Name',
    },
    {
      name: 'fwdata',
      header: 'FieldWorks',
    },
    {
      name: 'crdt',
      header: 'CRDT',
    },
    ...(serversStatus.find(s => s.loggedIn)
      ? [
        {
          name: 'lexbox',
          header: 'Lexbox',
        },
      ]
      : []),
  ] satisfies ColumnDef<Project>[];

  function matchesProject(projects: Project[], project: Project): Project | undefined {
    let matches: Project | undefined = undefined;
    if (project.id) {
      matches = projects.find(p => p.id == project.id && p.serverAuthority == project.serverAuthority);
    }
    return matches;
  }

  function syncedServer(serversProjects: { [server: string]: Project[] }, project: Project): ILexboxServer | undefined {
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

<div class="home">
  <DevContent>
    <div>
      <Card title="Create Project" class="w-fit m-4">
        <TextField
          label="New Project Name"
          class="m-4"
          placeholder="Project Name"
          bind:value={newProjectName}
          error={createError}
        />
        <Button slot="actions" variant="fill" icon={mdiBookPlusOutline} on:click={createProject}>Create Project</Button>
      </Card>
    </div>
  </DevContent>
  <div class="col-start-2 p-6 flex flex-col">
    <div class="flex-grow"></div>
    <div>
      <div class="text-center text-3xl mb-8">My projects</div>
      <Card class="p-6 shadow-2xl">
        <div slot="contents">
          {#await projectsPromise}
            <p>loading...</p>
          {:then projects}
            <Table {columns}
                   data={projects.filter((p) => p.fwdata || p.crdt).sort((p1, p2) => p1.name.localeCompare(p2.name))}
                   classes={{ th: 'p-4' }}>
              <tbody slot="data" let:columns let:data let:getCellContent>
              {#each data ?? [] as project, rowIndex}
                <tr class="tabular-nums">
                  {#each columns as column (column.name)}
                    <td use:tableCell={{ column, rowData:project, rowIndex, tableData: data }} use:links>
                      {#if column.name === 'fwdata'}
                        {#if project.fwdata}
                          <Button size="md" href={`/fwdata/${project.name}`}>
                            <img src={flexLogo} alt="FieldWorks logo" class="h-6"/>
                            Open
                          </Button>
                        {/if}
                      {:else if column.name === 'lexbox'}
                        {@const server = syncedServer(remoteProjects, project)}
                        {#if project.crdt && server}
                          <Button disabled color="success" icon={mdiBookSyncOutline} size="md">{server.displayName}</Button>
                        {/if}
                      {:else if column.name === 'crdt'}
                        {#if project.crdt}
                          <Button
                            icon={mdiBookEditOutline}
                            size="md"
                            href={`/project/${project.name}`}
                          >
                            Open
                          </Button>
                        {:else if project.fwdata}
                          <Button
                            size="md"
                            loading={importing === project.name}
                            icon={mdiBookArrowLeftOutline}
                            disabled={!!importing}
                            on:click={() => importFwDataProject(project.name)}
                          >
                            Import
                          </Button>
                        {/if}
                      {:else}
                        {getCellContent(column, project, rowIndex)}
                      {/if}
                    </td>
                  {/each}
                </tr>
              {/each}

              <DevContent>
                <tr class="tabular-nums">
                  <td>
                    Test project
                  </td>
                  <td use:links>
                    <Button size="md" icon={mdiTestTube} href="/testing/project-view">
                      Open
                    </Button>
                  </td>
                </tr>
              </DevContent>
              </tbody>
            </Table>
          {:catch error}
            <p>Error: {error.message}</p>
          {/await}
          <div class="mt-4">
            <div class="text-lg font-bold"><Icon path={mdiCloudSync}/> Remote projects
              {#if loadingRemoteProjects}
                  <ProgressCircle class="text-surface-content" indeterminate={true}/>
              {/if}
            </div>
            {#each serversStatus as status}
              {@const server = status.server}
              <div class="border my-1"/>
              <div class="flex flex-row items-center py-1">
                <p>{server.displayName}</p>
                <div class="flex-grow"></div>
                {#if status.loggedInAs}
                  <p class="mr-2 px-2 py-1 text-sm border rounded-full">{status.loggedInAs}</p>
                {/if}
                {#if status.loggedIn}
                  <Button variant="fill" color="primary" href="/api/auth/logout/{server.id}" icon={mdiLogout}>Logout</Button>
                {:else}
                  <Button variant="fill-light" color="primary" href="/api/auth/login/{server.id}" icon={mdiLogin}>Login</Button>
                {/if}
              </div>
              {@const serverProjects = remoteProjects[server.authority]?.filter(p => p.crdt) ?? []}
              {#each serverProjects as project}
                {@const localProject = matchesProject(projects, project)}
                <div class="flex flex-row items-center px-10">
                  <p>{project.name}</p>
                  <div class="flex-grow"></div>
                  {#if localProject?.crdt}
                    <Button disabled color="success" icon={mdiBookSyncOutline} size="md">Synced</Button>
                  {:else}
                    <Button
                      icon={mdiBookArrowDownOutline}
                      size="md"
                      loading={downloading === project.name}
                      on:click={() => downloadCrdtProject(project, server)}
                    >
                      Download
                    </Button>
                  {/if}
                </div>
              {/each}
            {/each}
          </div>
        </div>
      </Card>
    </div>
    <div class="flex-grow-[2]"></div>
  </div>
</div>

<style lang="postcss">
  .home {
    min-height: 100%;
    display: grid;
    grid-template-columns: 1fr auto 1fr;
  }

  .home :global(*:is(td, th)) {
    @apply px-10;
  }
</style>
