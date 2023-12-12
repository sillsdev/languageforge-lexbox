<script lang="ts">
  import { Badge, BadgeList, MemberBadge } from '$lib/components/Badges';
  import EditableText from '$lib/components/EditableText.svelte';
  import FormatDate from '$lib/components/FormatDate.svelte';
  import { ProjectTypeBadge } from '$lib/components/ProjectType';
  import FormatRetentionPolicy from '$lib/components/FormatRetentionPolicy.svelte';
  import HgLogView from '$lib/components/HgLogView.svelte';
  import DeleteModal from '$lib/components/modals/DeleteModal.svelte';
  import t from '$lib/i18n';
  import { isAdmin } from '$lib/user';
  import { z } from 'zod';
  import type { PageData } from './$types';
  import { _changeProjectDescription, _changeProjectName, _deleteProjectUser, type ProjectUser } from './+page';
  import AddProjectMember from './AddProjectMember.svelte';
  import ChangeMemberRoleModal from './ChangeMemberRoleModal.svelte';
  import { CircleArrowIcon, TrashIcon, type IconString } from '$lib/icons';
  import type { BadgeVariant } from '$lib/components/Badges/Badge.svelte';
  import { useNotifications } from '$lib/notify';
  import { DialogResponse } from '$lib/components/modals';
  import type { ErrorMessage } from '$lib/forms';
  import ResetProjectModal from './ResetProjectModal.svelte';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import { FormField } from '$lib/forms';
  import IconButton from '$lib/components/IconButton.svelte';
  import { delay } from '$lib/util/time';
  import { page } from '$app/stores';
  import ConfirmDeleteModal from '$lib/components/modals/ConfirmDeleteModal.svelte';
  import { _deleteProject } from '$lib/gql/mutations';
  import { goto } from '$app/navigation';
  import MoreSettings from '$lib/components/MoreSettings.svelte';
  import { AdminContent, HeaderPage } from '$lib/layout';
  import Markdown from 'svelte-exmarkdown';
  import { ProjectMigrationStatus, ProjectRole, ResetStatus } from '$lib/gql/generated/graphql';
  import { onMount } from 'svelte';
  import Button from '$lib/forms/Button.svelte';
  import Icon from '$lib/icons/Icon.svelte';

  export let data: PageData;
  $: user = data.user;
  let projectStore = data.project;
  $: project = $projectStore;
  $: changesetStore = data.changesets;

  $: projectHgUrl = import.meta.env.DEV
    ? `http://hg.${$page.url.host}/${data.code}`
    : `https://hg-public.${$page.url.host.replace('depot', 'forge')}/${data.code}`;

  const { notifySuccess, notifyWarning } = useNotifications();

  let changeMemberRoleModal: ChangeMemberRoleModal;
  async function changeMemberRole(projectUser: ProjectUser): Promise<void> {
    const { response } = await changeMemberRoleModal.open({
      userId: projectUser.user.id,
      name: projectUser.user.name,
      role: projectUser.role,
    });

    if (response === DialogResponse.Submit) {
      notifySuccess(
        $t('project_page.notifications.role_change', {
          name: projectUser.user.name,
          role: projectUser.role.toLowerCase(),
        }),
      );
    }
  }

  let resetProjectModal: ResetProjectModal;
  async function resetProject(): Promise<void> {
    await resetProjectModal.open(project.code, project.resetStatus);
  }

  let removeUserModal: DeleteModal;
  let userToDelete: ProjectUser | undefined;
  async function deleteProjectUser(projectUser: ProjectUser): Promise<void> {
    userToDelete = projectUser;
    const deleted = await removeUserModal.prompt(async () => {
      const { error } = await _deleteProjectUser(project.id, projectUser.user.id);
      return error?.message;
    });
    if (deleted) {
      notifyWarning($t('project_page.notifications.user_delete', { name: projectUser.user.name }));
    }
  }

  async function updateProjectName(newName: string): Promise<ErrorMessage> {
    const result = await _changeProjectName({ projectId: project.id, name: newName });
    if (result.error) {
      return result.error.message;
    }
    notifySuccess($t('project_page.notifications.rename_project', { name: newName }));
  }

  async function updateProjectDescription(newDescription: string): Promise<ErrorMessage> {
    const result = await _changeProjectDescription({
      projectId: project.id,
      description: newDescription,
    });
    if (result.error) {
      return result.error.message;
    }
    notifySuccess($t('project_page.notifications.describe', { description: newDescription }));
  }

  $: userId = user.id;
  $: canManage = isAdmin(user) || project?.users.find((u) => u.user.id == userId)?.role == ProjectRole.Manager;

  const projectNameValidation = z.string().min(1, $t('project_page.project_name_empty_error'));

  var getProjectDropdownTrigger: HTMLElement;

  var copyingToClipboard = false;
  var copiedToClipboard = false;

  async function copyProjectUrlToClipboard(): Promise<void> {
    getProjectDropdownTrigger.focus(); // keeps the dropdown open
    copyingToClipboard = true;
    await navigator.clipboard.writeText(projectHgUrl);
    copiedToClipboard = true;
    copyingToClipboard = false;
    await delay();
    copiedToClipboard = false;
  }

  let deleteProjectModal: ConfirmDeleteModal;

  async function softDeleteProject(): Promise<void> {
    const result = await deleteProjectModal.open(project.name, async () => {
      const { error } = await _deleteProject(project.id);
      return error?.message;
    });
    if (result.response === DialogResponse.Submit) {
      notifyWarning($t('delete_project_modal.success', { name: project.name, code: project.code }));
      await goto(data.home);
    }
  }

  let migrationStatus = project?.migrationStatus ?? ProjectMigrationStatus.Unknown;
  $: isMigrated = migrationStatus === ProjectMigrationStatus.Migrated;
  //no need to translate these since it'll only be temporary
  const migrationStatusTable = {
    [ProjectMigrationStatus.Migrated]: 'Migrated',
    [ProjectMigrationStatus.Migrating]: 'Migrating',
    [ProjectMigrationStatus.Unknown]: 'Unknown',
    [ProjectMigrationStatus.PrivateRedmine]: 'Not Migrated (private)',
    [ProjectMigrationStatus.PublicRedmine]: 'Not Migrated (public)',
  } satisfies Record<ProjectMigrationStatus, string>;
  const migrationStatusIcon = {
    [ProjectMigrationStatus.Migrated]: 'i-mdi-check-circle',
    [ProjectMigrationStatus.Migrating]: 'loading loading-spinner loading-xs',
    [ProjectMigrationStatus.Unknown]: undefined,
    [ProjectMigrationStatus.PrivateRedmine]: undefined,
    [ProjectMigrationStatus.PublicRedmine]: undefined,
  } satisfies Record<ProjectMigrationStatus, IconString | undefined>;
  const migrationStatusBadgeVariant = {
    [ProjectMigrationStatus.Migrated]: 'badge-success',
    [ProjectMigrationStatus.Migrating]: 'badge-warning',
    [ProjectMigrationStatus.Unknown]: 'badge-neutral',
    [ProjectMigrationStatus.PrivateRedmine]: 'badge-neutral',
    [ProjectMigrationStatus.PublicRedmine]: 'badge-neutral',
  } satisfies Record<ProjectMigrationStatus, BadgeVariant>;
  onMount(() => {
    migrationStatus = project?.migrationStatus ?? ProjectMigrationStatus.Unknown;
    if (migrationStatus === ProjectMigrationStatus.Migrating) {
      void watchMigrationStatus();
    }
  });

  async function watchMigrationStatus(): Promise<void> {
    if (!project) return;
    const result = await fetch(`/api/project/awaitMigrated?projectCode=${project.code}`);
    const response = await result.json();
    if (response) {
      migrationStatus = ProjectMigrationStatus.Migrated;
    }
  }

  async function migrateProject(): Promise<void> {
    if (!project) return;
    if (!confirm('Are you sure you want to migrate this project, you can not undo this action?')) return;
    await fetch(`/api/migrate/migrateRepo?projectCode=${project.code}`);
    migrationStatus = ProjectMigrationStatus.Migrating;
    await watchMigrationStatus();
  }
</script>

<!-- we need the if so that the page doesn't break when we delete the project -->
{#if project}
  <HeaderPage wide title={project.name}>
    <svelte:fragment slot="banner">
      {#if migrationStatus === ProjectMigrationStatus.Migrating}
            <div class="alert alert-warning mb-4">
              <span class="i-mdi-alert text-2xl" />
              <span>This project is currently being migrated. Some features may not work as expected.</span>
            </div>
      {/if}
    </svelte:fragment>
    <svelte:fragment slot="actions">
      {#if migrationStatus !== ProjectMigrationStatus.Migrating}
        <Dropdown>
          <!-- svelte-ignore a11y-label-has-associated-control -->
          <label bind:this={getProjectDropdownTrigger} tabindex="-1" class="btn btn-success">
            {$t('project_page.get_project.label')}
            <span class="i-mdi-dots-vertical text-2xl" />
          </label>
          <div slot="content" class="card w-[calc(100vw-1rem)] sm:max-w-[35rem]">
            <div class="card-body max-sm:p-4">
              <div class="prose">
                <Markdown
                  md={$t('project_page.get_project.instructions', {
                    type: project.type,
                    code: data.code,
                    name: project.name,
                  })}
                />
              </div>
              <AdminContent>
                <FormField label={$t('project_page.get_project.send_receive_url')}>
                  <div class="join">
                    <input
                      value={projectHgUrl}
                      class="input input-bordered join-item w-full focus:input-success"
                      readonly
                    />
                    <div
                      class="join-item tooltip-open"
                      class:tooltip={copiedToClipboard}
                      data-tip={$t('clipboard.copied')}
                    >
                      {#if copiedToClipboard}
                        <IconButton disabled icon="i-mdi-check" style="btn-outline btn-success" />
                      {:else}
                        <IconButton
                          loading={copyingToClipboard}
                          icon="i-mdi-content-copy"
                          style="btn-outline"
                          on:click={copyProjectUrlToClipboard}
                        />
                      {/if}
                    </div>
                  </div>
                </FormField>
              </AdminContent>
            </div>
          </div>
        </Dropdown>
      {/if}
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
        <AdminContent>
          <Badge type={migrationStatusBadgeVariant[migrationStatus]} icon={migrationStatusIcon[migrationStatus]}>
            {migrationStatusTable[migrationStatus]}
          </Badge>
        </AdminContent>
        {#if project.resetStatus === ResetStatus.InProgress}
          <button
            class:tooltip={isAdmin(user)}
            data-tip={$t('project_page.reset_project_modal.click_to_continue')}
            disabled={!isAdmin(user)}
            on:click={resetProject}
          >
            <Badge type="badge-warning">
              {$t('project_page.reset_project_modal.reset_in_progress')}
              <span class="i-mdi-warning text-xl mb-[-2px]" />
            </Badge>
          </button>
        {/if}
      </BadgeList>
    </svelte:fragment>
    <div class="space-y-4">
      <p class="text-2xl mb-4">{$t('project_page.summary')}</p>
      <div class="space-y-2">
        <span class="text-lg">
          {$t('project_page.project_code')}:
          <span class="text-secondary">{project.code}</span>
        </span>
        <div class="text-lg">
          {$t('project_page.last_commit')}:
          <span class="text-secondary"><FormatDate date={project.lastCommit} /></span>
        </div>
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

      <div>
        <p class="text-2xl mb-4">
          {$t('project_page.members')}
        </p>

        <BadgeList>
          {#each project.users as member}
            {@const canManageMember = canManage && (member.user.id !== userId || isAdmin(user))}
            <Dropdown disabled={!canManageMember}>
              <MemberBadge member={{ name: member.user.name, role: member.role }} canManage={canManageMember} />
              <ul slot="content" class="menu">
                <li>
                  <button on:click={() => changeMemberRole(member)}>
                    <span class="i-mdi-account-lock text-2xl" />
                    {$t('project_page.change_role')}
                  </button>
                </li>
                <li>
                  <button class="text-error" on:click={() => deleteProjectUser(member)}>
                    <TrashIcon />
                    {$t('project_page.remove_user')}
                  </button>
                </li>
              </ul>
            </Dropdown>
          {/each}
          {#if canManage}
            <AddProjectMember projectId={project.id} />
          {/if}

          <ChangeMemberRoleModal projectId={project.id} bind:this={changeMemberRoleModal} />

          <DeleteModal
            bind:this={removeUserModal}
            entityName={$t('project_page.remove_project_user_title')}
            isRemoveDialog
          >
            {$t('project_page.confirm_remove', {
              userName: userToDelete?.user.name ?? '',
            })}
          </DeleteModal>
        </BadgeList>
      </div>

      <div class="divider" />
      <div class="space-y-2">
        <p class="text-2xl mb-4 flex gap-4 items-baseline">
          {$t('project_page.history')}
          <a class="btn btn-sm btn-outline btn-info" href="/hg/{project.code}" target="_blank">
            {$t('project_page.hg.open_in_hgweb')}<span class="i-mdi-open-in-new text-2xl" />
          </a>
        </p>

        <div class="max-h-[75vh] overflow-auto border-b border-base-200">
          <HgLogView logEntries={$changesetStore.changesets} loading={$changesetStore.fetching} />
        </div>
      </div>

      {#if canManage}
        <div class="divider" />

        <MoreSettings>
          <button class="btn btn-error" class:hidden={!isMigrated} on:click={softDeleteProject}>
            {$t('delete_project_modal.submit')}<TrashIcon />
          </button>
          <AdminContent>
            <button class="btn btn-accent" class:hidden={!isMigrated} on:click={resetProject}>
              {$t('project_page.reset_project_modal.submit')}<CircleArrowIcon />
            </button>
            <ResetProjectModal bind:this={resetProjectModal} />
            {#if migrationStatus === ProjectMigrationStatus.PublicRedmine || migrationStatus === ProjectMigrationStatus.PrivateRedmine}
              <Button on:click={migrateProject}>
                Migrate Project
                <Icon icon="i-mdi-source-branch-sync" />
              </Button>
            {/if}
          </AdminContent>
        </MoreSettings>
      {/if}

      <ConfirmDeleteModal bind:this={deleteProjectModal} i18nScope="delete_project_modal" />
    </div>
  </HeaderPage>
{/if}
