<script lang="ts">
  import type {ILexboxServer} from '$lib/dotnet-types';
  import {type Project} from '$lib/services/projects-service';
  import {useAuthService, useProjectsService} from '$lib/services/service-provider';
  import Server from './Server.svelte';

  export let localProjects: Project[];
  export let refreshProjects: () => Promise<void>;

  const projectsService = useProjectsService();
  const authService = useAuthService();

  let remoteProjects: { [server: string]: Project[] } = {};
  let loadingRemoteProjects = false;

  async function fetchRemoteProjects(): Promise<void> {
    loadingRemoteProjects = true;
    try {
      let result = await projectsService.remoteProjects();
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
    await refreshProjects();
    await fetchRemoteProjects();
    serversPromise = authService.servers();
    await serversPromise;
  }


  let serversPromise = authService.servers();

</script>

{#await serversPromise}
  <Server status={undefined} projects={[]} localProjects={[]} loading={true}/>
{:then serversStatus}
  {#each serversStatus as status (status.server.id)}
    {@const server = status.server}
    {@const serverProjects = remoteProjects[server.id]?.filter(p => p.crdt) ?? []}
    <Server {status}
            projects={serverProjects}
            {localProjects}
            loading={loadingServerProjects === server.id || loadingRemoteProjects}
            on:refreshProjects={() => refreshServerProjects(server, true)}
            on:refreshAll={() => refreshProjectsAndServers()}/>
  {/each}
{/await}
