<script lang="ts">
  import { setupGlobalErrorHandlers } from '$lib/errors/global-errors';
  import { navigate, Route, Router, useLocation } from 'svelte-routing';
  import Sandbox from '$lib/sandbox/Sandbox.svelte';
  import DotnetProjectView from './DotnetProjectView.svelte';
  import HomeView from './home/HomeView.svelte';
  import TestProjectView from './TestProjectView.svelte';
  import { initRootLocation } from '$lib/services/root-location-service';

  let url = '';

  setupGlobalErrorHandlers();
  initRootLocation(useLocation());
</script>

<Route path="/project/:code/*" let:params>
  <Router {url}>
    {#key params.code}
      <DotnetProjectView code={params.code} type="crdt" />
    {/key}
  </Router>
</Route>
<Route path="/fwdata/:name/*" let:params>
  <Router {url}>
    {#key params.name}
      <DotnetProjectView code={params.name} type="fwdata" />
    {/key}
  </Router>
</Route>
<Route path="/paratext/project/:code/*" let:params>
  <Router {url}>
    {#key params.code}
      <DotnetProjectView code={params.code} type="crdt" paratext />
    {/key}
  </Router>
</Route>
<Route path="/paratext/fwdata/:name/*" let:params>
  <Router {url}>
    {#key params.name}
      <DotnetProjectView code={params.name} type="fwdata" paratext />
    {/key}
  </Router>
</Route>
<Route path="/testing/project-view/*">
  <Router {url}>
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
