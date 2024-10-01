<script lang="ts">
  import {Router, Route, navigate} from 'svelte-routing';
  import CrdtProjectView from './CrdtProjectView.svelte';
  import TestProjectView from './TestProjectView.svelte';
  import FwDataProjectView from './FwDataProjectView.svelte';
  import HomeView from './HomeView.svelte';
  import NotificationOutlet from './lib/notifications/NotificationOutlet.svelte';
  import Sandbox from './lib/sandbox/Sandbox.svelte';
  import { settings } from 'svelte-ux';

  export let url = '';

  settings({
    components: {
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
          content: 'Content',
        }
      }
    },
  });

</script>

<Router {url}>
  <nav>
  </nav>
  <div class="app">
    <Route path="/project/:name" let:params>
      <Router {url} basepath="/project/{params.name}">
        {#key params.name}
          <CrdtProjectView projectName={params.name}/>
        {/key}
      </Router>
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
    <Route path="/sandbox">
      <Sandbox />
    </Route>
    <Route path="/*">
      {setTimeout(() => navigate("/", { replace: true }))}
    </Route>
  </div>
</Router>
<NotificationOutlet/>
