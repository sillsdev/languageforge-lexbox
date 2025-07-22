<script lang="ts">
  import {DownloadProjectByCodeResult} from '$lib/dotnet-types/generated-types/FwLiteShared/Projects/DownloadProjectByCodeResult';
  import type {IServerStatus} from '$lib/dotnet-types';
  import type {Project} from '$lib/services/projects-service';
  import {createEventDispatcher} from 'svelte';
  import {mdiCloud} from '@mdi/js';
  import LoginButton from '$lib/auth/LoginButton.svelte';
  import {ListItem} from 'svelte-ux';
  import ButtonListItem from '$lib/utils/ButtonListItem.svelte';
  import {useProjectsService} from '$lib/services/service-provider';
  import {t} from 'svelte-i18n-lingui';
  import ProjectTitle from './ProjectTitle.svelte';
  import {cn} from '$lib/utils';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {AppNotification} from '$lib/notifications/notifications';
  import GetProjectByCodeDialog from '$lib/admin-dialogs/GetProjectByCodeDialog.svelte';
  // import {ProjectRole} from '$lib/dotnet-types/generated-types/LexCore/Entities/ProjectRole';

  const projectsService = useProjectsService();

  const dispatch = createEventDispatcher<{
    refreshProjects: void,
    refreshAll: void,
  }>();
  export let status: IServerStatus | undefined;
  $: server = status?.server;
  export let projects: Project[];
  export let canDowloadByCode: boolean = false;
  export let localProjects: Project[];
  export let loading: boolean;
  let downloading = '';

  async function downloadCrdtProject(project: Project) {
    if (matchesProject(localProjects, project)) return;

    downloading = project.code;
    try {
      const downloadPromise = projectsService.downloadProject(project);
      AppNotification.promise(downloadPromise, {
        loading: $t`Downloading ${project.name}...`,
        success: () => $t`Downloaded ${project.name}`,
        error: () => $t`Failed to download ${project.name}`,
        timeout: 'short',
      });
      await downloadPromise;
      dispatch('refreshAll');
      // Getting an updated list of localProjects will take a moment. For the time being, we do it manually.
      localProjects.push(project);
    } finally {
      downloading = '';
    }
  }

  async function downloadCrdtProjectByCode(projectCode: string): Promise<string | undefined> {
    // TODO: Pass in desired role as well
    const downloadResult = await projectsService.downloadProjectByCode(projectCode, server!);
    switch(downloadResult)
    {
      case DownloadProjectByCodeResult.Success:
        dispatch('refreshAll');
        return;
      case DownloadProjectByCodeResult.NotCrdtProject:
        return $t`Project ${projectCode} on server ${server?.displayName ?? ''} is not yet set up for FieldWorks Lite`;
      case DownloadProjectByCodeResult.ProjectNotFound:
        return $t`Project ${projectCode} not found on server ${server?.displayName ?? ''}`;
    }
  }

  function matchesProject(projects: Project[], project: Project): Project | undefined {
    if (project.id) {
      return projects.find(p => p.id == project.id && p.server?.id == project.server?.id);
    }
    return undefined;
  }

  let getProjectByCodeDialog: GetProjectByCodeDialog|undefined;
  function getProjectByCode()
  {
    getProjectByCodeDialog?.openDialog();
  }
</script>
<GetProjectByCodeDialog bind:this={getProjectByCodeDialog} onDownloadProject={downloadCrdtProjectByCode}/>
<div>
  <div class="flex flex-row mb-2 items-end mr-2 md:mr-0">
    <div class="sub-title !my-0">
      {#if server}
        {$t`${server.displayName} Server`}
      {:else}
        <div class="h-2 w-28 bg-secondary/50 rounded-full animate-pulse"></div>
      {/if}
    </div>
    <div class="flex-grow"></div>
    {#if status?.loggedIn}
      <Button icon="i-mdi-refresh"
              title={$t`Refresh Projects`}
              disabled={loading}
              class="mr-2"
              variant="ghost"
              size="icon"
              onclick={() => dispatch('refreshProjects')}/>
      <LoginButton {status} on:status={() => dispatch('refreshAll')}/>
    {/if}
  </div>
  <div class={cn('rounded', !projects.length && 'border')}>
    {#if !status || loading}
      <!--override the defaults from App.svelte-->
      <!-- eslint-disable-next-line @typescript-eslint/naming-convention -->
      <ListItem icon={mdiCloud} classes={{icon: 'text-neutral-50/50', root: 'animate-pulse dark:bg-muted/50 bg-muted/80 hover:bg-muted/30 hover:dark:bg-muted'}}>
        <div slot="title" class="h-4 bg-neutral-50/50 rounded-full w-32">
        </div>
        <div slot="actions" class="pointer-events-none">
          <div class="h-4 my-3 bg-neutral-50/50 rounded-full w-20"></div>
        </div>
      </ListItem>
    {:else if !projects.length}
      <p class="text-center md:rounded p-4">
        {#if status.loggedIn}
          <Button class="border border-primary" variant="link" target="_blank" href="{server?.authority}/wheresMyProject">
            {$t`Where are my projects?`}
            <Icon icon="i-mdi-open-in-new" class="size-4" />
          </Button>
        {:else}
          <LoginButton {status} on:status={() => dispatch('refreshAll')}/>
        {/if}
      </p>
      {#if status.loggedIn && canDowloadByCode}
        <Button icon="i-mdi-download"
                title={$t`Download project not listed`}
                disabled={loading}
                class="mr-2"
                variant="ghost"
                size="default"
                onclick={getProjectByCode}
                >{$t`Download project not listed`}</Button>
      {/if}
    {:else}
      <div class="shadow rounded">
        {#each projects as project}
          {@const localProject = matchesProject(localProjects, project)}
          {#if localProject?.crdt}
            <ButtonListItem href={`/project/${project.code}`}>
              <ListItem icon={mdiCloud}
                        classes={{root: 'dark:bg-muted/50 bg-muted/80 hover:bg-muted/30 hover:dark:bg-muted'}}
                        loading={downloading === project.name}>
                <ProjectTitle slot="title" {project}/>
                <div slot="actions" class="pointer-events-none shrink-0">
                  <Button disabled icon="i-mdi-book-sync-outline" variant="ghost" class="p-2">
                    {$t`Synced`}
                  </Button>
                </div>
              </ListItem>
            </ButtonListItem>
          {:else}
            {@const loading = downloading === project.code}
            <ButtonListItem onclick={() => downloadCrdtProject(project)} disabled={!!downloading}>
              <ListItem icon={mdiCloud}
                        classes={{root: cn('dark:bg-muted/50 bg-muted/80 hover:bg-muted/30 hover:dark:bg-muted', loading && 'brightness-50')}}
                        {loading}>
                <ProjectTitle slot="title" {project}/>
                <div slot="actions" class="pointer-events-none shrink-0">
                  <Button icon="i-mdi-book-arrow-down-outline" variant="ghost" class="p-2">
                    {$t`Download`}
                  </Button>
                </div>
              </ListItem>
            </ButtonListItem>
          {/if}
        {/each}
        {#if canDowloadByCode}
        <Button icon="i-mdi-download"
                title={$t`Download project not listed`}
                disabled={loading}
                class="mr-2"
                variant="ghost"
                size="default"
                onclick={getProjectByCode}
                >{$t`Download project not listed`}</Button>
        {/if}
      </div>
      <div class="text-center pt-2">
        <Button variant="link" target="_blank" href="{server?.authority}/wheresMyProject">
          {$t`I don't see my project`}
          <Icon icon="i-mdi-open-in-new" class="size-4" />
        </Button>
      </div>
    {/if}
  </div>
</div>
