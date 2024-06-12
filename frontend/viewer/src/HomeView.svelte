<script lang="ts">
  import {
    mdiBookArrowDownOutline,
    mdiBookArrowLeftOutline,
    mdiBookArrowUpOutline,
    mdiBookEditOutline,
    mdiBookPlusOutline,
    mdiBookSyncOutline,
    mdiTestTube
  } from '@mdi/js';
  import {navigate} from 'svelte-routing';
  import {Button, Card, type ColumnDef, ListItem, Table, TextField, tableCell, Icon} from 'svelte-ux';
  import flexLogo from './lib/assets/flex-logo.png';

  type Project = { name: string, crdt: boolean, fwdata: boolean, lexbox: boolean };

  let newProjectName = '';
  let projectsPromise = fetchProjects();

  let createError: string;
  async function createProject() {
    createError = '';

    if (!newProjectName) {
      createError = 'Project name is required.'
      return;
    }
    const response = await fetch(`/api/project?name=${newProjectName}`, {
      method: 'POST'
    });

    if (!response.ok) {
      createError = await response.json();
      return;
    }

    createError = '';
    newProjectName = '';
    projectsPromise = fetchProjects();
  }

  let loading = '';

  async function importFwDataProject(name: string) {
    loading = name;
    await fetch(`/api/import/fwdata/${name}`, {
      method: 'POST'
    });
    projectsPromise = fetchProjects();
    await projectsPromise;
    loading = '';
  }

  let downloading = '';

  async function downloadCrdtProject(name: string) {
    downloading = name;
    await fetch(`/api/download/crdt/${name}`, {method: 'POST'});
    projectsPromise = fetchProjects();
    await projectsPromise;
    downloading = '';
  }

  let uploading = '';
  async function uploadCrdtProject(name: string) {
    uploading = name;
    await fetch(`/api/upload/crdt/${name}`, {method: 'POST'});
    projectsPromise = fetchProjects();
    await projectsPromise;
    uploading = '';
  }

  async function fetchProjects() {
    let r = await fetch('/api/projects');
    return await r.json() as Promise<Project[]>;
  }

  let username = '';
  $: loggedIn = !!username;
  async function fetchMe() {
    let r = await fetch('/api/auth/me');
    let user: any = await r.json();
    username = user.name;
  }

  fetchMe();


  $: columns = [
    {
      name: 'name'
    },
    {
      name: 'fwdata',
      header: 'FieldWorks'
    },
    {
      name: 'crdt',
      header: 'CRDT'
    },
    ...(loggedIn ? [{
      name: 'lexbox',
      header: 'Lexbox CRDT',
    }] : []),
  ] satisfies ColumnDef<Project>[];
</script>
<style>
  .home {
    display: grid;
    grid-template-columns: auto;
  }

  .home :global(.column-name), .home :global(.column-crdt), .home :global(.column-fwdata), .home :global(.column-lexbox) {
    padding: 0.5rem;
  }
</style>
<div class="home">
  <div>
    <Card title="Create Project" class="w-fit m-4">
      <TextField label="New Project Name" class="m-4" placeholder="Project Name" bind:value={newProjectName} error={createError} />
      <Button slot="actions" variant="fill" icon={mdiBookPlusOutline} on:click={createProject}>Create Project</Button>
    </Card>
    <Card title="Account" class="w-fit m-4">
      {#if loggedIn}
        <p>{username}</p>
        <Button slot="actions" variant="fill" href="/api/auth/logout/default">Logout</Button>
      {:else}
        <Button slot="actions" variant="fill" href="/api/auth/login/default">Login</Button>
      {/if}
    </Card>

  </div>
  <Card title="Projects" class="w-fit m-4">
    <div slot="contents">
      {#await projectsPromise}
        <p>loading...</p>
      {:then projects}
        <Table columns={columns} data={projects} classes={{th: 'p-4'}}>

          <tbody slot="data" let:columns let:data let:getCellValue let:getCellContent>
          {#each data ?? [] as rowData, rowIndex}
            <tr class="tabular-nums">
              {#each columns as column (column.name)}
                {@const value = getCellValue(column, rowData, rowIndex)}

                <td use:tableCell={{ column, rowData, rowIndex, tableData: data }}>
                  {#if column.name === "fwdata"}
                    {#if rowData.fwdata}
                      <Button
                        size="md"
                        on:click={() => navigate(`/fwdata/${rowData.name}`)}>
                        <img src={flexLogo} alt="FieldWorks logo" class="h-6">
                        Edit
                      </Button>
                    {/if}
                  {:else if column.name === "lexbox"}
                    {#if rowData.lexbox && !rowData.crdt}
                      <Button
                        icon={mdiBookArrowDownOutline}
                        size="md"
                        loading={downloading === rowData.name}
                        on:click={() => downloadCrdtProject(rowData.name)}>
                        Download
                      </Button>
                    {:else if !rowData.lexbox && rowData.crdt && loggedIn}
                      <Button
                        icon={mdiBookArrowUpOutline}
                        size="md"
                        loading={uploading === rowData.name}
                        on:click={() => uploadCrdtProject(rowData.name)}>
                        Upload
                      </Button>
                    {:else if rowData.lexbox && rowData.crdt}
                      <Button
                        disabled
                        color="success"
                        icon={mdiBookSyncOutline}
                        size="md">
                        Synced
                      </Button>
                    {/if}
                  {:else if column.name === "crdt"}
                    {#if rowData.crdt}
                      <Button
                        icon={mdiBookEditOutline}
                        size="md"
                        on:click={() => navigate(`/project/${rowData.name}`)}>
                        Edit
                      </Button>
                    {:else if rowData.fwdata}
                      <Button
                        size="md"
                        icon={mdiBookArrowLeftOutline}
                        on:click={() => importFwDataProject(rowData.name)}>
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
          </tbody>
        </Table>
      {/await}

      <ListItem
        class="cursor-pointer hover:bg-primary/5"
        noShadow
        icon={mdiTestTube}
        title="Test Project"
        on:click={() => navigate('/testing/project-view')}/>
    </div>
  </Card>

</div>
