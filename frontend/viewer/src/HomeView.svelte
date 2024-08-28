<script lang="ts">
  import {
    mdiBookArrowDownOutline,
    mdiBookArrowLeftOutline,
    mdiBookArrowUpOutline,
    mdiBookEditOutline,
    mdiBookPlusOutline,
    mdiBookSyncOutline,
    mdiTestTube,
  } from '@mdi/js';
  import { links } from 'svelte-routing';
  import { Button, Card, type ColumnDef, ListItem, Table, TextField, tableCell, Icon } from 'svelte-ux';
  import flexLogo from './lib/assets/flex-logo.png';
  import DevContent, { isDev } from './lib/layout/DevContent.svelte';

  type Project = { name: string; crdt: boolean; fwdata: boolean; lexbox: boolean, server: string | null };

  let newProjectName = '';
  let projectsPromise = fetchProjects();

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
    projectsPromise = fetchProjects();
  }

  let importing = '';

  async function importFwDataProject(name: string) {
    importing = name;
    await fetch(`/api/import/fwdata/${name}`, {
      method: 'POST',
    });
    projectsPromise = fetchProjects();
    await projectsPromise;
    importing = '';
  }

  let downloading = '';

  async function downloadCrdtProject(project: Project) {
    downloading = project.name;
    await fetch(`/api/download/crdt/${project.server}/${project.name}`, { method: 'POST' });
    projectsPromise = fetchProjects();
    await projectsPromise;
    downloading = '';
  }

  let uploading = '';
  async function uploadCrdtProject(project: Project) {
    uploading = project.name;
    await fetch(`/api/upload/crdt/${project.server}/${project.name}`, { method: 'POST' });
    projectsPromise = fetchProjects();
    await projectsPromise;
    uploading = '';
  }

  async function fetchProjects() {
    let r = await fetch('/api/projects');
    return (await r.json()) as Promise<Project[]>;
  }

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
            header: 'Lexbox CRDT',
          },
        ]
      : []),
  ] satisfies ColumnDef<Project>[];

  function loggedIn(project: Project) {
    return servers.find(s => s.loggedIn && s.displayName === project.server);
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
          <div>
            {#each servers as server}
              <div class="flex flex-row items-center">
                <p>{server.displayName}</p>
                {#if server.loggedInAs}
                  <p class="m-1 px-1 text-sm border rounded-full">{server.loggedInAs}</p>
                {/if}
                <div class="flex-grow"></div>
                {#if server.loggedIn}
                  <Button slot="actions" variant="fill" href="/api/auth/logout/{server.displayName}">Logout</Button>
                {:else}
                  <Button slot="actions" variant="fill" href="/api/auth/login/{server.displayName}">Login</Button>
                {/if}
              </div>
              <div class="border my-1"/>
            {/each}
          </div>
          {#await projectsPromise}
            <p>loading...</p>
          {:then projects}
            <Table {columns} data={projects.filter((p) => $isDev || p.fwdata).sort((p1, p2) => p1.name.localeCompare(p2.name))} classes={{ th: 'p-4' }}>
              <tbody slot="data" let:columns let:data let:getCellValue let:getCellContent>
                {#each data ?? [] as rowData, rowIndex}
                  <tr class="tabular-nums">
                    {#each columns as column (column.name)}
                      <td use:tableCell={{ column, rowData, rowIndex, tableData: data }} use:links>
                        {#if column.name === 'fwdata'}
                          {#if rowData.fwdata}
                            <Button size="md" href={`/fwdata/${rowData.name}`}>
                              <img src={flexLogo} alt="FieldWorks logo" class="h-6" />
                              Open
                            </Button>
                          {/if}
                        {:else if column.name === 'lexbox'}
                          {#if rowData.lexbox && !rowData.crdt && loggedIn(rowData)}
                            <Button
                              icon={mdiBookArrowDownOutline}
                              size="md"
                              loading={downloading === rowData.name}
                              on:click={() => downloadCrdtProject(rowData)}
                            >
                              Download
                            </Button>
                          {:else if !rowData.lexbox && rowData.crdt && loggedIn(rowData)}
                            <Button
                              icon={mdiBookArrowUpOutline}
                              size="md"
                              loading={uploading === rowData.name}
                              on:click={() => uploadCrdtProject(rowData)}
                            >
                              Upload
                            </Button>
                          {:else if rowData.lexbox && rowData.crdt && loggedIn(rowData)}
                            <Button disabled color="success" icon={mdiBookSyncOutline} size="md">Synced</Button>
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
