<script lang="ts">
  import {Router, Route} from 'svelte-routing';
  import CrdtProjectView from './CrdtProjectView.svelte';
  import TestProjectView from './TestProjectView.svelte';
  import FwDataProjectView from './FwDataProjectView.svelte';
  import HomeView from './HomeView.svelte';

  export let url = '';

</script>

<Router {url}>
  <nav>
  </nav>
  <div class="app">
    <Route path="/project/:name" let:params>
      {#key params.name}
        <CrdtProjectView projectName={params.name}/>
      {/key}
    </Route>
    <Route path="/fwdata/:name" let:params>
      {#key params.name}
        <FwDataProjectView projectName={params.name}/>
      {/key}
    </Route>
    <Route path="/testing/project-view">
      <TestProjectView/>
    </Route>
    <Route path="/">
      <HomeView/>
    </Route>
  </div>
</Router>
