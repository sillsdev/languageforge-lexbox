<script lang="ts">
  import {Router, Link, Route} from 'svelte-routing';
  import CrdtProjectView from './CrdtProjectView.svelte';

  let projectsPromise = fetchProjects();
  export let url = '';

  let newProjectName = '';

  function createProject() {
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
</script>

<Router {url}>
  <nav>
    <ul>
      <li>
        <Link to="/">Home</Link>
      </li>
      {#await projectsPromise}
        <p>loading...</p>
      {:then projects}
        {#each projects as project}
          <li>
            <Link to={`/project/${project.name}`}>{project.name}</Link>
          </li>
        {/each}
      {/await}
    </ul>
  </nav>
  <div>
    <Route path="/project/:name" let:params>
      {#key params.name}
        <CrdtProjectView projectName={params.name}/>
      {/key}
    </Route>
    <Route path="/">
      <input bind:value={newProjectName}/>
      <button on:click={createProject}>Create Project</button>
    </Route>
  </div>
</Router>
