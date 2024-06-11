<script lang="ts">
  import {
    mdiBookArrowDownOutline,
    mdiBookArrowUpOutline,
    mdiBookEditOutline,
    mdiBookPlusOutline,
    mdiBookSyncOutline,
    mdiCubeOutline,
    mdiMicrosoftXbox,
    mdiTestTube
  } from '@mdi/js';
  import {navigate} from 'svelte-routing';
  import {Button, Card, type ColumnDef, ListItem, Table, TextField, tableCell, Icon} from 'svelte-ux';

  type Project = { name: string, crdt: boolean, fwdata: boolean, lexbox: boolean };

  let newProjectName = '';
  let projectsPromise = fetchProjects();

  function createProject() {
    if (!newProjectName) return;
    fetch(`/api/project?name=${newProjectName}`, {
      method: 'POST'
    }).then(() => {
      newProjectName = '';
      projectsPromise = fetchProjects();
    });
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

  async function importCrdtProject(name: string) {

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


  const columns: ColumnDef<Project>[] = [
    {
      name: 'name'
    },
    {
      name: 'crdt',
      header: 'CRDT'
    },
    {
      name: 'fwdata',
      header: 'FieldWorks'
    },
    {
      name: 'lexbox',
    }
  ];
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
      <TextField label="New Project Name" class="m-4" placeholder="Project Name" bind:value={newProjectName}/>
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
        <Table columns={columns} data={projects}>

          <tbody slot="data" let:columns let:data let:getCellValue let:getCellContent>
          {#each data ?? [] as rowData, rowIndex}
            <tr class="tabular-nums">
              {#each columns as column (column.name)}
                {@const value = getCellValue(column, rowData, rowIndex)}

                <td use:tableCell={{ column, rowData, rowIndex, tableData: data }}>
                  {#if column.name === "fwdata"}
                    {#if rowData.fwdata}
                      <Button
                        icon={mdiCubeOutline}
                        size="sm"
                        on:click={() => navigate(`/fwdata/${rowData.name}`)}>
                        Edit
                      </Button>
                    {/if}
                  {:else if column.name === "lexbox"}
                    {#if rowData.lexbox && !rowData.crdt}
                      <Button
                        icon={mdiBookArrowDownOutline}
                        size="sm"
                        on:click={() => importCrdtProject(rowData.name)}>
                        Download
                      </Button>
                    {:else if !rowData.lexbox && rowData.crdt && loggedIn}
                      <Button
                        icon={mdiBookArrowUpOutline}
                        size="sm"
                        loading={uploading === rowData.name}
                        on:click={() => uploadCrdtProject(rowData.name)}>
                        Upload
                      </Button>
                    {:else if rowData.lexbox && rowData.crdt}
                      <Icon size="1.5em" data={mdiBookSyncOutline}/>
                    {/if}
                  {:else if column.name === "crdt"}
                    {#if rowData.crdt}
                      <Button
                        icon={mdiBookEditOutline}
                        size="sm"
                        on:click={() => navigate(`/project/${rowData.name}`)}>
                        Edit
                      </Button>
                    {:else if rowData.fwdata}
                      <Button
                        size="sm"
                        icon={mdiBookPlusOutline}
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
