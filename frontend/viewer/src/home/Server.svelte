﻿<script lang="ts">
  import type {IServerStatus} from '$lib/dotnet-types';
  import type {Project} from '$lib/services/projects-service';
  import {createEventDispatcher} from 'svelte';
  import {mdiBookArrowDownOutline, mdiBookSyncOutline, mdiCloud, mdiRefresh} from '@mdi/js';
  import LoginButton from '$lib/auth/LoginButton.svelte';
  import {Button, ListItem, Settings} from 'svelte-ux';
  import ButtonListItem from '$lib/utils/ButtonListItem.svelte';
  import {useProjectsService} from '$lib/services/service-provider';
  import {t} from 'svelte-i18n-lingui';
  import ProjectTitle from './ProjectTitle.svelte';

  const projectsService = useProjectsService();

  const dispatch = createEventDispatcher<{
    refreshProjects: void,
    refreshAll: void,
  }>();
  export let status: IServerStatus | undefined;
  $: server = status?.server;
  export let projects: Project[];
  export let localProjects: Project[];
  export let loading: boolean;
  let downloading = '';

  async function downloadCrdtProject(project: Project) {
    if (matchesProject(localProjects, project)) return;

    downloading = project.code;
    try {
      await projectsService.downloadProject(project);
      dispatch('refreshAll');
      // Getting an updated list of localProjects will take a moment. For the time being, we do it manually.
      localProjects.push(project);
    } finally {
      downloading = '';
    }
  }

  function matchesProject(projects: Project[], project: Project): Project | undefined {
    if (project.id) {
      return projects.find(p => p.id == project.id && p.server?.id == project.server?.id);
    }
    return undefined;
  }
</script>
<div>
  <div class="flex flex-row mb-2 items-end mr-2 md:mr-0">
    <div class="sub-title !my-0">
      {#if server}
        {$t`${server.displayName} Server`}
      {:else}
        <div class="h-2 w-28 bg-surface-content/50 rounded-full animate-pulse"></div>
      {/if}
    </div>
    <div class="flex-grow"></div>
    {#if status?.loggedIn}
      <Button icon={mdiRefresh}
              title={$t`Refresh Projects`}
              disabled={loading}
              class="mr-2"
              on:click={() => dispatch('refreshProjects')}/>
      <LoginButton {status} on:status={() => dispatch('refreshAll')}/>
    {/if}
  </div>
  <div>
    {#if !status || loading}
      <!--override the defaults from App.svelte-->
      <!-- eslint-disable-next-line @typescript-eslint/naming-convention -->
      <Settings components={{ListItem: {classes: {root: 'animate-pulse'}}}}>
        <ListItem icon={mdiCloud} classes={{icon: 'text-neutral-50/50'}}>
          <div slot="title" class="h-4 bg-neutral-50/50 rounded-full w-32">
          </div>
          <div slot="actions" class="pointer-events-none">
            <div class="h-4 my-3 bg-neutral-50/50 rounded-full w-20"></div>
          </div>
        </ListItem>
      </Settings>
    {:else if !projects.length}
      <p class="text-surface-content/50 text-center elevation-1 md:rounded p-4">
        {#if status.loggedIn}
          {$t`No projects`}
        {:else}
          <LoginButton {status} on:status={() => dispatch('refreshAll')}/>
        {/if}
      </p>
    {:else}
      {#each projects as project}
        {@const localProject = matchesProject(localProjects, project)}
        {#if localProject?.crdt}
          <ButtonListItem href={`/project/${project.code}`}>
            <ListItem icon={mdiCloud}
                      title={project.name}
                      loading={downloading === project.name}>
              <div slot="actions" class="pointer-events-none shrink-0">
                <Button disabled icon={mdiBookSyncOutline} class="p-2">
                  {$t`Synced`}
                </Button>
              </div>
            </ListItem>
          </ButtonListItem>
        {:else}
          {@const loading = downloading === project.code}
          <ButtonListItem on:click={() => downloadCrdtProject(project)} disabled={!!downloading}>
            <ListItem icon={mdiCloud}
                      {loading}>
              <ProjectTitle slot="title" {project}/>
              <div slot="actions" class="pointer-events-none shrink-0">
                <Button icon={mdiBookArrowDownOutline} class="p-2">
                  {$t`Download`}
                </Button>
              </div>
            </ListItem>
          </ButtonListItem>
        {/if}
      {/each}
    {/if}
  </div>
</div>
