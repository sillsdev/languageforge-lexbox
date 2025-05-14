<script lang="ts" module>
  import {navigate} from 'svelte-routing';
  declare global {
    interface Window {
      svelteNavigate: (to: string) => void;
    }
  }
  window.svelteNavigate =  (to: string) => {
   navigate(to, {replace: true});
  };
</script>
<script lang="ts">
  import {Router, Route} from 'svelte-routing';
  import TestProjectView from './TestProjectView.svelte';
  import HomeView from './home/HomeView.svelte';
  import NotificationOutlet from './lib/notifications/NotificationOutlet.svelte';
  import Sandbox from './lib/sandbox/Sandbox.svelte';
  import {settings} from 'svelte-ux';
  import DotnetProjectView from './DotnetProjectView.svelte';
  import {setupGlobalErrorHandlers} from '$lib/errors/global-errors';
  import {ModeWatcher, mode, theme} from 'mode-watcher';
  import {initScottyPortalContext} from '$lib/layout/Scotty.svelte';

  let url = '';

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
          root: 'cursor-pointer overflow-hidden',
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
      },
      Drawer: {
        classes:
          '[&.ResponsiveMenu]:rounded-t-xl [&.ResponsiveMenu]:py-2 [&.ResponsiveMenu]:pb-[env(safe-area-inset-bottom)]',
      },
      Menu: {
        classes: {
          /* these classes prevent a MultiSelectField from having 2 scrollbars when used with `resize` */
          root: 'flex flex-col',
          menu: '[&:has(.actions)]:overflow-hidden',
        }
      }
    },
  });
  /* eslint-enable @typescript-eslint/naming-convention */

  setupGlobalErrorHandlers();
  initScottyPortalContext();

  $effect(() => {
    // we set the mode and theme manually, because blazor scrubbs them off the html tag during/after the initial page load
    document.documentElement.classList.toggle('dark', mode.current === 'dark');
    document.documentElement.setAttribute('data-theme', theme.current ?? '');
  });
</script>

<ModeWatcher />

<div class="app">
  <Router {url}>
    <Route path="/project/:code" let:params>
      <Router {url} basepath="/project/{params.code}">
        {#key params.code}
          <DotnetProjectView code={params.code} type="crdt"/>
        {/key}
      </Router>
    </Route>
    <Route path="/fwdata/:name" let:params>
      <Router {url} basepath="/fwdata/{params.name}">
        {#key params.name}
          <DotnetProjectView code={params.name} type="fwdata"/>
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
  </Router>
  <NotificationOutlet/>
</div>
