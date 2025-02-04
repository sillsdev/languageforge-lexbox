<script lang="ts">
  import {
    mdiBookArrowLeftOutline,
    mdiBookEditOutline,
    mdiBookPlusOutline,
    mdiChatQuestion,
    mdiChevronRight,
    mdiFaceAgent,
    mdiTestTube,
  } from '@mdi/js';
  import {AppBar, Button, ListItem} from 'svelte-ux';
  import flexLogo from '$lib/assets/flex-logo.png';
  import logoLight from '$lib/assets/logo-light.svg';
  import logoDark from '$lib/assets/logo-dark.svg';
  import DevContent, {isDev} from '$lib/layout/DevContent.svelte';
  import {
    useFwLiteConfig,
    useImportFwdataService,
    useProjectsService, useTroubleshootingService
  } from '$lib/services/service-provider';
  import AnchorListItem from '$lib/utils/AnchorListItem.svelte';
  import TroubleshootDialog from '$lib/troubleshoot/TroubleshootDialog.svelte';
  import ServersList from './ServersList.svelte';

  const projectsService = useProjectsService();
  const importFwdataService = useImportFwdataService();
  const fwLiteConfig = useFwLiteConfig();
  const exampleProjectName = 'Example-Project';

  function dateTimeProjectSuffix(): string {
    return new Date().toISOString()
      .replace(/[^0-9]+/g, '-')
      .replace(/-$/, '');
  }

  let createProjectLoading = false;
  async function createProject(projectName: string) {
    try {
      createProjectLoading = true;
      if ($isDev) projectName += `-dev-${dateTimeProjectSuffix()}`;
      await projectsService.createProject(projectName);
      await refreshProjects();
    } finally {
      createProjectLoading = false;
    }
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


  let projectsPromise = projectsService.localProjects().then(projects => projects.sort((p1, p2) => p1.name.localeCompare(p2.name)));

  async function refreshProjects() {
    let promise = projectsService.localProjects().then(p => p.sort((p1, p2) => p1.name.localeCompare(p2.name)));
    await promise;//avoids clearing out the list until the new list is fetched
    projectsPromise = promise;
  }


  const supportsTroubleshooting = useTroubleshootingService();
  let showTroubleshooting = false;
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
  <div slot="actions" class="flex gap-2">
    <Button
      href={fwLiteConfig.feedbackUrl}
      target="_blank"
      size="sm"
      variant="outline"
      icon={mdiChatQuestion}>
        Feedback
    </Button>
    {#if supportsTroubleshooting}
      <Button size="sm" variant="outline" icon={mdiFaceAgent} title="Troubleshoot" iconOnly={false}
              on:click={() => showTroubleshooting = !showTroubleshooting}>
      </Button>
      <TroubleshootDialog bind:open={showTroubleshooting}/>
    {/if}
    <DevContent>
      <Button
        href="/sandbox"
        size="sm"
        variant="outline"
        icon={mdiTestTube}>
        Sandbox
      </Button>
    </DevContent>
  </div>
</AppBar>
<div class="mx-auto md:w-full md:py-4 max-w-2xl">
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
              {@const server = project.server}
              <AnchorListItem href={`/project/${project.name}`}>
                <ListItem title={project.name}
                          icon={mdiBookEditOutline}
                          subheading={!server ? 'Local only' : ('Synced with ' + server.displayName)}>
                  <div slot="actions" class="pointer-events-none">
                    <Button icon={mdiChevronRight} class="p-2"/>
                  </div>
                </ListItem>
              </AnchorListItem>
            {/each}
            <DevContent>
              <AnchorListItem href={`/testing/project-view`}>
                <ListItem title="Test Project" icon={mdiTestTube}>
                  <div slot="actions" class="pointer-events-none">
                    <Button icon={mdiChevronRight} class="p-2"/>
                  </div>
                </ListItem>
              </AnchorListItem>
            </DevContent>
            {#if !projects.some(p => p.name === exampleProjectName) || $isDev}
              <ListItem title="Create Example Project" on:click={() => createProject(exampleProjectName)} loading={createProjectLoading}>
                <div slot="actions" class="pointer-events-none">
                  <Button icon={mdiBookPlusOutline} class="p-2"/>
                </div>
              </ListItem>
            {/if}
          </div>
        </div>
        <ServersList localProjects={projects} {refreshProjects}/>
        {#if projects.some(p => p.fwdata)}
          <div>
            <p class="sub-title">Classic FieldWorks Projects</p>
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
                          on:click={async (e) =>
                          {
                            e.preventDefault();
                            await importFwDataProject(project.name);
                          }}>
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

    :global(:is(.ListItem)) {
      @apply max-md:!rounded-none;
      @apply contrast-[0.95];
    }

    :global(.sub-title) {
      @apply m-2;
      @apply text-surface-content/50 text-sm;
    }
  }
</style>
