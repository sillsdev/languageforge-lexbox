<script lang="ts">
  import {
    mdiBookArrowDownOutline,
    mdiBookArrowLeftOutline,
    mdiBookEditOutline,
    mdiBookPlusOutline,
    mdiBookSyncOutline, mdiChatQuestion, mdiChevronRight, mdiCloud,
    mdiTestTube,
  } from '@mdi/js';
  import {
    Button,
    ListItem,
    AppBar
  } from 'svelte-ux';
  import flexLogo from './lib/assets/flex-logo.png';
  import logoLight from './lib/assets/logo-light.svg';
  import logoDark from './lib/assets/logo-dark.svg';
  import DevContent, {isDev} from './lib/layout/DevContent.svelte';
  import {type Project} from './lib/services/projects-service';
  import {onMount} from 'svelte';
  import {
    useAuthService,
    useFwLiteConfig,
    useImportFwdataService,
    useProjectsService
  } from './lib/services/service-provider';
  import type {ILexboxServer, IServerStatus} from '$lib/dotnet-types';
  import LoginButton from '$lib/auth/LoginButton.svelte';
  import AnchorListItem from '$lib/utils/AnchorListItem.svelte';

  const projectsService = useProjectsService();
  const authService = useAuthService();
  const importFwdataService = useImportFwdataService();
  const fwLiteConfig = useFwLiteConfig();
  const exampleProjectName = 'Example-Project';

  function dateTimeProjectSuffix(): string {
    return new Date().toISOString()
      .replace(/[^0-9]+/g, '-')
      .replace(/-$/, '');
  }

  async function createProject(projectName: string) {
    if ($isDev) projectName += `-dev-${dateTimeProjectSuffix()}`;
    await projectsService.createProject(projectName);
    await refreshProjects();
  }

  let importing = '';

  async function importFwDataProject(name: string) {
    if (importing) return;
    importing = name;
    try {
      await importFwdataService.import(name);
      await refreshProjects();
    } finally {
      importing = '';
    }
  }

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

  let projectsPromise = projectsService.localProjects().then(projects => projects.sort((p1, p2) => p1.name.localeCompare(p2.name)));

  async function refreshProjects() {
    let promise = projectsService.localProjects().then(p => p.sort((p1, p2) => p1.name.localeCompare(p2.name)));
    await promise;//avoids clearing out the list until the new list is fetched
    projectsPromise = promise;
  }

  let remoteProjects: { [server: string]: Project[] } = {};
  let loadingRemoteProjects = false;
  async function fetchRemoteProjects(): Promise<void> {
    loadingRemoteProjects = true;
    try {
      let result = await projectsService.remoteProjects();
      for (let serverProjects of result) {
        remoteProjects[serverProjects.server.authority] = serverProjects.projects;
      }
      remoteProjects = remoteProjects;
    } finally {
      loadingRemoteProjects = false;
    }
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
    let matches: Project | undefined = undefined;
    if (project.id) {
      matches = projects.find(p => p.id == project.id && p.serverAuthority == project.serverAuthority);
    }
    return matches;
  }

  function syncedServer(serversProjects: { [server: string]: Project[] }, project: Project, serversStatus: IServerStatus[]): ILexboxServer | undefined {
    //this may be null, even if the project is synced, when the project info isn't cached on the server yet.
    if (project.serverAuthority) {
      return serversStatus.find(s => s.server.id == project.serverAuthority)?.server ?? {
        displayName: 'Unknown server ' + project.serverAuthority,
        authority: project.serverAuthority,
        id: project.serverAuthority
      } satisfies ILexboxServer;
    }
    let authority =  Object.entries(serversProjects)
      .find(([_server, projects]) => matchesProject(projects, project))?.[0];
    return authority ? serversStatus.find(s => s.server.authority == authority)?.server : undefined;
  }
</script>
<AppBar title="Projects" class="bg-secondary min-h-12 shadow-md justify-between" menuIcon={null}>
  <div slot="title" class="text-lg flex gap-2 items-center">
    <picture>
      <source srcset={logoLight} media="(prefers-color-scheme: dark)">
      <source srcset={logoDark} media="(prefers-color-scheme: light)">
      <img src={logoDark} alt="Lexbox logo" class="h-6">
    </picture>
    <h3>Projects</h3>
  </div>
  <div slot="actions">
    <Button
      href={fwLiteConfig.feedbackUrl}
      target="_blank"
      size="sm"
      variant="outline"
      icon={mdiChatQuestion}>
        Feedback
    </Button>
  </div>
</AppBar>
<div class="contents md:flex md:flex-col md:w-3/4 lg:w-2/4 md:mx-auto md:py-4 md:min-h-full">
  <div class="flex-grow hidden md:block"></div>
  <div class="project-list">
    {#await projectsPromise}
      <p>loading...</p>
    {:then projects}
      <div class="space-y-4 md:space-y-8">
        <div>
          <p class="sub-title">Local</p>
          <div>
            {#each projects.filter(p => p.crdt) as project, i (project.id ?? i)}
              {@const server = syncedServer(remoteProjects, project, serversStatus)}
              <AnchorListItem href={`/project/${project.name}`}>
                <ListItem title={project.name}
                          icon={mdiBookEditOutline}
                          subheading={!server ? 'Local only' : ('Syncing with ' + server.displayName)}>
                  <div slot="actions" class="pointer-events-none">
                    <Button icon={mdiChevronRight} class="p-2"/>
                  </div>
                </ListItem>
              </AnchorListItem>
            {/each}
            <AnchorListItem href={`/testing/project-view`}>
              <ListItem title="Test Project" icon={mdiTestTube}>
                <div slot="actions" class="pointer-events-none">
                  <Button icon={mdiChevronRight} class="p-2"/>
                </div>
              </ListItem>
            </AnchorListItem>
            {#if !projects.some(p => p.name === exampleProjectName) || $isDev}
              <ListItem title="Create Example Project" on:click={() => createProject(exampleProjectName)}>
                <div slot="actions" class="pointer-events-none">
                  <Button icon={mdiBookPlusOutline} class="p-2"/>
                </div>
              </ListItem>
            {/if}
          </div>
        </div>
        {#each serversStatus as status}
          {@const server = status.server}
          {@const serverProjects = remoteProjects[server.authority]?.filter(p => p.crdt) ?? []}
          <div>
            <div class="flex flex-row items-center mr-2 mb-2">
              <p class="sub-title">{server.displayName} Server</p>
              <div class="flex-grow"></div>
              <LoginButton {status} on:status={() => refreshProjectsAndServers()}/>
            </div>
            <div>
              {#if !serverProjects.length}
                <p class="pl-2 text-surface-content/50 text-center">
                  {#if status.loggedIn}
                    No projects
                  {:else}
                    Login to see projects
                  {/if}
                </p>
              {/if}
              {#each serverProjects as project}
                {@const localProject = matchesProject(projects, project)}
                <ListItem icon={mdiCloud}
                          class={localProject?.crdt ? 'pointer-events-none' : ''}
                          title={project.name}
                          on:click={() =>{ if (!localProject?.crdt) {void downloadCrdtProject(project, server);} }}
                          loading={downloading === project.name}>
                  <div slot="actions" class="pointer-events-none">
                    <Button disabled={localProject?.crdt} icon={localProject?.crdt ? mdiBookSyncOutline : mdiBookArrowDownOutline} class="p-2">
                      {localProject?.crdt ? 'Synced' : 'Download'}
                    </Button>
                  </div>
                </ListItem>
              {/each}
            </div>
          </div>
        {/each}

        {#if projects.some(p => p.fwdata)}
          <div>
            <p class="sub-title">Legacy FieldWorks Projects</p>
            <div>
              {#each projects.filter(p => p.fwdata) as project (project.id ?? project.name)}
                <AnchorListItem href={`/fwdata/${project.name}`}>
                  <ListItem title={project.name}>
                    <img slot="avatar" src={flexLogo} alt="FieldWorks logo" class="h-6"/>
                    <div slot="actions">
                      <DevContent invisible>
                        <Button
                          loading={importing === project.name}
                          icon={mdiBookArrowLeftOutline}
                          title="Import"
                          disabled={!!importing}
                          on:click={() => importFwDataProject(project.name)}>
                        </Button>
                      </DevContent>
                    </div>
                  </ListItem>
                </AnchorListItem>
              {/each}
            </div>
          </div>
        {/if}
      </div>
    {:catch error}
      <p>Error: {error.message}</p>
    {/await}
  </div>

  <div class="md:flex-grow-[2]"></div>
</div>

<style lang="postcss">
  .project-list {
    display: flex;
    flex-direction: column;

    :global(:is(.ListItem, .anchor-list-item)) {
      @apply max-sm:!rounded-none;
    }
  }
  .sub-title {
    @apply m-2;
    @apply text-surface-content/50 text-sm;
  }
</style>
