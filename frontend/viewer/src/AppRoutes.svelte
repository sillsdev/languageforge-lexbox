<script lang="ts">
  import { setupGlobalErrorHandlers } from '$lib/errors/global-errors';
  import { navigate, Route, Router, useLocation } from 'svelte-routing';
  import Sandbox from '$lib/sandbox/Sandbox.svelte';
  import DotnetProjectView from './DotnetProjectView.svelte';
  import HomeView from './home/HomeView.svelte';
  import TestProjectView from './TestProjectView.svelte';
  import { initRootLocation } from '$lib/services/root-location-service';
  import { useAppStorage } from '$lib/utils/app-storage.svelte';

  let url = '';

  setupGlobalErrorHandlers();
  const location = initRootLocation(useLocation());
  const appStorage = useAppStorage();
  const startedAtDefault = window.location.pathname === '/';

  // Wait for lastUrl to load, restore if we started at '/', then begin tracking.
  // The subscription must not start until after restore, otherwise the immediate
  // subscribe callback would .set('/') and clobber the saved value before load() finishes.
  let hasRestored = false;
  $effect(() => {
    if (hasRestored) return;
    if (appStorage.lastUrl.loading) return;
    hasRestored = true;

    const savedUrl = appStorage.lastUrl.current;
    if (startedAtDefault && savedUrl && savedUrl !== '/') {
      navigate(savedUrl, { replace: true });
    }

    // Now that restore is done, start tracking URL changes for next launch
    location.subscribe(() => {
      const { pathname, search, hash } = document.location;
      void appStorage.lastUrl.set(pathname + search + hash);
    });
  });
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
