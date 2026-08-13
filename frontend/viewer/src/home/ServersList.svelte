<script lang="ts">
  import type {ILexboxServer, IProjectModel, IServerProjects} from '$lib/dotnet-types';
  import {useAuthService, useProjectsService} from '$lib/services/service-provider';
  import Server from './Server.svelte';

  interface Props {
    localProjects: IProjectModel[];
    refreshProjects: () => Promise<void>;
  }

  let { localProjects, refreshProjects }: Props = $props();

  const projectsService = useProjectsService();
  const authService = useAuthService();

  let remoteProjects: { [server: string]: IServerProjects } = $state({});
  let loadingRemoteProjects = $state(false);

  async function fetchRemoteProjects(): Promise<void> {
    loadingRemoteProjects = true;
    try {
      let result = await projectsService.remoteProjects();
      for (let serverProjects of result) {
        remoteProjects[serverProjects.server.id] = serverProjects;
      }
      remoteProjects = remoteProjects;
    } finally {
      loadingRemoteProjects = false;
    }
  }

  let loadingServerProjects: undefined | string = $state(undefined);

  async function refreshServerProjects(server: ILexboxServer, force: boolean = false) {
    loadingServerProjects = server.id;
    remoteProjects[server.id] = await projectsService.serverProjects(server.id, force);
    remoteProjects = remoteProjects;
    loadingServerProjects = undefined;
  }

  fetchRemoteProjects().catch((error) => {
    console.error('Failed to fetch remote projects', error);
    throw error;
  });

  async function refreshProjectsAndServers() {
    await refreshProjects();
    await fetchRemoteProjects();
    serversPromise = authService.servers();
    await serversPromise;
  }


  let serversPromise = $state(authService.servers());

</script>

{#await serversPromise}
  <Server status={undefined} projects={[]} localProjects={[]} loading={true}/>
{:then serversStatus}
  {#each serversStatus as status (status.server.id)}
    {@const server = status.server}
    {@const serverProjects = remoteProjects[server.id]?.projects.filter(p => p.crdt) ?? []}
    {@const canDownloadByCode = remoteProjects[server.id]?.canDownloadByCode}
    <Server {status}
            projects={serverProjects}
            {canDownloadByCode}
            {localProjects}
            loading={loadingServerProjects === server.id || loadingRemoteProjects}
            refreshProjects={() => refreshServerProjects(server, true)}
            refreshAll={() => refreshProjectsAndServers()}/>
  {/each}
{/await}
