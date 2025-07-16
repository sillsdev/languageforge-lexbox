<script lang="ts">
  import {DownloadProjectByCodeResult} from '$lib/dotnet-types/generated-types/FwLiteShared/Projects/DownloadProjectByCodeResult';
  import type {IServerStatus} from '$lib/dotnet-types';
  import type {Project} from '$lib/services/projects-service';
  import LoginButton from '$lib/auth/LoginButton.svelte';
  import {useProjectsService} from '$lib/services/service-provider';
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {AppNotification} from '$lib/notifications/notifications';
  import GetProjectByCodeDialog from '$lib/admin-dialogs/GetProjectByCodeDialog.svelte';
  import type {UserProjectRole} from '$lib/dotnet-types/generated-types/LcmCrdt/UserProjectRole';
  import ProjectListItem from './ProjectListItem.svelte';
  import {transitionContext} from './transitions';
  import ListItem from '$lib/components/ListItem.svelte';
  import {navigate} from 'svelte-routing';

  const [send, receive] = transitionContext.getOr([function(){return {}}, function(){return {}}]);

  const projectsService = useProjectsService();

  interface Props {
    status: IServerStatus | undefined;
    projects: Project[];
    localProjects: Project[];
    loading?: boolean;
    canDownloadByCode?: boolean;
    refreshProjects?: () => void;
    refreshAll?: () => void;
  }

  let {
    status,
    projects,
    localProjects,
    loading = false,
    canDownloadByCode,
    refreshProjects = () => {
    },
    refreshAll = () => {
    }
  }: Props = $props();

  const undownloadedProjects = $derived(projects.filter(project => !matchesProject(localProjects, project)?.crdt));

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
        action: {
          label: $t`Open`,
          onClick: () => {
            navigate('/project/' + project.code);
          },
        }
      });
      await downloadPromise;
      refreshAll();
      // Getting an updated list of localProjects will take a moment. For the time being, we do it manually.
      localProjects.push(project);
    } finally {
      downloading = '';
    }
  }

  async function downloadCrdtProjectByCode(projectCode: string, userRole: UserProjectRole): Promise<string | undefined> {
    const downloadResult = await projectsService.downloadProjectByCode(projectCode, server!, userRole);
    switch(downloadResult)
    {
      case DownloadProjectByCodeResult.Success:
        refreshAll();
        return;
      case DownloadProjectByCodeResult.Forbidden:
        return $t`You don't have permission to download project ${projectCode} from ${server?.displayName ?? ''}`;
      case DownloadProjectByCodeResult.NotCrdtProject:
        return $t`Project ${projectCode} on ${server?.displayName ?? ''} is not yet set up for FieldWorks Lite`;
      case DownloadProjectByCodeResult.ProjectNotFound:
        return $t`Project ${projectCode} not found on ${server?.displayName ?? ''}`;
      case DownloadProjectByCodeResult.ProjectAlreadyDownloaded:
        return $t`You have already downloaded the ${projectCode} project`;
    }
  }

  function validateCodeForDownload(projectCode: string): string | undefined {
    if (localProjects.some(p => p.code === projectCode && p.server?.id === server?.id)) {
      return $t`You have already downloaded the ${projectCode} project`;
    }
  }

  function matchesProject(projects: Project[], project: Project): Project | undefined {
    if (project.id) {
      return projects.find(p => p.id == project.id && p.server?.id == project.server?.id);
    }
    return undefined;
  }

  let getProjectByCodeDialog: GetProjectByCodeDialog|undefined;
  function getProjectByCode() {
    getProjectByCodeDialog?.openDialog();
  }
  let server = $derived(status?.server);
</script>
<GetProjectByCodeDialog
  bind:this={getProjectByCodeDialog}
  onDownloadProject={downloadCrdtProjectByCode}
  validateCode={validateCodeForDownload}
  />
<div id={server?.id}>
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
  {#if !status || loading}
    <!--override the defaults from App.svelte-->
    <!-- eslint-disable-next-line @typescript-eslint/naming-convention -->
    <ProjectListItem icon="i-mdi-cloud" skeleton/>
  {:else if !undownloadedProjects.length && !canDownloadByCode}
    <div class="flex flex-col items-center justify-center md:rounded p-4 rounded border">
      {#if status.loggedIn}
        <Button class="border border-primary" variant="link" target="_blank"
                href="{server?.authority}/wheresMyProject">
          {$t`Where are my projects?`}
          <Icon icon="i-mdi-open-in-new" class="size-4"/>
        </Button>
      {:else}
        <LoginButton {status} statusChange={() => refreshAll()}/>
      {/if}
    </div>
  {:else}
    <div>
      {#each undownloadedProjects as project (project.id)}
        {@const loading = downloading === project.code}
        <div out:send={{key: 'project-' + project.code}} in:receive={{key: 'project-' + project.code}}>
          <ProjectListItem onclick={() => downloadCrdtProject(project)} icon="i-mdi-cloud" {project} {loading}>
            {#snippet actions()}
              <div class="pointer-events-none shrink-0">
                <Button icon="i-mdi-book-arrow-down-outline" variant="ghost" class="p-2">
                  {loading ? $t`Downloading...` : $t`Download`}
                </Button>
              </div>
            {/snippet}
          </ProjectListItem>
        </div>
      {/each}
      {#if canDownloadByCode}
        <ListItem icon="i-mdi-download"
                title={$t`Download unlisted project`}
                disabled={loading}
                onclick={getProjectByCode}>
          {$t`Download unlisted project`}
        </ListItem>
      {/if}
    </div>
    <div class="text-center pt-2">
      <Button variant="link" target="_blank" href="{server?.authority}/wheresMyProject">
        {$t`I don't see my project`}
        <Icon icon="i-mdi-open-in-new" class="size-4"/>
      </Button>
    </div>
  {/if}
</div>
