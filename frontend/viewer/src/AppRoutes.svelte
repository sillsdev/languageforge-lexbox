<script lang="ts">
  import {setupGlobalErrorHandlers} from '$lib/errors/global-errors';
  import {navigate, Route, Router, useLocation} from 'svelte-routing';
  import Sandbox from '$lib/sandbox/Sandbox.svelte';
  import DotnetProjectView from './DotnetProjectView.svelte';
  import HomeView from './home/HomeView.svelte';
  import TestProjectView from './TestProjectView.svelte';
  import {initRootLocation} from '$lib/services/root-location-service';
  import {trackUrl} from './url-tracker';
  import {initAppStorage} from '$lib/storage';

  const url = '';

  setupGlobalErrorHandlers();
  const appStorage = initAppStorage();
  trackUrl(appStorage);
  initRootLocation(useLocation());
  type HarmonyProjectParams = { code: string };
  type FwdataParams = { name: string };
</script>

<Route path="/project/:code/*" >
  {#snippet children({ params }: { params: HarmonyProjectParams })}
    <Router {url}>
      {#key params.code}
        <DotnetProjectView code={params.code} type="crdt" />
      {/key}
    </Router>
  {/snippet}
</Route>
<Route path="/fwdata/:name/*" >
  {#snippet children({ params }: { params: FwdataParams })}
    <Router {url}>
      {#key params.name}
        <DotnetProjectView code={params.name} type="fwdata" />
      {/key}
    </Router>
  {/snippet}
</Route>
<Route path="/paratext/project/:code/*" >
  {#snippet children({ params }: { params: HarmonyProjectParams })}
    <Router {url}>
      {#key params.code}
        <DotnetProjectView code={params.code} type="crdt" paratext />
      {/key}
    </Router>
  {/snippet}
</Route>
<Route path="/paratext/fwdata/:name/*" >
  {#snippet children({ params }: { params: FwdataParams })}
    <Router {url}>
      {#key params.name}
        <DotnetProjectView code={params.name} type="fwdata" paratext />
      {/key}
    </Router>
  {/snippet}
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
