<script lang="ts">
  import { Badge, BadgeList, MemberBadge } from '$lib/components/Badges';
  import EditableText from '$lib/components/EditableText.svelte';
  import { ProjectTypeBadge } from '$lib/components/ProjectType';
  import FormatRetentionPolicy from '$lib/components/FormatRetentionPolicy.svelte';
  import HgLogView from '$lib/components/HgLogView.svelte';
  import DeleteModal from '$lib/components/modals/DeleteModal.svelte';
  import t, { date, number } from '$lib/i18n';
  import { z } from 'zod';
  import type { PageData } from './$types';
  import {
    _changeProjectDescription,
    _changeProjectName,
    _deleteProjectUser,
    _leaveProject,
    type ProjectUser,
  } from './+page';
  import CopyToClipboardButton from '$lib/components/CopyToClipboardButton.svelte';
  import AddProjectMember from './AddProjectMember.svelte';
  import BulkAddProjectMembers from './BulkAddProjectMembers.svelte';
  import ChangeMemberRoleModal from './ChangeMemberRoleModal.svelte';
  import { CircleArrowIcon, TrashIcon } from '$lib/icons';
  import { useNotifications } from '$lib/notify';
  import {DialogResponse, Modal} from '$lib/components/modals';
  import { Button, type ErrorMessage } from '$lib/forms';
  import ResetProjectModal from './ResetProjectModal.svelte';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import ConfirmDeleteModal from '$lib/components/modals/ConfirmDeleteModal.svelte';
  import {_deleteProject} from '$lib/gql/mutations';
  import { goto } from '$app/navigation';
  import MoreSettings from '$lib/components/MoreSettings.svelte';
  import { AdminContent, HeaderPage, PageBreadcrumb } from '$lib/layout';
  import Markdown from 'svelte-exmarkdown';
  import { ProjectRole, ProjectType, ResetStatus } from '$lib/gql/generated/graphql';
  import Icon from '$lib/icons/Icon.svelte';
  import OpenInFlexModal from './OpenInFlexModal.svelte';
  import OpenInFlexButton from './OpenInFlexButton.svelte';
  import SendReceiveUrlField from './SendReceiveUrlField.svelte';
  import {isDev} from '$lib/layout/DevContent.svelte';
  import UserModal from '$lib/components/Users/UserModal.svelte';
  import IconButton from '$lib/components/IconButton.svelte';
  import ConfirmModal from '$lib/components/modals/ConfirmModal.svelte';
  import ProjectConfidentialityBadge from './ProjectConfidentialityBadge.svelte';
  import ProjectConfidentialityModal from './ProjectConfidentialityModal.svelte';

  export let data: PageData;
  $: user = data.user;
  let projectStore = data.project;
  $: project = $projectStore;
  $: changesetStore = data.changesets;
  let isEmpty: boolean = false;
  $: isEmpty = project?.lastCommit == null;
  // TODO: Once we've stabilized the lastCommit issue with project reset, get rid of the next line
  $: if (! $changesetStore.fetching) isEmpty = $changesetStore.changesets.length === 0;
  $: members = project.users.sort((a, b) => {
    if (a.role !== b.role) {
      return a.role === ProjectRole.Manager ? -1 : 1;
    }
    return a.user.name.localeCompare(b.user.name);
  });

  let lexEntryCount: number | string | null | undefined = undefined;
  $: lexEntryCount = project.flexProjectMetadata?.lexEntryCount;

  const TRUNCATED_MEMBER_COUNT = 5;
  let showAllMembers = false;
  $: showMembers = showAllMembers ? members : members.slice(0, TRUNCATED_MEMBER_COUNT);

  const { notifySuccess, notifyWarning } = useNotifications();

  let userModal: UserModal;

  let loadingEntryCount = false;
  async function updateEntryCount(): Promise<void> {
    loadingEntryCount = true;
    const response = await fetch(`/api/project/updateLexEntryCount/${project.code}`, {method: 'POST'});
    lexEntryCount = await response.text();
    loadingEntryCount = false;
  }

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
  $: canManage = user.isAdmin || project?.users.find((u) => u.user.id == userId)?.role == ProjectRole.Manager;

  const projectNameValidation = z.string().trim().min(1, $t('project_page.project_name_empty_error'));

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

  let hgCommandResultModal: Modal;
  let hgCommandResponse = '';
  let hgCommandRunning = false;

  async function verify(): Promise<void> {
    await hgCommand(async () => fetch(`/api/project/hgVerify/${project.code}`));
  }

  async function recover(): Promise<void> {
    await hgCommand(async () => fetch(`/api/project/hgRecover/${project.code}`));
  }

  async function streamHgCommandResponse(body: ReadableStream<Uint8Array> | null): Promise<void> {
    if (body == null) return;
    const decoder = new TextDecoder();
    // Would be nice to just do this:
    // for await (const chunk of body) {
    //   hgCommandResponse += decoder.decode(chunk, {stream: true});
    // }
    // But that only works on Firefox. So until Chrome implements that, we have to do this:
    const reader = body.getReader();
    let done = false;
    let value;
    while (!done) {
      ({done, value} = await reader.read());
      // The {stream: true} is important here; without it, in theory the output could be
      // broken in the middle of a UTF-8 byte sequence and get garbled. But with stream: true,
      // TextDecoder() will know to expect more output, so if the stream returns a partial
      // UTF-8 sequence, TextDecoder() will return everything except that sequence and wait
      // for more bytes before decoding that sequence.
      if (value) hgCommandResponse += decoder.decode(value, {stream: true});
    }
  }

  async function hgCommand(execute: ()=> Promise<Response>): Promise<void> {
    hgCommandResponse = '';
    void hgCommandResultModal.openModal(true, true);
    let response = await execute();
    hgCommandRunning = true;
    try {
      await streamHgCommandResponse(response.body);
      // Some commands, like hg recover, return nothing if there's nothing to be done
      if (hgCommandResponse == '') hgCommandResponse = 'No response';
    } finally {
      hgCommandRunning = false;
    }
  }

  let projectConfidentialityModal: ProjectConfidentialityModal;
  let openInFlexModal: OpenInFlexModal;
  let leaveModal: ConfirmModal;

  async function leaveProject(): Promise<void> {
    projectStore.pause();
    changesetStore.pause();
    let left = false;
    try {
      left = await leaveModal.open(async () => {
        const result = await _leaveProject(project.id);
        if (result.error?.byType('LastMemberCantLeaveError')) {
          return $t('project_page.leave.last_to_leave');
        }
      });
      if (left) {
        notifySuccess($t('project_page.leave.leave_success', {projectName: project.name}))
        await goto(data.home);
      }
    } finally {
      if (!left) {
        projectStore.resume();
        changesetStore.resume();
      }
    }
  }
</script>

<PageBreadcrumb>{$t('project_page.project')}</PageBreadcrumb>

<!-- we need the if so that the page doesn't break when we delete the project -->
{#if project}
  <HeaderPage wide title={project.name}>
    <svelte:fragment slot="actions">
      {#if project.type === ProjectType.FlEx && $isDev}
          <a href="./{project.code}/viewer" class="btn btn-neutral text-[#DCA54C] flex items-center gap-2">
            {$t('project_page.open_with_viewer')}
            <span class="i-mdi-dictionary text-2xl" />
          </a>
          <OpenInFlexModal bind:this={openInFlexModal} {project}/>
          <OpenInFlexButton projectId={project.id} on:click={openInFlexModal.open}/>
      {:else}
        <Dropdown>
          <button class="btn btn-primary">
            {$t('project_page.get_project.label', {isEmpty: isEmpty.toString()})}
            <span class="i-mdi-dots-vertical text-2xl" />
          </button>
          <div slot="content" class="card w-[calc(100vw-1rem)] sm:max-w-[35rem]">
            <div class="card-body max-sm:p-4">
              <div class="prose">
                <h3>{$t('project_page.get_project.instructions_header', {type: project.type, mode: 'normal', isEmpty: isEmpty.toString()})}</h3>
                {#if project.type === ProjectType.WeSay}
                  {#if isEmpty}
                    <Markdown
                        md={$t('project_page.get_project.instructions_wesay_empty', {
                        code: project.code,
                        login: encodeURIComponent(user.emailOrUsername),
                        name: project.name,
                      })}
                    />
                  {:else}
                    <Markdown
                        md={$t('project_page.get_project.instructions_wesay', {
                        code: project.code,
                        login: encodeURIComponent(user.emailOrUsername),
                        name: project.name,
                      })}
                    />
                  {/if}
                {:else}
                  {#if isEmpty}
                  <Markdown
                    md={$t('project_page.get_project.instructions_flex_empty', {
                    code: project.code,
                    login: user.emailOrUsername,
                    name: project.name,
                  })}
                  />
                  {:else}
                  <Markdown
                    md={$t('project_page.get_project.instructions_flex', {
                    code: project.code,
                    login: user.emailOrUsername,
                    name: project.name,
                  })}
                  />
                  {/if}
                {/if}
              </div>
              <SendReceiveUrlField projectCode={project.code} />
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
        <ProjectConfidentialityBadge on:click={projectConfidentialityModal.openModal} {canManage} isConfidential={project.isConfidential ?? undefined} />
        <ProjectTypeBadge type={project.type} />
        <Badge>
          <FormatRetentionPolicy policy={project.retentionPolicy} />
        </Badge>
        {#if project.resetStatus === ResetStatus.InProgress}
          <button
            class:tooltip={user.isAdmin}
            data-tip={$t('project_page.reset_project_modal.click_to_continue')}
            disabled={!user.isAdmin}
            on:click={resetProject}
          >
            <Badge variant="badge-warning">
              {$t('project_page.reset_project_modal.reset_in_progress')}
              <span class="i-mdi-warning text-xl mb-[-2px]" />
            </Badge>
          </button>
        {/if}
      </BadgeList>
      <ProjectConfidentialityModal bind:this={projectConfidentialityModal} projectId={project.id} isConfidential={project.isConfidential ?? undefined} />
    </svelte:fragment>
    <div class="space-y-4">
      <p class="text-2xl mb-4">{$t('project_page.summary')}</p>
      <div class="space-y-2">
        <span class="text-lg">
          {$t('project_page.project_code')}:
          <span class="inline-flex items-center gap-1">
            <span class="text-secondary">{project.code}</span>
            <CopyToClipboardButton textToCopy={project.code} size="btn-sm" outline={false} />
          </span>
        </span>
        <div class="text-lg">
          {$t('project_page.created_at')}:
          <span class="text-secondary">{$date(project.createdDate)}</span>
        </div>
        <div class="text-lg">
          {$t('project_page.last_commit')}:
          <span class="text-secondary">{$date(project.lastCommit)}</span>
        </div>
        {#if project.type === ProjectType.FlEx || project.type === ProjectType.WeSay}
          <div class="text-lg flex items-center gap-1">
            {$t('project_page.num_entries')}:
            <span class="text-secondary">
              {$number(lexEntryCount)}
            </span>
            <AdminContent>
              <IconButton
                loading={loadingEntryCount}
                icon="i-mdi-refresh"
                size="btn-sm"
                variant="btn-ghost"
                outline={false}
                on:click={updateEntryCount}
              />
            </AdminContent>
          </div>
        {/if}
        <div>
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

      <div>
        <p class="text-2xl mb-4">
          {$t('project_page.members.title')}
        </p>

        <BadgeList grid={showMembers.length > TRUNCATED_MEMBER_COUNT}>
          {#each showMembers as member}
            {@const canManageMember = canManage && (member.user.id !== userId || user.isAdmin)}
            <Dropdown disabled={!canManageMember}>
              <MemberBadge member={{ name: member.user.name, role: member.role }} canManage={canManageMember} />
              <ul slot="content" class="menu">
                <AdminContent>
                  <li>
                    <button on:click={() => userModal.open(member.user)}>
                      <Icon icon="i-mdi-card-account-details-outline" size="text-2xl" />
                      {$t('project_page.view_user_details')}
                    </button>
                  </li>
                </AdminContent>
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

          {#if members.length > TRUNCATED_MEMBER_COUNT}
            <div class="justify-self-start">
              <Button outline size="btn-sm" on:click={() => (showAllMembers = !showAllMembers)}>
                {showAllMembers ? $t('project_page.members.show_less') : $t('project_page.members.show_all')}
              </Button>
            </div>
          {/if}

          {#if canManage}
            <div class="flex grow flex-wrap place-self-end gap-3 place-content-end" style="grid-column: -2 / -1">
              <AddProjectMember projectId={project.id} />
              <BulkAddProjectMembers projectId={project.id} />
            </div>
          {/if}

          <ChangeMemberRoleModal projectId={project.id} bind:this={changeMemberRoleModal} />
          <UserModal bind:this={userModal}/>

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
          <HgLogView logEntries={$changesetStore.changesets} loading={$changesetStore.fetching} projectCode={project.code} />
        </div>
      </div>

      <div class="divider"/>

      <MoreSettings column>
        <div class="flex gap-4 max-sm:flex-col-reverse">
          {#if canManage}
            <button class="btn btn-error" on:click={softDeleteProject}>
              {$t('delete_project_modal.submit')}
              <TrashIcon/>
            </button>
            <Button outline variant="btn-warning" on:click={projectConfidentialityModal.openModal}>
              {$t('project.confidential.set_confidentiality')}
              <Icon icon="i-mdi-shield-lock-outline"/>
            </Button>
            {/if}
          <Button outline variant="btn-error" on:click={leaveProject}>
            {$t('project_page.leave.leave_project')}
            <Icon icon="i-mdi-exit-run"/>
          </Button>
          <ConfirmModal bind:this={leaveModal}
                        title={$t('project_page.leave.confirm_title')}
                        submitText={$t('project_page.leave.leave_action')}
                        submitIcon="i-mdi-exit-run"
                        submitVariant="btn-error"
                        cancelText={$t('project_page.leave.dont_leave')}>
            <p>{$t('project_page.leave.confirm_leave')}</p>
          </ConfirmModal>
        </div>
        <AdminContent>
          <div class="divider m-0" />
          <div class="flex gap-4 max-sm:flex-col-reverse">
            <button class="btn btn-accent" on:click={resetProject}>
              {$t('project_page.reset_project_modal.submit')}
              <CircleArrowIcon/>
            </button>
            <ResetProjectModal bind:this={resetProjectModal}/>
            <Modal bind:this={hgCommandResultModal} closeOnClickOutside={false}>
              <div class="card">
                <div class="card-body overflow-auto">
                  {#if hgCommandResponse === ''}
                    <span class="loading loading-ring loading-lg"></span>
                  {:else}
                    <pre>{hgCommandResponse}</pre>
                    {#if hgCommandRunning}
                      <span class="loading loading-dots loading-xs"></span>
                    {/if}
                  {/if}
                </div>
              </div>
            </Modal>
            <Button on:click={recover}>HG Recover</Button>
            <Button on:click={verify}>HG Verify</Button>
          </div>
        </AdminContent>
        <ConfirmDeleteModal bind:this={deleteProjectModal} i18nScope="delete_project_modal"/>
      </MoreSettings>

    </div>
  </HeaderPage>
{/if}
