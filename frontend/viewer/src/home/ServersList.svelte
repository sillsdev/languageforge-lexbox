<script lang="ts">
  import {
    mdiBookArrowDownOutline,
    mdiBookSyncOutline,
    mdiCloud,
    mdiRefresh,
  } from '@mdi/js';
  import {Button, Icon, ListItem} from 'svelte-ux';
  import LoginButton from '$lib/auth/LoginButton.svelte';
  import AnchorListItem from '$lib/utils/AnchorListItem.svelte';
  import type {ILexboxServer, IServerStatus} from '$lib/dotnet-types';
  import {type Project} from '$lib/services/projects-service';
  import {onMount} from 'svelte';
  import {useAuthService, useProjectsService} from '$lib/services/service-provider';
  export let localProjects: Project[];
  export let refreshProjects: () => Promise<void>;

  const projectsService = useProjectsService();
  const authService = useAuthService();
  let downloading = '';

  async function downloadCrdtProject(project: Project, server: ILexboxServer) {
    downloading = project.name;
    if (project.id == null) throw new Error('Project id is null');
    try {
      await projectsService.downloadProject(project.id, project.name, server);
      await refreshProjects();
    } finally {
      downloading = '';
    }
  }

  let remoteProjects: { [server: string]: Project[] } = {};
  let loadingRemoteProjects = false;

  async function fetchRemoteProjects(force: boolean = false): Promise<void> {
    loadingRemoteProjects = true;
    try {
      let result = await projectsService.remoteProjects(force);
      for (let serverProjects of result) {
        remoteProjects[serverProjects.server.id] = serverProjects.projects;
      }
      remoteProjects = remoteProjects;
    } finally {
      loadingRemoteProjects = false;
    }
  }

  let loadingServerProjects: undefined | string = undefined;

  async function refreshServerProjects(server: ILexboxServer, force: boolean = false) {
    loadingServerProjects = server.id;
    remoteProjects[server.id] = await projectsService.serverProjects(server.id, force);
    remoteProjects = remoteProjects;
    loadingServerProjects = undefined;
  }

  fetchRemoteProjects().catch((error) => {
    console.error(`Failed to fetch remote projects`, error);
    throw error;
  });

  async function refreshProjectsAndServers() {
    await fetchRemoteProjects();
    serversStatus = await authService.servers();
  }


  let serversStatus: IServerStatus[] = [];
  onMount(async () => {
    serversStatus = await authService.servers();
  });

  function matchesProject(projects: Project[], project: Project): Project | undefined {
    if (project.id) {
      return projects.find(p => p.id == project.id && p.server?.id == project.server?.id);
    }
    return undefined;
  }
</script>

{#each serversStatus as status}
  {@const server = status.server}
  {@const serverProjects = remoteProjects[server.id]?.filter(p => p.crdt) ?? []}
  <div>
    <div class="flex flex-row mb-2 items-end mr-2 md:mr-0">
      <p class="sub-title !my-0">
        {server.displayName} Server
      </p>
      <div class="flex-grow"></div>
      {#if status.loggedIn}
        <Button icon={mdiRefresh}
                title="Refresh Projects"
                disabled={loadingServerProjects === server.id}
                on:click={() => refreshServerProjects(server, true)}/>
        <LoginButton {status} on:status={() => refreshProjectsAndServers()}/>
      {/if}
    </div>
    <div>
      {#if !serverProjects.length}
        <p class="text-surface-content/50 text-center elevation-1 md:rounded p-4">
          {#if status.loggedIn}
            No projects
          {:else}
            <LoginButton {status} on:status={() => refreshProjectsAndServers()}/>
          {/if}
        </p>
      {/if}
      {#if loadingServerProjects === server.id}
        <p class="text-surface-content/50 text-center elevation-1 md:rounded p-4">
          <Icon data={mdiRefresh} class="animate-spin"/>
          Loading...
        </p>
      {:else}
        {#each serverProjects as project}
          {@const localProject = matchesProject(localProjects, project)}
          {#if localProject?.crdt}
            <AnchorListItem href={`/project/${project.name}`}>
              <ListItem icon={mdiCloud}
                        title={project.name}
                        loading={downloading === project.name}>
                <div slot="actions" class="pointer-events-none">
                  <Button disabled icon={mdiBookSyncOutline} class="p-2">
                    Synced
                  </Button>
                </div>
              </ListItem>
            </AnchorListItem>
          {:else}
            <ListItem icon={mdiCloud}
                      title={project.name}
                      on:click={() => void downloadCrdtProject(project, server)}
                      loading={downloading === project.name}>
              <div slot="actions" class="pointer-events-none">
                <Button icon={mdiBookArrowDownOutline} class="p-2">
                  Download
                </Button>
              </div>
            </ListItem>
          {/if}
        {/each}
      {/if}
    </div>
  </div>
{/each}
