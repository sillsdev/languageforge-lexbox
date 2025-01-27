<script lang="ts" context="module">
  import {navigate} from 'svelte-routing';
  declare global {
    interface Window {
      svelteNavigate: (to: string) => void;
    }
  }
  window.svelteNavigate =  (to: string) => {
   console.log('svelteNavigate', to);
   navigate(to, {replace: true});
  };
</script>
<script lang="ts">
  import {Router, Route} from 'svelte-routing';
  import TestProjectView from './TestProjectView.svelte';
  import HomeView from './HomeView.svelte';
  import NotificationOutlet from './lib/notifications/NotificationOutlet.svelte';
  import Sandbox from './lib/sandbox/Sandbox.svelte';
  import {settings} from 'svelte-ux';
  import DotnetProjectView from './DotnetProjectView.svelte';
  import {setupGlobalErrorHandlers} from '$lib/errors/global-errors';

  export let url = '';

  /* eslint-disable @typescript-eslint/naming-convention */
  settings({
    components: {
      AppBar: {
        classes: {
          root: 'max-md:px-1',
        },
      },
      MenuItem: {
        classes: {
          root: 'justify-end',
        },
      },
      ListItem: {
        classes: {
          root: 'cursor-pointer hover:bg-surface-300 hover:border-surface-300 overflow-hidden',
          subheading: 'whitespace-nowrap overflow-hidden overflow-x-clip text-ellipsis',
        }
      },
      ExpansionPanel: {
        classes: {
          toggle: 'p-0',
        },
      },
      Collapse: {
        classes: {
          content: 'CollapseContent',
          icon: 'CollapseIcon',
        }
      }
    },
  });
  /* eslint-enable @typescript-eslint/naming-convention */

  setupGlobalErrorHandlers();
</script>

<Router {url}>
  <nav>
  </nav>
  <div class="app">
    <Route path="/project/:name" let:params>
      <Router {url} basepath="/project/{params.name}">
        {#key params.name}
          <DotnetProjectView projectName={params.name} type="crdt"/>
        {/key}
      </Router>
    </Route>
    <Route path="/fwdata/:name" let:params>
      <Router {url} basepath="/fwdata/{params.name}">
        {#key params.name}
          <DotnetProjectView projectName={params.name} type="fwdata"/>
        {/key}
      </Router>
    </Route>
    <Route path="/testing/project-view">
      <Router {url} basepath="/testing/project-view">
        <TestProjectView/>
      </Router>
    </Route>
    <Route path="/">
      <HomeView/>
    </Route>
    <Route path="/sandbox">
      <Sandbox/>
    </Route>
    <Route path="/*">
      {setTimeout(() => navigate('/', {replace: true}))}
    </Route>
  </div>
</Router>
<NotificationOutlet/>
