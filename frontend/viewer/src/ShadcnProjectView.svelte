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
  import * as Resizable from '$lib/components/ui/resizable';
  import ProjectSidebar, {type View} from '$lib/ProjectSidebar.svelte';
  import BrowseView from './browse/BrowseView.svelte';
  import TasksView from './tasks/TasksView.svelte';

  const {
    onloaded,
    projectName,
    // isConnected,
    // showHomeButton = true,
    // about = undefined,
  }: {
    onloaded: (loaded: boolean) => void;
    about?: string | undefined;
    projectName: string;
    isConnected: boolean;
    showHomeButton?: boolean;
  } = $props();
  let currentView: View = $state('browse');

  onMount(() => {
    onloaded(true);
  });

</script>

<div class="h-screen flex">
  <Sidebar.Provider style="--sidebar-width: 100%;">
    <Resizable.PaneGroup direction="horizontal">
      <ProjectSidebar {projectName} bind:currentView />
      <Resizable.Pane defaultSize={85}>
        <Sidebar.Inset>
          <div class="p-4">
            {#if currentView === 'browse'}
              <BrowseView />
            {:else if currentView === 'tasks'}
              <TasksView />
            {/if}
          </div>
        </Sidebar.Inset>
      </Resizable.Pane>
    </Resizable.PaneGroup>
  </Sidebar.Provider>
</div>
