<script lang="ts">
  import {TooltipProvider} from '$lib/components/ui/tooltip';
  import {setupGlobalErrorHandlers} from '$lib/errors/global-errors';
  import {ModeWatcher} from 'mode-watcher';
  import {navigate, Route, Router} from 'svelte-routing';
  import NotificationOutlet from './lib/notifications/NotificationOutlet.svelte';
  import Sandbox from '$lib/sandbox/Sandbox.svelte';
  import DotnetProjectView from './DotnetProjectView.svelte';
  import HomeView from './home/HomeView.svelte';
  import TestProjectView from './TestProjectView.svelte';

  let url = '';

  setupGlobalErrorHandlers();
</script>

<ModeWatcher />


<TooltipProvider delayDuration={300}>
  <div class="app">
    <Router {url}>
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
    </Router>
    <NotificationOutlet/>
  </div>
</TooltipProvider>
