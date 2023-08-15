<script lang="ts">
  import { Badge, BadgeList, MemberBadge } from '$lib/components/Badges';
  import EditableText from '$lib/components/EditableText.svelte';
  import FormatDate from '$lib/components/FormatDate.svelte';
  import FormatProjectType from '$lib/components/FormatProjectType.svelte';
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
  import { TrashIcon } from '$lib/icons';
  import { notifySuccess, notifyWarning } from '$lib/notify';
  import { DialogResponse } from '$lib/components/modals';
  import type { ErrorMessage } from '$lib/forms';
  import ResetProjectModal from './ResetProjectModal.svelte';

  export let data: PageData;
  $: user = data.user;
  let projectStore = data.project;
  $: project = $projectStore;
  $: _project = project as NonNullable<typeof project>;

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
        })
      );
    }
  }

  let resetProjectModal: ResetProjectModal;
  async function resetProject(): void {
    const response = await resetProjectModal.open();
    console.log('Reset project modal response:', response);
  }

  let deleteUserModal: DeleteModal;
  let userToDelete: ProjectUser | undefined;
  async function deleteProjectUser(projectUser: ProjectUser): Promise<void> {
    userToDelete = projectUser;
    const deleted = await deleteUserModal.prompt(async () => {
      const { error } = await _deleteProjectUser(_project.id, projectUser.user.id);
      return error?.message;
    });
    if (deleted) {
      notifyWarning($t('project_page.notifications.user_delete', { name: projectUser.user.name }));
    }
  }

  async function updateProjectName(newName: string): Promise<ErrorMessage> {
    const result = await _changeProjectName({ projectId: _project.id, name: newName });
    if (result.error) {
      return result.error.message;
    }
    notifySuccess($t('project_page.notifications.rename_project', { name: newName }));
  }

  async function updateProjectDescription(newDescription: string): Promise<ErrorMessage> {
    const result = await _changeProjectDescription({
      projectId: _project.id,
      description: newDescription,
    });
    if (result.error) {
      return result.error.message;
    }
    notifySuccess($t('project_page.notifications.describe', { description: newDescription }));
  }

  $: userId = user.id;
  $: canManage = isAdmin(user) || user.projects.find(p => p.code == project?.code)?.role == 'Manager';

  const projectNameValidation = z.string().min(1, $t('project_page.project_name_empty_error'));
</script>

<svelte:head>
  <title>{project?.name ?? $t('project_page.not_found', { code: data.code })}</title>
</svelte:head>

<div class="space-y-4">
  {#if project}
    <div class="space-y-2">
      <div class="text-3xl flex items-center gap-3 flex-wrap">
        <span>{$t('project_page.project')}:</span>
        <span class="text-primary">
          <EditableText
            disabled={!canManage}
            value={project.name}
            validation={projectNameValidation}
            saveHandler={updateProjectName}
          />
        </span>
      </div>
      <BadgeList>
        <Badge><FormatProjectType type={project.type} /></Badge>
        <Badge><FormatRetentionPolicy policy={project.retentionPolicy} /></Badge>
      </BadgeList>
    </div>

    <div class="divider" />

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
          <div class="dropdown dropdown-end">
            <MemberBadge
              member={{ name: member.user.name, role: member.role }}
              canManage={canManage && (member.user.id != userId || isAdmin(user))}
            />
            <ul class="dropdown-content menu bg-base-200 p-2 shadow rounded-box z-10">
              <li>
                <button on:click={() => changeMemberRole(member)}>
                  <span class="i-mdi-account-lock text-2xl" />
                  {$t('project_page.change_role')}
                </button>
              </li>
              <li>
                <button class="hover:bg-error hover:text-error-content" on:click={() => deleteProjectUser(member)}>
                  <TrashIcon />
                  {$t('project_page.remove_user')}
                </button>
              </li>
            </ul>
          </div>
        {/each}
        {#if canManage}
          <AddProjectMember projectId={project.id} />
        {/if}

        <ChangeMemberRoleModal projectId={project.id} bind:this={changeMemberRoleModal} />
        {#if isAdmin(user)}
          <ResetProjectModal bind:this={resetProjectModal} code={data.code} />
        {/if}

        <DeleteModal
          bind:this={deleteUserModal}
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
      <p class="text-2xl mb-4">
        <a class="link" href="/hg/{project.code}" target="_blank" rel="noreferrer">
          {$t('project_page.history')}
          <span class="i-mdi-open-in-new align-middle" />
        </a>
      </p>

      <!-- <HgWeb code={project.code} /> -->
      <HgLogView json={project.changesets} />
    </div>
    {#if isAdmin(user)}
    <div class="card card-bordered border-error">
      <p class="text-2xl mb-4">
        <span class="link" on:click={() => resetProject()}>
          {$t('project_page.reset_project_modal.title', {name: project?.name})}
          <span class="i-mdi-open-in-new align-middle" />
        </span>
      </p>
    </div>
    {/if}
  {:else}
    {$t('project_page.not_found', { code: data.code })}
  {/if}
</div>
