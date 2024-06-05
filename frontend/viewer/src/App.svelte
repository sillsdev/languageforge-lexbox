<script lang="ts">
  import {Router, Link, Route, navigate} from 'svelte-routing';
  import CrdtProjectView from './CrdtProjectView.svelte';
  import TestProjectView from './TestProjectView.svelte';
  import {ListItem, Card, TextField, Button} from 'svelte-ux';
  import {mdiCubeOutline, mdiMicrosoftXbox, mdiTestTube} from '@mdi/js';

  let projectsPromise = fetchProjects();
  export let url = '';

  let newProjectName = '';

  function createProject() {
    if (!newProjectName) return;
    fetch(`/api/project?name=${newProjectName}`, {
      method: 'POST'
    }).then(() => {
      newProjectName = '';
      projectsPromise = fetchProjects();
    });
  }

  let loading = ''
  async function importFwDataProject(name: string) {
    loading = name;
    await fetch(`/api/import/fwdata/${name}`, {
      method: 'POST'
    });
    projectsPromise = fetchProjects();
    await projectsPromise;
    loading = '';
  }

  function fetchProjects() {
    return fetch('/api/projects').then(r => r.json() as Promise<{ name: string, origin: 'FieldWorks' | 'CRDT' }[]>);
  }
</script>

<Router {url}>
  <nav>
  </nav>
  <div>
    <Route path="/project/:name" let:params>
      {#key params.name}
        <CrdtProjectView projectName={params.name}/>
      {/key}
    </Route>
    <Route path="/testing/project-view">
      <TestProjectView/>
    </Route>
    <Route path="/">

      <Card title="Create Project" class="w-fit m-4">
        <TextField label="New Project Name" class="m-4" placeholder="Project Name" bind:value={newProjectName}/>
        <Button slot="actions" variant="fill" on:click={createProject}>Create Project</Button>
      </Card>
      <Card title="Projects" class="w-fit m-4">
        <div slot="contents">
          {#await projectsPromise}
            <p>loading...</p>
          {:then projects}
            {#each projects as project}

              {#if project.origin === 'CRDT'}
                <ListItem
                  class="cursor-pointer hover:bg-primary/5"
                  noShadow
                  title={project.name}
                  icon={mdiMicrosoftXbox}
                  on:click={() => navigate(`/project/${project.name}`)}>
                </ListItem>
              {:else if project.origin === 'FieldWorks'}
                <ListItem
                  class="cursor-pointer hover:bg-primary/5"
                  noShadow
                  title={project.name}
                  loading={loading === project.name}
                  icon={mdiCubeOutline}
                  on:click={() => importFwDataProject(project.name)}>
                </ListItem>
              {/if}
            {/each}
            <ListItem
              class="cursor-pointer hover:bg-primary/5"
              noShadow
              icon={mdiTestTube}
              title="Test Project"
            on:click={() => navigate('/testing/project-view')}/>
          {/await}
        </div>
      </Card>
    </Route>
  </div>
</Router>
