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
  import {cn} from '$lib/utils';
  import {initView, initViewSettings} from '$lib/views/view-service';
  import {initDialogService} from '$lib/entry-editor/dialog-service';
  import DeleteDialog from '$lib/entry-editor/DeleteDialog.svelte';
  import {initFeatures} from '$lib/services/feature-service';

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
  const fieldView = initView();
  const viewSettings = initViewSettings();
  let deleteDialog = $state<DeleteDialog | undefined>(undefined);
  const dialogService = initDialogService(() => deleteDialog);
  const features = initFeatures({});

  onMount(() => {
    onloaded(true);
  });
  let open = $state(true);
</script>
<DeleteDialog bind:this={deleteDialog} />
<div class="h-screen flex PortalTarget overflow-hidden shadcn-root">
  <Sidebar.Provider bind:open>
      <ProjectSidebar {projectName} bind:currentView />
      <Sidebar.Inset class="flex-1 relative">
        {#if currentView === 'browse'}
          <BrowseView />
        {:else if currentView === 'tasks'}
          <TasksView />
        {/if}
      </Sidebar.Inset>
  </Sidebar.Provider>
</div>
