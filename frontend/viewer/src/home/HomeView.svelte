<script lang="ts">
  import {
    mdiBookArrowLeftOutline,
    mdiBookEditOutline,
    mdiBookPlusOutline,
    mdiChevronRight,
    mdiDelete,
    mdiTestTube,
  } from '@mdi/js';
  import {AppBar, Button as UxButton, ListItem, TextField} from 'svelte-ux';
  import flexLogo from '$lib/assets/flex-logo.png';
  import logoLight from '$lib/assets/logo-light.svg';
  import logoDark from '$lib/assets/logo-dark.svg';
  import storybookIcon from '../stories/assets/storybook-icon.svg'
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
  import ProjectTitle from './ProjectTitle.svelte';
  import type {IProjectModel} from '$lib/dotnet-types';
  import ThemePicker from '$lib/ThemePicker.svelte';
  import {Button} from '$lib/components/ui/button';
  import {mode} from 'mode-watcher';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {Icon} from '$lib/components/ui/icon';

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
  async function deleteProject(project: IProjectModel) {
    try {
      deletingProject = project.id;
      await projectsService.deleteProject(project.code);
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
  let troubleshootDialog: TroubleshootDialog | undefined;

</script>

<AppBar title={$t`Dictionaries`} class="bg-primary/15 min-h-12 shadow-md justify-between" menuIcon={null}>
  <div slot="title" class="text-lg flex gap-2 items-center">
    <Icon src={mode.current === 'dark' ? logoLight : logoDark} alt={$t`Lexbox logo`} />
    <h3>{$t`Dictionaries`}</h3>
  </div>
  <div slot="actions" class="flex">
    {#if import.meta.env.DEV}
      <Button href="http://localhost:6006/" target="_blank"
          variant="ghost" size="icon" iconProps={{src: storybookIcon, alt: 'Storybook icon'}} />
    {/if}
    <DevContent>
      <Button href="/sandbox" variant="ghost" size="icon" icon="i-mdi-test-tube" />
    </DevContent>
    <LocalizationPicker/>
    <ThemePicker />
    <ResponsiveMenu.Root>
      <ResponsiveMenu.Trigger />
      <ResponsiveMenu.Content>
        <ResponsiveMenu.Item href={fwLiteConfig.feedbackUrl} target="_blank" icon="i-mdi-chat-question">
          {$t`Feedback`}
        </ResponsiveMenu.Item>
        {#if supportsTroubleshooting}
          <ResponsiveMenu.Item
            icon="i-mdi-face-agent"
            onSelect={() => troubleshootDialog?.open()}>
            {$t`Troubleshoot`}
          </ResponsiveMenu.Item>
        {/if}
      </ResponsiveMenu.Content>
    </ResponsiveMenu.Root>
    {#if supportsTroubleshooting}
      <TroubleshootDialog bind:this={troubleshootDialog} />
    {/if}
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
          <div class="flex flex-row items-end">
            <p class="sub-title">{$t`Local`}</p>
            <div class="flex-grow"></div>

            <Button icon="i-mdi-refresh"
                    title={$t`Refresh Projects`}
                    class="mb-2"
                    size="icon"
                    variant="ghost"
                    onclick={() => refreshProjects()}/>
          </div>
          <div class="shadow rounded">
            {#each projects.filter((p) => p.crdt) as project, i (project.id ?? i)}
              {@const server = project.server}
              {@const loading = deletingProject === project.id}
              <ButtonListItem href={`/project/${project.code}`}>
                <ListItem
                  icon={mdiBookEditOutline}
                  subheading={!server ? $t`Local only` : $t`Synced with ${server.displayName}`}
                  {loading}
                  classes={{root: 'dark:bg-muted/50 bg-muted/80 hover:bg-muted/30 hover:dark:bg-muted', subheading: 'text-muted-foreground'}}
                >
                  <ProjectTitle slot="title" {project}/>
                  <div slot="actions" class="shrink-0">
                    {#if $isDev}
                      <UxButton
                        icon={mdiDelete}
                        title={$t`Delete`}
                        class="p-2 hover:bg-primary/20"
                        on:click={(e) => {
                          e.preventDefault();
                          void deleteProject(project);
                        }}
                      />
                    {/if}
                    <UxButton icon={mdiChevronRight} class="p-2 pointer-events-none" />
                  </div>
                </ListItem>
              </ButtonListItem>
            {/each}
            <DevContent>
              <ButtonListItem href={`/testing/project-view`}>
                <ListItem title={$t`Test Project`} icon={mdiTestTube}
                          classes={{root: 'dark:bg-muted/50 bg-muted/80 hover:bg-muted/30 hover:dark:bg-muted'}}>
                  <div slot="actions" class="pointer-events-none shrink-0">
                    <UxButton icon={mdiChevronRight} class="p-2" />
                  </div>
                </ListItem>
              </ButtonListItem>
            </DevContent>
            {#if !projects.some(p => p.name === exampleProjectName) || $isDev}
              <ButtonListItem onclick={() => createExampleProject()} disabled={createProjectLoading}>
                <ListItem
                  title={$t`Create Example Project`}
                  loading={createProjectLoading}
                  classes={{root: 'dark:bg-muted/50 bg-muted/80 hover:bg-muted/30 hover:dark:bg-muted'}}
                >
                  <div slot="actions" class="flex flex-nowrap gap-2">
                    {#if $isDev}
                      <TextField
                        bind:value={customExampleProjectName}
                        placeholder={$t`Project name...`}
                        on:click={(e) => e.stopPropagation()}
                      />
                    {/if}
                    <UxButton icon={mdiBookPlusOutline} class="pointer-events-none p-2"/>
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
            <div class="shadow rounded">
              {#each projects.filter((p) => p.fwdata) as project (project.id ?? project.name)}
                <ButtonListItem href={`/fwdata/${project.code}`}>
                  <ListItem classes={{root: 'dark:bg-muted/50 bg-muted/80 hover:bg-muted/30 hover:dark:bg-muted' }}>
                    <ProjectTitle slot="title" {project}/>
                    <Icon slot="avatar" src={flexLogo} alt={$t`FieldWorks logo`} />
                    <div slot="actions" class="shrink-0">
                      <DevContent invisible>
                        <UxButton
                          loading={importing === project.name}
                          icon={mdiBookArrowLeftOutline}
                          title={$t`Import`}
                          disabled={!!importing}
                          on:click={async (e) => {
                            e.preventDefault();
                            await importFwDataProject(project.name);
                          }}
                          class="hover:bg-primary/20"
                        ></UxButton>
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
      @apply text-sm text-muted-foreground;
    }
  }
</style>
