<script module lang="ts">
  import {onMount} from 'svelte';
  import { writable } from 'svelte/store';

  export let useShadcn = writable(false);

  globalThis.enableShadcn = (enable = true) => {
    useShadcn.set(enable);
    // eslint-disable-next-line @typescript-eslint/no-unused-expressions
    enable ? localStorage.setItem('shadcnMode', 'true') : localStorage.removeItem('shadcnMode');
  };
  useShadcn.set(localStorage.getItem('shadcnMode') === 'true');
</script>

<script lang="ts">
  import * as Sidebar from '$lib/components/ui/sidebar';
  import ProjectSidebar, {type View} from './project/ProjectSidebar.svelte';
  import BrowseView from './project/browse/BrowseView.svelte';
  import TasksView from './project/tasks/TasksView.svelte';
  import {initView, initViewSettings} from '$lib/views/view-service';
  import DialogsProvider from '$lib/DialogsProvider.svelte';
  import {navigate, Route, useRouter} from 'svelte-routing';
  import {watch} from 'runed';

  const {
    onloaded,
    // isConnected,
    // showHomeButton = true,
    // about = undefined,
  }: {
    onloaded: (loaded: boolean) => void;
    about?: string | undefined;
    isConnected: boolean;
    showHomeButton?: boolean;
  } = $props();

  const fieldView = initView();
  const viewSettings = initViewSettings();

  onMount(() => {
    onloaded(true);
  });
  let open = $state(true);
  const {base} = useRouter();
</script>
<DialogsProvider/>
<div class="h-screen flex PortalTarget overflow-hidden shadcn-root">
  <Sidebar.Provider bind:open>
    <ProjectSidebar/>
    <Sidebar.Inset class="flex-1 relative">
      <Route path="/browse">
        <BrowseView/>
      </Route>
      <Route path="/tasks">
        <TasksView/>
      </Route>
      <Route path="/">
        {setTimeout(() => navigate(`${$base.uri}/browse`, {replace: true}))}
      </Route>
      <Route path="/*">
        Unknown route
      </Route>
    </Sidebar.Inset>
  </Sidebar.Provider>
</div>
