<script lang="ts">
  import {navigate, Route, Router} from 'svelte-routing';
  import DotnetProjectView from './DotnetProjectView.svelte';
  import TestProjectView from './TestProjectView.svelte';
  import HomeView from './home/HomeView.svelte';
  import Sandbox from './lib/sandbox/Sandbox.svelte';

  let url = '';
</script>

<Route path="/project/:code/*" let:params>
  <Router {url} basepath="/project/{params.code}">
    {#key params.code}
      <DotnetProjectView code={params.code} type="crdt" />
    {/key}
  </Router>
</Route>
<Route path="/fwdata/:name/*" let:params>
  <Router {url} basepath="/fwdata/{params.name}">
    {#key params.name}
      <DotnetProjectView code={params.name} type="fwdata" />
    {/key}
  </Router>
</Route>
<Route path="/testing/project-view/*">
  <Router {url} basepath="/testing/project-view">
    <TestProjectView />
  </Router>
</Route>
<Route path="/">
  <HomeView />
</Route>
<Route path="/sandbox">
  <Sandbox />
</Route>
<Route path="/*">
  {setTimeout(() => navigate('/', { replace: true }))}
</Route>
