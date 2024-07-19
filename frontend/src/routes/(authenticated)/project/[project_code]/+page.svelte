<script lang="ts">
  import { Badge, BadgeList } from '$lib/components/Badges';
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
    _removeProjectFromOrg,
    type ProjectUser,
  } from './+page';
  import AddProjectMember from './AddProjectMember.svelte';
  import BulkAddProjectMembers from './BulkAddProjectMembers.svelte';
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
  import { AdminContent, PageBreadcrumb } from '$lib/layout';
  import Markdown from 'svelte-exmarkdown';
  import { OrgRole, ProjectRole, ProjectType, ResetStatus } from '$lib/gql/generated/graphql';
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
  import { DetailItem, EditableDetailItem } from '$lib/layout';
  import MembersList from './MembersList.svelte';
  import DetailsPage from '$lib/layout/DetailsPage.svelte';
  import OrgList from './OrgList.svelte';
  import AddOrganization from './AddOrganization.svelte';

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

  const { notifySuccess, notifyWarning } = useNotifications();

  let userModal: UserModal;

  let loadingEntryCount = false;
  async function updateEntryCount(): Promise<void> {
    loadingEntryCount = true;
    const response = await fetch(`/api/project/updateLexEntryCount/${project.code}`, {method: 'POST'});
    lexEntryCount = await response.text();
    loadingEntryCount = false;
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

  let removeProjectFromOrgModal: DeleteModal;
  // let orgToRemove: string | undefined;
  async function removeProjectFromOrg(orgId: string): Promise<void> {
    // orgToRemove = orgId;
    const removed = await removeProjectFromOrgModal.prompt(async () => {
      const { error } = await _removeProjectFromOrg(project.id, orgId);
      return error?.message;
    });
    if (removed) {
      notifyWarning('Your project has successfully been removed from the organization.');
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
  $: orgsManagedByUser = user.orgs.filter(o => o.role === OrgRole.Admin).map(o => o.orgId);
  $: canManage = user.isAdmin || project?.users.find((u) => u.user.id == userId)?.role == ProjectRole.Manager || !!project?.organizations.find((o) => orgsManagedByUser.includes(o.id));

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
  <DetailsPage wide title={project.name}>
    <svelte:fragment slot="actions">
      {#if project.type === ProjectType.FlEx && $isDev}
        {#if project.isLanguageForgeProject}
          <a href="./{project.code}/viewer" target="_blank" class="btn btn-neutral text-[#DCA54C] flex items-center gap-2">
            {$t('project_page.open_with_viewer')}
            <span class="i-mdi-dictionary text-2xl" />
          </a>
        {/if}
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
    <svelte:fragment slot="details">
      <DetailItem title={$t('project_page.project_code')} text={project.code} copyToClipboard={true} />
      <DetailItem title={$t('project_page.created_at')} text={$date(project.createdDate)} />
      <DetailItem title={$t('project_page.last_commit')} text={$date(project.lastCommit)} />
      {#if project.type === ProjectType.FlEx || project.type === ProjectType.WeSay}
        <DetailItem title={$t('project_page.num_entries')} text={$number(lexEntryCount)}>
          <AdminContent slot="extras">
            <IconButton
              loading={loadingEntryCount}
              icon="i-mdi-refresh"
              size="btn-sm"
              variant="btn-ghost"
              outline={false}
              on:click={updateEntryCount}
            />
          </AdminContent>
        </DetailItem>
      {/if}
      <div>
        <EditableDetailItem
          title={$t('project_page.description')}
          value={project.description}
          disabled={!canManage}
          saveHandler={updateProjectDescription}
          placeholder={$t('project_page.add_description')}
          multiline
        />
      </div>
    </svelte:fragment>

    <div class="space-y-4">
      <OrgList
        organizations={project.organizations}
        on:removeProjectFromOrg={(event) => removeProjectFromOrg(event.detail)}
      >
        <svelte:fragment slot="extraButtons">
          {#if canManage}
            <AddOrganization projectId={project.id} userIsAdmin={user.isAdmin} />
          {/if}
        </svelte:fragment>
        <DeleteModal
            bind:this={removeProjectFromOrgModal}
            entityName={'Project'}
            isRemoveDialog
          >
          {'Would you like to remove your project from the organization, {orgId}'}
        </DeleteModal>
      </OrgList>

      <MembersList
        projectId={project.id}
        {members}
        canManageMember={(member) => canManage && (member.user?.id !== userId || user.isAdmin)}
        canManageList={canManage}
        on:openUserModal={(event) => userModal.open(event.detail.user)}
        on:deleteProjectUser={(event) => deleteProjectUser(event.detail)}
        >
          <svelte:fragment slot="extraButtons">
            <AddProjectMember projectId={project.id} />
            <BulkAddProjectMembers projectId={project.id} />
          </svelte:fragment>
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
      </MembersList>

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
  </DetailsPage>
{/if}
