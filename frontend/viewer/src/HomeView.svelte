<script lang="ts">
  import {
    mdiBookArrowDownOutline,
    mdiBookArrowLeftOutline,
    mdiBookEditOutline,
    mdiBookPlusOutline,
    mdiBookSyncOutline, mdiCloudSync,
    mdiTestTube,
  } from '@mdi/js';
  import {links} from 'svelte-routing';
  import {Button, Card, type ColumnDef, Table, TextField, tableCell, Icon, ProgressCircle} from 'svelte-ux';
  import flexLogo from './lib/assets/flex-logo.png';
  import DevContent, {isDev} from './lib/layout/DevContent.svelte';

  type Project = {
    name: string;
    crdt: boolean;
    fwdata: boolean;
    lexbox: boolean,
    server: string | null,
    id: string | null
  };

  let newProjectName = '';

  let createError: string;

  async function createProject() {
    createError = '';

    if (!newProjectName) {
      createError = 'Project name is required.';
      return;
    }
    const response = await fetch(`/api/project?name=${newProjectName}`, {
      method: 'POST',
    });

    if (!response.ok) {
      createError = await response.json();
      return;
    }

    createError = '';
    newProjectName = '';
    void refreshProjects();
  }


  let importing = '';

  async function importFwDataProject(name: string) {
    importing = name;
    await fetch(`/api/import/fwdata/${name}`, {
      method: 'POST',
    });
    await refreshProjects();
    importing = '';
  }

  let downloading = '';

  async function downloadCrdtProject(project: Project) {
    downloading = project.name;
    await fetch(`/api/download/crdt/${project.server}/${project.name}`, {method: 'POST'});
    await refreshProjects();
    downloading = '';
  }

  let uploading = '';

  async function uploadCrdtProject(project: Project) {
    uploading = project.name;
    await fetch(`/api/upload/crdt/${project.server}/${project.name}`, {method: 'POST'});
    await refreshProjects();
    uploading = '';
  }

  let projectsPromise = fetchProjects();
  let projects: Project[] = [];

  async function fetchProjects() {
    let r = await fetch('/api/localProjects');
    projects = (await r.json()) as Project[];
    return projects;
  }

  async function refreshProjects() {
    let promise = fetchProjects();
    await promise;//avoids clearing out the list until the new list is fetched
    projectsPromise = promise;
  }

  let remoteProjects: { [server: string]: Project[] } = {};
  let loadingRemoteProjects = false;
  async function fetchRemoteProjects() {
    loadingRemoteProjects = true;
    let r = await fetch('/api/remoteProjects');
    remoteProjects = (await r.json()) as { [server: string]: Project[] };
    loadingRemoteProjects = false;
  }
  fetchRemoteProjects();

  type ServerStatus = { displayName: string; loggedIn: boolean; loggedInAs: string | null };
  let servers: ServerStatus[] = [];

  async function fetchServers() {
    let r = await fetch('/api/auth/servers');
    servers = await r.json();
  }

  fetchServers();

  $: columns = [
    {
      name: 'name',
      header: 'Name',
    },
    {
      name: 'fwdata',
      header: 'FieldWorks',
    },
    ...($isDev
      ? [
        {
          name: 'crdt',
          header: 'CRDT',
        },
      ]
      : []),
    ...(servers.find(s => s.loggedIn)
      ? [
        {
          name: 'lexbox',
          header: 'Lexbox',
        },
      ]
      : []),
  ] satisfies ColumnDef<Project>[];

  function matchesProject(projects: Project[], project: Project) {
    let matches: Project | undefined = undefined;
    if (project.id) {
      matches = projects.find(p => p.id == project.id);
    }
    //for now the local project list does not include the id, so fallback to the name
    if (!matches) {
      matches = projects.find(p => p.name === project.name);
    }
    return matches;
  }

  function syncedServer(serversProjects: { [server: string]: Project[] }, project: Project): string | null {
    return Object.entries(serversProjects)
      .find(([server, projects]) => matchesProject(projects, project))?.[0] ?? null;
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
                   data={projects.filter((p) => $isDev || p.fwdata).sort((p1, p2) => p1.name.localeCompare(p2.name))}
                   classes={{ th: 'p-4' }}>
              <tbody slot="data" let:columns let:data let:getCellValue let:getCellContent>
              {#each data ?? [] as rowData, rowIndex}
                <tr class="tabular-nums">
                  {#each columns as column (column.name)}
                    <td use:tableCell={{ column, rowData, rowIndex, tableData: data }} use:links>
                      {#if column.name === 'fwdata'}
                        {#if rowData.fwdata}
                          <Button size="md" href={`/fwdata/${rowData.name}`}>
                            <img src={flexLogo} alt="FieldWorks logo" class="h-6"/>
                            Open
                          </Button>
                        {/if}
                      {:else if column.name === 'lexbox'}
                        {@const server = syncedServer(remoteProjects, rowData)}
                        {#if rowData.crdt && server}
                          <Button disabled color="success" icon={mdiBookSyncOutline} size="md">{server}</Button>
                        {/if}
                      {:else if column.name === 'crdt'}
                        {#if rowData.crdt}
                          <Button
                            icon={mdiBookEditOutline}
                            size="md"
                            href={`/project/${rowData.name}`}
                          >
                            Open
                          </Button>
                        {:else if rowData.fwdata}
                          <Button
                            size="md"
                            loading={importing === rowData.name}
                            icon={mdiBookArrowLeftOutline}
                            on:click={() => importFwDataProject(rowData.name)}
                          >
                            Import
                          </Button>
                        {/if}
                      {:else}
                        {getCellContent(column, rowData, rowIndex)}
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
            {#each servers as server}
              <div class="border my-1"/>
              <div class="flex flex-row items-center">
                <p>{server.displayName}</p>
                <div class="flex-grow"></div>
                {#if server.loggedInAs}
                  <p class="m-1 px-1 text-sm border rounded-full">{server.loggedInAs}</p>
                {/if}
                {#if server.loggedIn}
                  <Button slot="actions" variant="fill" href="/api/auth/logout/{server.displayName}">Logout</Button>
                {:else}
                  <Button slot="actions" variant="fill" href="/api/auth/login/{server.displayName}">Login</Button>
                {/if}
              </div>
              {@const serverProjects = remoteProjects[server.displayName] ?? []}
              {#each serverProjects as project}
                <div class="flex flex-row items-center px-10">
                  <p>{project.name}</p>
                  <div class="flex-grow"></div>
                  {#if matchesProject(projects, project)?.crdt}
                    <Button disabled color="success" icon={mdiBookSyncOutline} size="md">Synced</Button>
                  {:else}
                    <Button
                      icon={mdiBookArrowDownOutline}
                      size="md"
                      loading={downloading === project.name}
                      on:click={() => downloadCrdtProject(project)}
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

<style>
  .home {
    min-height: 100%;
    display: grid;
    grid-template-columns: 1fr auto 1fr;
  }

  .home :global(*:is(td, th)) {
    @apply px-10;
  }
</style>
