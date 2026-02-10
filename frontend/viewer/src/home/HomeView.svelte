<script lang="ts">
  import AppBar from './AppBar.svelte';
  import flexLogo from '$lib/assets/flex-logo.png';
  import logoLight from '$lib/assets/logo-light.svg';
  import logoDark from '$lib/assets/logo-dark.svg';
  import storybookIcon from '../stories/assets/storybook-icon.svg';
  import DevContent, {devModeToggle, isDev} from '$lib/layout/DevContent.svelte';
  import {
    useImportFwdataService,
    useProjectsService,
  } from '$lib/services/service-provider';
  import TroubleshootDialog from '$lib/troubleshoot/TroubleshootDialog.svelte';
  import ServersList from './ServersList.svelte';
  import {t} from 'svelte-i18n-lingui';
  import LocalizationPicker from '$lib/i18n/LocalizationPicker.svelte';
  import type {IProjectModel} from '$lib/dotnet-types';
  import ThemePicker from '$lib/components/ThemePicker.svelte';
  import {Button} from '$lib/components/ui/button';
  import {mode} from 'mode-watcher';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {Icon} from '$lib/components/ui/icon';
  import ProjectListItem from './ProjectListItem.svelte';
  import ListItem from '$lib/components/ListItem.svelte';
  import {Input} from '$lib/components/ui/input';
  import {crossfade} from 'svelte/transition';
  import {cubicOut} from 'svelte/easing';
  import {transitionContext} from './transitions';
  import Anchor from '$lib/components/ui/anchor/anchor.svelte';
  import FeedbackDialog from '$lib/about/FeedbackDialog.svelte';
  import DeleteDialog from '$lib/entry-editor/DeleteDialog.svelte';
  import UpdateDialog from '$lib/updates/UpdateDialog.svelte';
  import {SYNC_DIALOG_QUERY_PARAM} from '../project/SyncDialog.svelte';

  const projectsService = useProjectsService();
  const importFwdataService = useImportFwdataService();
  const exampleProjectName = 'Example-Project';
  const [send, receive] = crossfade({
    duration: 500,
    easing: cubicOut,
  });
  transitionContext.set([send, receive]);
  function dateTimeProjectSuffix(): string {
    return new Date()
      .toISOString()
      .replace(/[^0-9]+/g, '-')
      .replace(/-$/, '');
  }

  let customExampleProjectName = $state('');

  let createProjectLoading = $state(false);

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

  let deletingProject = $state<string>();

  async function deleteProject(project: IProjectModel) {
    if (!deleteDialog) throw new Error('Delete dialog not initialized');
    try {
      const syncDialogUrl = `/project/${project.code}/browse?${SYNC_DIALOG_QUERY_PARAM}=true`;
      if (!await deleteDialog.prompt($t`Project`, $t`${project.name}`, {
        isDangerous: true,
        details: $t`Make sure your [changes are synced](${syncDialogUrl}) to Lexbox.`,
      })) return;
      deletingProject = project.id;
      await projectsService.deleteProject(project.code);
      await refreshProjects();
    } finally {
      deletingProject = undefined;
    }
  }

  let importing = $state('');

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

  let projectsPromise = $state(projectsService
    .localProjects()
    .then((projects) => projects.sort((p1, p2) => p1.name.localeCompare(p2.name))));

  async function refreshProjects() {
    let promise = projectsService.localProjects().then((p) => p.sort((p1, p2) => p1.name.localeCompare(p2.name)));
    await promise; //avoids clearing out the list until the new list is fetched
    projectsPromise = promise;
  }

  let troubleshootDialog = $state<TroubleshootDialog>();

  let feedbackOpen = $state(false);
  let updateDialogOpen = $state(false);

  let deleteDialog = $state<DeleteDialog>();
</script>

<DeleteDialog bind:this={deleteDialog}/>

<AppBar tabTitle={$t`Dictionaries`}>
  {#snippet title()}
    <div class="text-lg flex gap-2 items-center">
      <Icon {@attach devModeToggle} src={mode.current === 'dark' ? logoLight : logoDark} class="size-8" alt={$t`Lexbox logo`}/>
      <h3>{$t`Dictionaries`}</h3>
    </div>
  {/snippet}

  {#snippet actions()}
    <div class="flex gap-1">
      {#if import.meta.env.DEV}
        <Button href="http://localhost:6006/" target="_blank"
                variant="ghost" size="icon" iconProps={{src: storybookIcon, alt: 'Storybook icon'}}/>
      {/if}
      <DevContent>
        <Button href="/sandbox" variant="ghost" size="icon" icon="i-mdi-test-tube" title="Sandbox"/>
        <Button href="/swagger" variant="ghost" size="icon" icon="i-mdi-api" target="_blank" title="Swagger"/>
      </DevContent>
      <LocalizationPicker/>
      <ThemePicker buttonProps={{variant: 'outline'}}/>
      <ResponsiveMenu.Root>
        <ResponsiveMenu.Trigger/>
        <ResponsiveMenu.Content>
          <ResponsiveMenu.Item onSelect={() => updateDialogOpen = true} icon="i-mdi-update">
            {$t`Updates`}
          </ResponsiveMenu.Item>
          <ResponsiveMenu.Item onSelect={() => feedbackOpen = true} icon="i-mdi-message">
            {$t`Feedback & Support`}
          </ResponsiveMenu.Item>
          <ResponsiveMenu.Item
            icon="i-mdi-help-circle"
            onSelect={() => troubleshootDialog?.open()}>
            {$t`Troubleshoot`}
          </ResponsiveMenu.Item>
        </ResponsiveMenu.Content>
      </ResponsiveMenu.Root>
      <UpdateDialog bind:open={updateDialogOpen}/>
      <FeedbackDialog bind:open={feedbackOpen}/>
      <TroubleshootDialog bind:this={troubleshootDialog}/>
    </div>
    {/snippet}
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
          <div>
            {#each projects.filter((p) => p.crdt) as project (project.id ?? project)}
              {@const server = project.server}
              {@const loading = deletingProject === project.id}
              <div out:send={{key: 'project-' + project.code}} in:receive={{key: 'project-' + project.code}}>
                <ResponsiveMenu.Root contextMenu>
                  <ResponsiveMenu.Trigger>
                    {#snippet child({props})}
                      <Anchor {...props} href={`/project/${project.code}`}>
                        <ProjectListItem icon="i-mdi-book-edit-outline"
                                         {project}
                                         {loading}
                                         subtitle={!server ? $t`Local only` : $t`Synced with ${server.displayName}`}
                        >
                          {#snippet actions()}
                            <div class="flex items-center">
                              <Icon icon="i-mdi-chevron-right" class="p-2"/>
                            </div>
                          {/snippet}
                        </ProjectListItem>
                      </Anchor>
                    {/snippet}
                  </ResponsiveMenu.Trigger>
                  <ResponsiveMenu.Content>
                    <ResponsiveMenu.Item icon="i-mdi-bug" onSelect={() => troubleshootDialog?.open(project.code)}>
                      {$t`Troubleshoot`}
                    </ResponsiveMenu.Item>
                    <ResponsiveMenu.Item icon="i-mdi-delete" onSelect={() => void deleteProject(project)}>
                      {$t`Delete`}
                    </ResponsiveMenu.Item>
                  </ResponsiveMenu.Content>
                </ResponsiveMenu.Root>
              </div>
            {/each}
            <DevContent>
              <Anchor href="/testing/project-view">
                <ProjectListItem icon="i-mdi-test-tube" project={{ name: 'Test Project', code: 'Test Project' }}>
                  {#snippet actions()}
                    <Icon icon="i-mdi-chevron-right" class="p-2"/>
                  {/snippet}
                </ProjectListItem>
              </Anchor>
            </DevContent>
            {#if !projects.some(p => p.name === exampleProjectName) || $isDev}
              <ListItem onclick={() => createExampleProject()} loading={createProjectLoading}>
                <span>{$t`Create Example Project`}</span>
                {#snippet actions()}
                  <div class="flex flex-nowrap items-center gap-2">
                    {#if $isDev}
                      <Input
                        bind:value={customExampleProjectName}
                        placeholder={$t`Project name...`}
                        onclick={(e) => e.stopPropagation()}
                        autocapitalize="on"
                      />
                    {/if}
                    <Icon icon="i-mdi-book-plus-outline" class="p-2"/>
                  </div>
                {/snippet}

              </ListItem>
            {/if}
          </div>
        </div>
        <ServersList localProjects={projects} {refreshProjects}/>
        {#if projects.some((p) => p.fwdata)}
          <div>
            <p class="sub-title">{$t`Classic FieldWorks Projects`}</p>
            <div>
              {#each projects.filter((p) => p.fwdata) as project (project.name)}
                <Anchor href={`/fwdata/${project.code}`}>
                  <ProjectListItem {project}>
                    {#snippet icon()}
                      <Icon src={flexLogo} alt={$t`FieldWorks logo`}/>
                    {/snippet}
                    {#snippet actions()}
                      <DevContent invisible>
                        <Button loading={importing === project.name}
                                icon="i-mdi-book-arrow-left-outline"
                                size="icon"
                                variant="ghost"
                                title={$t`Import`}
                                disabled={!!importing}
                                onclick={async (e) => {
                          e.preventDefault();
                          await importFwDataProject(project.name);
                        }} class="hover:bg-primary/20"></Button>
                      </DevContent>
                    {/snippet}
                  </ProjectListItem>
                </Anchor>
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

    :global(.sub-title) {
      @apply m-2;
      @apply text-sm text-muted-foreground;
    }
  }
</style>
