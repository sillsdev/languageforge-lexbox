<script lang="ts">
  import { Badge, BadgeList } from '$lib/components/Badges';
  import EditableText from '$lib/components/EditableText.svelte';
  import { ProjectTypeBadge } from '$lib/components/ProjectType';
  import FormatRetentionPolicy from '$lib/components/FormatRetentionPolicy.svelte';
  import t from '$lib/i18n';
  import { isAdmin } from '$lib/user';
  import { z } from 'zod';
  import type { PageData } from './$types';
  import {
    _changeDraftProjectDescription,
    _changeDraftProjectName,
    _promoteDraftProject,
  } from './+page';
  import { useNotifications } from '$lib/notify';
  import { type ErrorMessage } from '$lib/forms';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import { HeaderPage, PageBreadcrumb } from '$lib/layout';
  import Markdown from 'svelte-exmarkdown';
  import { ProjectType } from '$lib/gql/generated/graphql';
  import SendReceiveUrlField from '../../project/[project_code]/SendReceiveUrlField.svelte';
  import IconButton from '$lib/components/IconButton.svelte';
  import { delay } from '$lib/util/time';
  import { goto } from '$app/navigation';

  // We're certainly not going to use all those imports, but I can delete the redundant ones later

  export let data: PageData;
  $: user = data.user;
  let projectStore = data.project;
  $: project = $projectStore;
  $: canManage = isAdmin(user);

  const { notifySuccess, notifyWarning } = useNotifications();

  // TODO: This is the third copy of this clipboard-copying code. Time to refactor into a separate component.
  var copyingToClipboard = false;
  var copiedToClipboard = false;

  async function copyProjectCodeToClipboard(): Promise<void> {
    copyingToClipboard = true;
    await navigator.clipboard.writeText(project.code);
    copiedToClipboard = true;
    copyingToClipboard = false;
    await delay();
    copiedToClipboard = false;
  }

  const projectNameValidation = z.string().min(1, $t('project_page.project_name_empty_error'));

  async function updateProjectName(newName: string): Promise<ErrorMessage> {
    const result = await _changeDraftProjectName({ projectId: project.id, name: newName });
    if (result.error) {
      return result.error.message;
    }
    notifySuccess($t('project_page.notifications.rename_project', { name: newName }));
  }

  async function updateProjectDescription(newDescription: string): Promise<ErrorMessage> {
    const result = await _changeDraftProjectDescription({
      projectId: project.id,
      description: newDescription,
    });
    if (result.error) {
      return result.error.message;
    }
    notifySuccess($t('project_page.notifications.describe', { description: newDescription }));
  }

  async function promoteProject(): Promise<void> {
    const result = await _promoteDraftProject({
      draftProjectId: project.id,
    });
    if (result.error) {
      notifyWarning(result.error.message);
    }
    notifySuccess($t('project_page.notifications.promote_project'));
    await goto(`/project/${project.code}`);
  }
</script>


<PageBreadcrumb>{$t('project_page.project')}</PageBreadcrumb>

<!-- we need the if so that the page doesn't break when we delete the project -->
{#if project}
  <HeaderPage wide title={project.name}>
    <svelte:fragment slot="banner">
        <div class="alert alert-warning mb-4">
          <span class="i-mdi-alert text-2xl" />
          <span>This is a draft project. Click below to promote it to a real project.</span>
        </div>
    </svelte:fragment>
    <svelte:fragment slot="actions">
      <button on:click={promoteProject} class="btn btn-primary">
        {$t('project_page.promote_project.label')}
      </button>
    </svelte:fragment>
    <svelte:fragment slot="title">
      <div class="max-w-full flex items-baseline flex-wrap">
        <span class="mr-2">{$t('project_page.project')}:</span>
        <span class="text-primary max-w-full">
          <EditableText
            disabled={!canManage}
            value={project.name}
            validation={projectNameValidation}
            saveHandler={updateProjectName}
          />
        </span>
      </div>
    </svelte:fragment>
    <svelte:fragment slot="header-content">
      <BadgeList>
        <ProjectTypeBadge type={project.type} />
        <Badge>
          <FormatRetentionPolicy policy={project.retentionPolicy} />
        </Badge>
      </BadgeList>
    </svelte:fragment>
    <div class="space-y-4">
      <p class="text-2xl mb-4">{$t('project_page.summary')}</p>
      <div class="space-y-2">
        <span class="text-lg">
          {$t('project_page.project_code')}:
          <span class="inline-flex items-center gap-1">
            <span class="text-secondary">{project.code}</span>
            {#if copiedToClipboard}
              <div class="tooltip tooltip-open" data-tip={$t('clipboard.copied')}>
                <IconButton fake icon="i-mdi-check" size="btn-sm" class="text-success" />
              </div>
            {:else}
                <IconButton
                loading={copyingToClipboard}
                icon="i-mdi-content-copy"
                size="btn-sm"
                variant="btn-ghost"
                outline={false}
                on:click={copyProjectCodeToClipboard}
              />
            {/if}
          </span>
        </span>
        <div class="text-lg">{$t('project_page.description')}:</div>
        <span class="text-secondary">
          <EditableText
            value={project.description}
            disabled={!canManage}
            saveHandler={updateProjectDescription}
            placeholder={$t('project_page.add_description')}
            multiline
          />
        </span>
      </div>
    </div>
  </HeaderPage>
{/if}
