<script lang="ts">
  import type {IServerStatus} from '$lib/dotnet-types';
  import type {Project} from '$lib/services/projects-service';
  import LoginButton from '$lib/auth/LoginButton.svelte';
  import ButtonListItem from '$lib/utils/ButtonListItem.svelte';
  import {useProjectsService} from '$lib/services/service-provider';
  import {t} from 'svelte-i18n-lingui';
  import {cn} from '$lib/utils';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {AppNotification} from '$lib/notifications/notifications';
  import ProjectListItem from './ProjectListItem.svelte';

  const projectsService = useProjectsService();

  interface Props {
    status: IServerStatus | undefined;
    projects: Project[];
    localProjects: Project[];
    loading?: boolean;
    refreshProjects?: () => void;
    refreshAll?: () => void;
  }

  let {
    status,
    projects,
    localProjects,
    loading = false,
    refreshProjects = () => {
    },
    refreshAll = () => {
    }
  }: Props = $props();
  let downloading = $state('');

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
      refreshAll();
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

  let server = $derived(status?.server);
</script>
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
              onclick={() => refreshProjects()}/>
      <LoginButton {status} statusChange={() => refreshAll()}/>
    {/if}
  </div>
  <div class={cn('rounded', !projects.length && 'border')}>
    {#if !status || loading}
      <!--override the defaults from App.svelte-->
      <!-- eslint-disable-next-line @typescript-eslint/naming-convention -->
      <ProjectListItem icon="i-mdi-cloud" skeleton/>
    {:else if !projects.length}
      <p class="text-center md:rounded p-4">
        {#if status.loggedIn}
          <Button class="border border-primary" variant="link" target="_blank"
                  href="{server?.authority}/wheresMyProject">
            {$t`Where are my projects?`}
            <Icon icon="i-mdi-open-in-new" class="size-4"/>
          </Button>
        {:else}
          <LoginButton {status} statusChange={() => refreshAll()}/>
        {/if}
      </p>
    {:else}
      <div class="shadow rounded">
        {#each projects as project}
          {@const localProject = matchesProject(localProjects, project)}
          {#if localProject?.crdt}
            <ButtonListItem href={`/project/${project.code}`}>
              <ProjectListItem {project}>
                {#snippet actions()}
                  <div class="pointer-events-none shrink-0">
                    <Button disabled icon="i-mdi-book-sync-outline" variant="ghost" class="p-2">
                      {$t`Synced`}
                    </Button>
                  </div>
                {/snippet}
              </ProjectListItem>
            </ButtonListItem>
          {:else}
            {@const loading = downloading === project.code}
            <ButtonListItem onclick={() => downloadCrdtProject(project)} disabled={!!downloading}>
              <ProjectListItem icon="i-mdi-cloud" {project} {loading}>
                {#snippet actions()}
                  <div class="pointer-events-none shrink-0">
                    <Button icon="i-mdi-book-arrow-down-outline" variant="ghost" class="p-2">
                      {$t`Download`}
                    </Button>
                  </div>
                {/snippet}
              </ProjectListItem>
            </ButtonListItem>
          {/if}
        {/each}
      </div>
      <div class="text-center pt-2">
        <Button variant="link" target="_blank" href="{server?.authority}/wheresMyProject">
          {$t`I don't see my project`}
          <Icon icon="i-mdi-open-in-new" class="size-4"/>
        </Button>
      </div>
    {/if}
  </div>
</div>
