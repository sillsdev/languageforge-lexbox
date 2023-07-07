<script lang="ts">
  import { Badge, BadgeList, MemberBadge } from '$lib/components/Badges';
  import EditableText from '$lib/components/EditableText.svelte';
  import FormatDate from '$lib/components/FormatDate.svelte';
  import FormatProjectType from '$lib/components/FormatProjectType.svelte';
  import FormatRetentionPolicy from '$lib/components/FormatRetentionPolicy.svelte';
  import HgLogView from '$lib/components/HgLogView.svelte';
  import DeleteModal from '$lib/components/modals/DeleteModal.svelte';
  import type { $OpResult, ChangeProjectDescriptionMutation, ChangeProjectNameMutation } from '$lib/gql/types';
  import t from '$lib/i18n';
  import { isAdmin } from '$lib/user';
  import { z } from 'zod';
  import type { PageData } from './$types';
  import { _changeProjectDescription, _changeProjectName, _deleteProjectUser, type ProjectUser } from './+page';
  import AddProjectMember from './AddProjectMember.svelte';
  import ChangeMemberRoleModal from './ChangeMemberRoleModal.svelte';
  import {TrashIcon} from '$lib/icons';
  import Notify from '$lib/notify/notify.svelte';
  let notify: Notify;
  export let data: PageData;

  $: user = data.user;
  $: project = data.project;
  $: _project = project as NonNullable<typeof project>;

  let changeMemberRoleModal: ChangeMemberRoleModal;
  async function changeMemberRole(projectUser: ProjectUser): Promise<void> {
    await changeMemberRoleModal.open({
      userId: projectUser.user.id,
      name: projectUser.user.name,
      role: projectUser.role,
    });
    notify.add(`Project role of '${projectUser.user.name}' set to '${projectUser.role.toLowerCase()}'.`, 'success', 10)
  }

  let deleteUserModal: DeleteModal;
  let userToDelete: ProjectUser | undefined;
  async function deleteProjectUser(projectUser: ProjectUser): Promise<void> {
    userToDelete = projectUser;
    await deleteUserModal.prompt(async () => {
      await _deleteProjectUser(_project.id, projectUser.user.id);
    });
    notify.add(`User, '${projectUser.user.name}' has been removed.`, 'warning', 10);
  }

  function updateProjectName(newName: string): $OpResult<ChangeProjectNameMutation> {
    const result = _changeProjectName({ projectId: _project.id, name: newName });
    notify.add(`Project name is now, '${newName}'`, 'success', 10);
    return result;
  }

  function updateProjectDescription(newDescription: string): $OpResult<ChangeProjectDescriptionMutation> {
    const result = _changeProjectDescription({
      projectId: _project.id,
      description: newDescription,
    });
    notify.add(`Project description set to, '${newDescription}'`, 'success', 15);
    return result;
  }

  $: userId = user?.id;
  $: canManage = isAdmin(user) || user?.projects.find((project) => project.code == project.code)?.role == 'Manager';

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
        <span class="text-secondary">
          <EditableText
            disabled={!canManage}
            bind:value={project.name}
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
      <span>
        <EditableText
          bind:value={project.description}
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
            <ul class="dropdown-content menu bg-base-200 p-2 shadow rounded-box">
              <li>
                <button on:click={() => changeMemberRole(member)}>
                  <span class="i-mdi-account-lock text-2xl" />
                  {$t('project_page.change_role')}
                </button>
              </li>
              <li>
                <button class="hover:bg-error hover:text-error-content" on:click={() => deleteProjectUser(member)}>
                    <TrashIcon></TrashIcon>
                    {$t('project_page.remove_user')}
                </button>
              </li>
            </ul>
          </div>
        {/each}
        {#if canManage}
          <AddProjectMember projectId={project.id} notify={notify}/>
        {/if}

        <ChangeMemberRoleModal projectId={project.id} bind:this={changeMemberRoleModal} />

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
        <a class="link" href="/api/hg-view/{project.code}" target="_blank" rel="noreferrer">
          {$t('project_page.history')}
          <span class="i-mdi-open-in-new align-middle" />
        </a>
      </p>

      <!-- <HgWeb code={project.code} /> -->
      <HgLogView json={project.changesets} />
    </div>
  {:else}
    {$t('project_page.not_found', { code: data.code })}
  {/if}
</div>
<Notify bind:this={notify}></Notify>
