<script module lang="ts">
  import {onMount} from 'svelte';
  import {writable} from 'svelte/store';

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
  import ProjectSidebar from './project/ProjectSidebar.svelte';
  import BrowseView from './project/browse/BrowseView.svelte';
  import TasksView from './project/tasks/TasksView.svelte';
  import {initView, initViewSettings, initViews} from '$lib/views/view-service';
  import DialogsProvider from '$lib/DialogsProvider.svelte';
  import {navigate, Route, useRouter} from 'svelte-routing';
  import ActivityView from '$lib/activity/ActivityView.svelte';
  import {AppNotification} from '$lib/notifications/notifications';
  import type {HTMLAttributes} from 'svelte/elements';
  import {useIdleService} from '$lib/services/idle-service';

  const {
    onloaded,
    ...rest
  }: {
    onloaded: (loaded: boolean) => void;
  } & HTMLAttributes<HTMLDivElement> = $props();

  initViews();
  initView();
  initViewSettings();

  // Load idle service into component context for inputs to use
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const _idleService = useIdleService();

  onMount(() => {
    onloaded(true);
  });
  let open = $state(true);
  const {base} = useRouter();

  type FwLiteEvent = {type: 'notification', message: string};

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  function isFwLiteMessage(data: any): data is FwLiteEvent {
    return typeof data ==='object' && data.type === 'notification' && typeof data.message === 'string' && data.message.length > 0 ;
  }

  //listening to messages from paratext
  function onMessage(event: {data: unknown}) {
    if (isFwLiteMessage(event.data)) {
      AppNotification.display(event.data.message, { timeout: 'long' });
    }
  }
</script>
<svelte:window on:message={onMessage}/>
<DialogsProvider/>
<div class="h-screen flex PortalTarget overflow-hidden shadcn-root" {...rest}>
  <Sidebar.Provider bind:open>
    <ProjectSidebar/>
    <Sidebar.Inset class="flex-1 relative">
      <Route path="/browse">
        <BrowseView/>
      </Route>
      <Route path="/tasks">
        <TasksView/>
      </Route>
      <Route path="/activity">
        <ActivityView />
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
