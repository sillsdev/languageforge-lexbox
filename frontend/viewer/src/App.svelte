<script lang="ts">
  import {Router, Link, Route, navigate} from 'svelte-routing';
  import CrdtProjectView from './CrdtProjectView.svelte';
  import TestProjectView from './TestProjectView.svelte';
  import {ListItem, Card, TextField, Button} from 'svelte-ux';

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

  function fetchProjects() {
    return fetch('/api/projects').then(r => r.json() as Promise<{ name: string }[]>);
  }

  let username = '';

  function fetchMe() {
    return fetch('/api/auth/me').then(r => r.json()).then(user => {
      username = user.name;
    });
  }

  fetchMe();
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
      <Card title="Account" class="w-fit m-4">
        {#if username}
          <p>Logged in as {username}</p>
        {:else}
          <Button slot="actions" variant="fill" href="/api/auth/login">Login</Button>
        {/if}
      </Card>
      <Card title="Projects" class="w-fit m-4">
        <div slot="contents">
          {#await projectsPromise}
            <p>loading...</p>
          {:then projects}
            {#each projects as project}
              <ListItem
                class="cursor-pointer hover:bg-primary/5"
                noShadow
                title={project.name}
                on:click={() => navigate(`/project/${project.name}`)}>
              </ListItem>
            {/each}
            <ListItem
              class="cursor-pointer hover:bg-primary/5"
              noShadow
              title="Test Project"
            on:click={() => navigate('/testing/project-view')}/>
          {/await}
        </div>
      </Card>
    </Route>
  </div>
</Router>
