﻿<script lang="ts">
  import {
    mdiBookArrowLeftOutline,
    mdiBookEditOutline,
    mdiBookPlusOutline,
    mdiChatQuestion,
    mdiChevronRight,
    mdiDelete,
    mdiFaceAgent,
    mdiRefresh,
    mdiTestTube,
  } from '@mdi/js';
  import {AppBar, Button, ListItem, TextField} from 'svelte-ux';
  import flexLogo from '$lib/assets/flex-logo.png';
  import logoLight from '$lib/assets/logo-light.svg';
  import logoDark from '$lib/assets/logo-dark.svg';
  import DevContent, {isDev} from '$lib/layout/DevContent.svelte';
  import {
    useFwLiteConfig,
    useImportFwdataService,
    useProjectsService,
    useTroubleshootingService,
  } from '$lib/services/service-provider';
  import ButtonListItem from '$lib/utils/ButtonListItem.svelte';
  import TroubleshootDialog from '$lib/troubleshoot/TroubleshootDialog.svelte';
  import ServersList from './ServersList.svelte';
  import {t} from 'svelte-i18n-lingui';
  import LocalizationPicker from '$lib/i18n/LocalizationPicker.svelte';

  const projectsService = useProjectsService();
  const importFwdataService = useImportFwdataService();
  const fwLiteConfig = useFwLiteConfig();
  const exampleProjectName = 'Example-Project';

  function dateTimeProjectSuffix(): string {
    return new Date()
      .toISOString()
      .replace(/[^0-9]+/g, '-')
      .replace(/-$/, '');
  }

  let customExampleProjectName: string = '';

  let createProjectLoading = false;
  async function createExampleProject() {
    try {
      createProjectLoading = true;
      let projectName = exampleProjectName;
      if ($isDev) {
        if (customExampleProjectName) {
          projectName = customExampleProjectName;
        } else {
          projectName += `-dev-${dateTimeProjectSuffix()}`;
        }
      }
      await projectsService.createProject(projectName);
      await refreshProjects();
    } finally {
      createProjectLoading = false;
    }
  }

  let deletingProject: undefined | string = undefined;
  async function deleteProject(projectName: string) {
    try {
      deletingProject = projectName;
      await projectsService.deleteProject(projectName);
      await refreshProjects();
    } finally {
      deletingProject = undefined;
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

  let projectsPromise = projectsService
    .localProjects()
    .then((projects) => projects.sort((p1, p2) => p1.name.localeCompare(p2.name)));

  async function refreshProjects() {
    let promise = projectsService.localProjects().then((p) => p.sort((p1, p2) => p1.name.localeCompare(p2.name)));
    await promise; //avoids clearing out the list until the new list is fetched
    projectsPromise = promise;
  }

  const supportsTroubleshooting = useTroubleshootingService();
  let showTroubleshooting = false;

</script>

<AppBar title={$t`Dictionaries`} class="bg-primary/25 min-h-12 shadow-md justify-between" menuIcon={null}>
  <div slot="title" class="text-lg flex gap-2 items-center">
    <picture>
      <source srcset={logoLight} media="(prefers-color-scheme: dark)" />
      <source srcset={logoDark} media="(prefers-color-scheme: light)" />
      <img src={logoDark} alt={$t`Lexbox logo`} class="h-6" />
    </picture>
    <h3>{$t`Dictionaries`}</h3>
  </div>
  <div slot="actions" class="flex gap-2">
    <Button href={fwLiteConfig.feedbackUrl} target="_blank" size="sm" variant="outline" icon={mdiChatQuestion}>
      {$t`Feedback`}
    </Button>
    {#if supportsTroubleshooting}
      <Button
        size="sm"
        variant="outline"
        icon={mdiFaceAgent}
        title={$t`Troubleshoot`}
        iconOnly={false}
        on:click={() => (showTroubleshooting = !showTroubleshooting)}
      ></Button>
      <TroubleshootDialog bind:open={showTroubleshooting} />
    {/if}
    <DevContent>
      <Button href="/sandbox" size="sm" variant="outline" icon={mdiTestTube}>Sandbox</Button>
    </DevContent>
    <LocalizationPicker/>
  </div>
</AppBar>
<div class="mx-auto md:w-full md:py-4 max-w-2xl">
  <div class="flex-grow hidden md:block"></div>
  <div class="project-list">
    {#await projectsPromise}
      <p>{$t`loading...`}</p>
    {:then projects}
      <div class="space-y-4 md:space-y-8">
        <div>
          <div class="flex flex-row">
            <p class="sub-title">{$t`Local`}</p>
            <div class="flex-grow"></div>
            <Button icon={mdiRefresh} title={$t`Refresh Projects`} on:click={() => refreshProjects()} />
          </div>
          <div>
            {#each projects.filter((p) => p.crdt) as project, i (project.id ?? i)}
              {@const server = project.server}
              {@const loading = deletingProject === project.name}
              <ButtonListItem href={`/project/${project.name}`}>
                <ListItem
                  title={project.name}
                  icon={mdiBookEditOutline}
                  subheading={!server ? $t`Local only` : $t`Synced with ${server.displayName}`}
                  {loading}
                >
                  <div slot="actions">
                    {#if $isDev}
                      <Button
                        icon={mdiDelete}
                        title={$t`Delete`}
                        class="p-2"
                        on:click={(e) => {
                          e.preventDefault();
                          void deleteProject(project.name);
                        }}
                      />
                    {/if}
                    <Button icon={mdiChevronRight} class="p-2 pointer-events-none" />
                  </div>
                </ListItem>
              </ButtonListItem>
            {/each}
            <DevContent>
              <ButtonListItem href={`/testing/project-view`}>
                <ListItem title={$t`Test Project`} icon={mdiTestTube}>
                  <div slot="actions" class="pointer-events-none">
                    <Button icon={mdiChevronRight} class="p-2" />
                  </div>
                </ListItem>
              </ButtonListItem>
            </DevContent>
            {#if !projects.some(p => p.name === exampleProjectName) || $isDev}
              <ButtonListItem on:click={() => createExampleProject()} disabled={createProjectLoading}>
                <ListItem
                  title={$t`Create Example Project`}
                  loading={createProjectLoading}
                >
                  <div slot="actions" class="flex flex-nowrap gap-2">
                    {#if $isDev}
                      <TextField
                        bind:value={customExampleProjectName}
                        placeholder={$t`Project name...`}
                        on:click={(e) => e.stopPropagation()}
                      />
                    {/if}
                    <Button icon={mdiBookPlusOutline} class="pointer-events-none p-2"/>
                  </div>
                </ListItem>
              </ButtonListItem>
            {/if}
          </div>
        </div>
        <ServersList localProjects={projects} {refreshProjects} />
        {#if projects.some((p) => p.fwdata)}
          <div>
            <p class="sub-title">{$t`Classic FieldWorks Projects`}</p>
            <div>
              {#each projects.filter((p) => p.fwdata) as project (project.id ?? project.name)}
                <ButtonListItem href={`/fwdata/${project.name}`}>
                  <ListItem title={project.name}>
                    <img slot="avatar" src={flexLogo} alt={$t`FieldWorks logo`} class="h-6" />
                    <div slot="actions">
                      <DevContent invisible>
                        <Button
                          loading={importing === project.name}
                          icon={mdiBookArrowLeftOutline}
                          title={$t`Import`}
                          disabled={!!importing}
                          on:click={async (e) => {
                            e.preventDefault();
                            await importFwDataProject(project.name);
                          }}
                        ></Button>
                      </DevContent>
                    </div>
                  </ListItem>
                </ButtonListItem>
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
