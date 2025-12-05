<script lang="ts">
  import {Badge, BadgeButton, BadgeList} from '$lib/components/Badges';
  import EditableText from '$lib/components/EditableText.svelte';
  import {ProjectTypeBadge} from '$lib/components/ProjectType';
  import FormatRetentionPolicy from '$lib/components/FormatRetentionPolicy.svelte';
  import HgLogView from '$lib/components/HgLogView.svelte';
  import DeleteModal from '$lib/components/modals/DeleteModal.svelte';
  import t, {date, number} from '$lib/i18n';
  import {z} from 'zod';
  import type {PageData} from './$types';
  import {
    _changeProjectDescription,
    _changeProjectName,
    _deleteProjectUser,
    _leaveProject,
    _removeProjectFromOrg,
    _updateFLExModelVersion,
    _updateProjectLanguageList,
    _updateProjectLexEntryCount,
    _updateProjectRepoSizeInKb,
    type ProjectUser,
  } from './+page';
  import AddProjectMember from './AddProjectMember.svelte';
  import BulkAddProjectMembers from './BulkAddProjectMembers.svelte';
  import {CircleArrowIcon, TrashIcon} from '$lib/icons';
  import {useNotifications} from '$lib/notify';
  import {DialogResponse, Modal} from '$lib/components/modals';
  import {Button, type ErrorMessage} from '$lib/forms';
  import ResetProjectModal from './ResetProjectModal.svelte';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import ConfirmDeleteModal from '$lib/components/modals/ConfirmDeleteModal.svelte';
  import {_deleteProject} from '$lib/gql/mutations';
  import {goto} from '$app/navigation';
  import MoreSettings from '$lib/components/MoreSettings.svelte';
  import {AdminContent, FeatureFlagContent, PageBreadcrumb} from '$lib/layout';
  import Markdown from 'svelte-exmarkdown';
  import {OrgRole, ProjectRole, ProjectType, ResetStatus, RetentionPolicy} from '$lib/gql/generated/graphql';
  import Icon from '$lib/icons/Icon.svelte';
  import OpenInFlexModal from './OpenInFlexModal.svelte';
  import OpenInFlexButton from './OpenInFlexButton.svelte';
  import SendReceiveUrlField from './SendReceiveUrlField.svelte';
  import UserModal from '$lib/components/Users/UserModal.svelte';
  import IconButton from '$lib/components/IconButton.svelte';
  import ConfirmModal from '$lib/components/modals/ConfirmModal.svelte';
  import ProjectConfidentialityBadge from './ProjectConfidentialityBadge.svelte';
  import ProjectConfidentialityModal from './ProjectConfidentialityModal.svelte';
  import {DetailItem, EditableDetailItem} from '$lib/layout';
  import MembersList from './MembersList.svelte';
  import DetailsPage from '$lib/layout/DetailsPage.svelte';
  import OrgList from './OrgList.svelte';
  import AddOrganization from './AddOrganization.svelte';
  import AddPurpose from './AddPurpose.svelte';
  import WritingSystemList from '$lib/components/Projects/WritingSystemList.svelte';
  import {onMount} from 'svelte';
  import {getSearchParamValues} from '$lib/util/query-params';
  import FlexModelVersionText from '$lib/components/Projects/FlexModelVersionText.svelte';
  import CrdtSyncButton from './CrdtSyncButton.svelte';
  import {_askToJoinProject} from '../create/+page';
// TODO: Should we duplicate this function in the project_code/+page.ts file, rather than importing it from elsewhere?
  import {Duration} from '$lib/util/time';
  import {hasFeatureFlag} from '$lib/user';
  import {resolve} from '$app/paths';
  import {NewTabLinkMarkdown} from '$lib/components/Markdown';

  interface Props {
    data: PageData;
  }

  const { data }: Props = $props();
  let user = $derived(data.user);
  let projectStore = $derived(data.project);
  let project = $derived($projectStore);
  let changesetStore = $derived(data.changesets);
  // TODO: Once we've stabilized the lastCommit issue with project reset, get rid of the `$changesetStore.fetching` part
  // and just let this logic be `project?.lastCommit == null`
  const isEmpty: boolean = $derived(
    !$changesetStore.fetching ? $changesetStore.changesets.length === 0 : project?.lastCommit == null,
  );
  let members = $derived(
    project.users.sort((a, b) => {
      if (a.role !== b.role) {
        return a.role === ProjectRole.Manager ? -1 : 1;
      }
      return a.user.name.localeCompare(b.user.name);
    }),
  );

  let lexEntryCount = $derived(project.flexProjectMetadata?.lexEntryCount);
  let flexModelVersion = $derived(project.flexProjectMetadata?.flexModelVersion);
  let vernacularLangTags = $derived(project.flexProjectMetadata?.writingSystems?.vernacularWss);
  let analysisLangTags = $derived(project.flexProjectMetadata?.writingSystems?.analysisWss);

  const { notifySuccess, notifyWarning } = useNotifications();

  type ProjectPageQueryParams = {
    addUserId: string;
    addUserName: string;
  };

  onMount(() => {
    // query params not available during SSR
    const urlValues = getSearchParamValues<ProjectPageQueryParams>();
    if (urlValues.addUserId && urlValues.addUserName && addProjectMember) {
      void addProjectMember.openModal(urlValues.addUserId, urlValues.addUserName);
    }

    if (project && project.repoSizeInKb == null) {
      void updateRepoSize();
    }
  });

  let addProjectMember: AddProjectMember | undefined = $state();

  let userModal: UserModal | undefined = $state();

  let loadingRepoSize = $state(false);
  async function updateRepoSize(): Promise<void> {
    loadingRepoSize = true;
    await _updateProjectRepoSizeInKb(project.code);
    loadingRepoSize = false;
  }

  function sizeStrInMb(sizeInKb: number): string {
    return `${$number(sizeInKb / 1024, { maximumFractionDigits: 1 })} MB`;
  }

  let loadingEntryCount = $state(false);
  async function updateEntryCount(): Promise<void> {
    loadingEntryCount = true;
    await _updateProjectLexEntryCount(project.code);
    loadingEntryCount = false;
  }

  let loadingModelVersion = $state(false);
  async function updateModelVersion(): Promise<void> {
    loadingModelVersion = true;
    await _updateFLExModelVersion(project.code);
    loadingModelVersion = false;
  }

  let loadingLanguageList = $state(false);
  async function updateLanguageList(): Promise<void> {
    loadingLanguageList = true;
    await _updateProjectLanguageList(project.code);
    loadingLanguageList = false;
  }

  let orgRoles = $derived(
    project.organizations?.map((o) => user.orgs?.find((org) => org.orgId === o.id)?.role).filter((r) => !!r) ?? [],
  );
  let projectRole = $derived(project?.users.find((u) => u.user.id == user.id)?.role);
  let userIsOrgAdmin = $derived(orgRoles.some((role) => role === OrgRole.Admin));

  // Mirrors PermissionService.CanViewProjectMembers() in C#
  let canViewOtherMembers = $derived(
    user.isAdmin ||
      projectRole == ProjectRole.Manager ||
      (projectRole && !project.isConfidential) || // public by default for members (non-members shouldn't even be here)
      userIsOrgAdmin,
  );

  // Almost mirrors PermissionService.CanAskToJoinProject() in C#, but admins won't be shown the "ask to join" button
  let canAskToJoinProject = $derived(!user.isAdmin && !projectRole && orgRoles.some((_) => true));
  let canSyncProject = $derived(user.isAdmin || projectRole == ProjectRole.Manager || projectRole == ProjectRole.Editor || userIsOrgAdmin);

  let resetProjectModal: ResetProjectModal | undefined = $state();
  async function resetProject(): Promise<void> {
    await resetProjectModal?.open(project.code, project.resetStatus);
  }

  let removeUserModal: DeleteModal | undefined = $state();
  let userToDelete: ProjectUser | undefined = $state();
  async function deleteProjectUser(projectUser: ProjectUser): Promise<void> {
    if (!removeUserModal) return;
    userToDelete = projectUser;
    const deleted = await removeUserModal.prompt(async () => {
      const { error } = await _deleteProjectUser(project.id, projectUser.user.id);
      return error?.message;
    });
    if (deleted) {
      notifyWarning($t('project_page.notifications.user_delete', { name: projectUser.user.name }));
    }
  }

  let removeProjectFromOrgModal: DeleteModal | undefined = $state();
  let orgToRemove: string = $state('');
  async function removeProjectFromOrg(orgId: string, orgName: string): Promise<void> {
    if (!removeProjectFromOrgModal) return;
    orgToRemove = orgName;
    const removed = await removeProjectFromOrgModal.prompt(async () => {
      const { error } = await _removeProjectFromOrg(project.id, orgId);
      return error?.message;
    });
    if (removed) {
      notifyWarning($t('project_page.notifications.remove_project_from_org', { orgName: orgToRemove }));
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

  let userId = $derived(user.id);
  let orgsManagedByUser = $derived(user.orgs.filter((o) => o.role === OrgRole.Admin).map((o) => o.orgId));
  let canManage = $derived(
    user.isAdmin ||
      project?.users.find((u) => u.user.id == userId)?.role == ProjectRole.Manager ||
      !!project?.organizations?.find((o) => orgsManagedByUser.includes(o.id)),
  );

  const projectNameValidation = z.string().trim().min(1, $t('project_page.project_name_empty_error'));

  let deleteProjectModal: ConfirmDeleteModal | undefined = $state();

  async function softDeleteProject(): Promise<void> {
    if (!deleteProjectModal) return;
    projectStore.pause();
    changesetStore.pause();
    let deleted = false;
    try {
      const result = await deleteProjectModal.open(project.name, async () => {
        const { error } = await _deleteProject(project.id);
        return error?.message;
      });
      if (result.response === DialogResponse.Submit) {
        deleted = true;
        notifyWarning($t('delete_project_modal.success', { name: project.name, code: project.code }));
        await goto(resolve(data.home));
      }
    } finally {
      if (!deleted) {
        projectStore.resume();
        changesetStore.resume();
      }
    }
  }

  let hgCommandResultModal: Modal | undefined = $state();
  let hgCommandResponse = $state('');
  let hgCommandRunning = $state(false);

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
      ({ done, value } = await reader.read());
      // The {stream: true} is important here; without it, in theory the output could be
      // broken in the middle of a UTF-8 byte sequence and get garbled. But with stream: true,
      // TextDecoder() will know to expect more output, so if the stream returns a partial
      // UTF-8 sequence, TextDecoder() will return everything except that sequence and wait
      // for more bytes before decoding that sequence.
      if (value) hgCommandResponse += decoder.decode(value, { stream: true });
    }
  }

  async function hgCommand(execute: () => Promise<Response>): Promise<void> {
    if (!hgCommandResultModal) return;
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

  let askLoading = $state(false);
  async function askToJoinProject(projectId: string, projectName: string): Promise<void> {
    askLoading = true;
    const joinResult = await _askToJoinProject(projectId);
    askLoading = false;
    if (!joinResult.error) {
      notifySuccess($t('project.create.join_request_sent', { projectName }), Duration.Persistent);
    }
    if (joinResult.error?.byType('ProjectHasNoManagers')) {
      notifyWarning($t('project.create.join_request_error_no_managers', { projectName }), Duration.Persistent);
    }
  }

  let projectConfidentialityModal: ProjectConfidentialityModal | undefined = $state();
  let openInFlexModal: OpenInFlexModal | undefined = $state();
  let leaveModal: ConfirmModal | undefined = $state();

  async function leaveProject(): Promise<void> {
    if (!leaveModal) return;
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
        notifySuccess($t('project_page.leave.leave_success', { projectName: project.name }));
        await goto(resolve(data.home));
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
  <DetailsPage wide titleText={project.name}>
    {#snippet actions()}
      {#if project.type === ProjectType.FlEx}
        <FeatureFlagContent flag="FwLiteBeta">
          <CrdtSyncButton {project} {isEmpty} canManageProject={canManage} />
        </FeatureFlagContent>
      {/if}
      {#if canAskToJoinProject}
        <Button variant="btn-primary" loading={askLoading} onclick={() => askToJoinProject(project.id, project.name)}>
          {#if !askLoading}
            <span class="i-mdi-email text-2xl"></span>
          {/if}
          {$t('project_page.join_project.label')}
        </Button>
      {:else if canSyncProject}
        {#if project && project.type === ProjectType.FlEx && !isEmpty}
          <OpenInFlexModal bind:this={openInFlexModal} {project}/>
          <OpenInFlexButton projectId={project.id} onclick={openInFlexModal?.open}/>
        {:else}
          <Dropdown>
            <button class="btn btn-primary">
              {$t('project_page.get_project.label', {isEmpty: isEmpty.toString()})}
              <span class="i-mdi-dots-vertical text-2xl"></span>
            </button>
            {#snippet content()}
              <div class="card w-[calc(100vw-1rem)] sm:max-w-[35rem]">
                <div class="card-body max-sm:p-4">
                  <div class="prose">
                    <h3>
                      {$t('project_page.get_project.instructions_header', {
                        type: project.type,
                        mode: 'normal',
                        isEmpty: isEmpty.toString(),
                      })}
                    </h3>
                    {#if project.type === ProjectType.WeSay}
                      {#if isEmpty}
                        <NewTabLinkMarkdown
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
                    {:else if isEmpty}
                      <NewTabLinkMarkdown
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
                  </div>
                  <SendReceiveUrlField projectCode={project.code}/>
                </div>
              </div>
            {/snippet}
          </Dropdown>
        {/if}
      {/if}
    {/snippet}
    {#snippet title()}
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
    {/snippet}
    {#snippet headerContent()}
      <BadgeList>
        <ProjectConfidentialityBadge
          onclick={projectConfidentialityModal?.openModal}
          {canManage}
          isConfidential={project.isConfidential ?? undefined}
        />
        <ProjectTypeBadge type={project.type} />
        {#if project.retentionPolicy === RetentionPolicy.Unknown}
          {#if canManage}
            <AddPurpose projectId={project.id} />
          {/if}
        {:else}
          <Badge>
            <FormatRetentionPolicy policy={project.retentionPolicy} />
          </Badge>
        {/if}
        {#if project.resetStatus === ResetStatus.InProgress}
          <button
            class:tooltip={user.isAdmin}
            data-tip={$t('project_page.reset_project_modal.click_to_continue')}
            disabled={!user.isAdmin}
            onclick={resetProject}
          >
            <Badge variant="badge-warning">
              {$t('project_page.reset_project_modal.reset_in_progress')}
              <span class="i-mdi-warning text-xl mb-[-2px]"></span>
            </Badge>
          </button>
        {/if}
        {#if project.hasHarmonyCommits}
          <Badge>
            {$t('project_page.using_fw_lite')}
          </Badge>
        {/if}
      </BadgeList>
      <ProjectConfidentialityModal
        bind:this={projectConfidentialityModal}
        projectId={project.id}
        isConfidential={project.isConfidential ?? undefined}
      />
    {/snippet}
    {#snippet details()}
      <DetailItem title={$t('project_page.project_code')} text={project.code} copyToClipboard={true} />
      <DetailItem title={$t('project_page.created_at')} text={$date(project.createdDate)} />
      <DetailItem title={$t('project_page.last_commit')} text={$date(project.lastCommit)} />
      <DetailItem
        title={$t('project_page.repo_size')}
        loading={loadingRepoSize}
        text={sizeStrInMb(project.repoSizeInKb ?? 0)}
      />
      {#if project.type === ProjectType.FlEx || project.type === ProjectType.WeSay}
        <DetailItem title={$t('project_page.num_entries')} text={$number(lexEntryCount)}>
          {#snippet extras()}
            <AdminContent>
              <IconButton
                loading={loadingEntryCount}
                icon="i-mdi-refresh"
                size="btn-sm"
                variant="btn-ghost"
                outline={false}
                onclick={updateEntryCount}
              />
            </AdminContent>
          {/snippet}
        </DetailItem>
      {/if}
      {#if project.type === ProjectType.FlEx}
        <DetailItem title={$t('project_page.model_version')}>
          <FlexModelVersionText modelVersion={flexModelVersion ?? 0} />
          {#snippet extras()}
            <AdminContent>
              <IconButton
                loading={loadingModelVersion}
                icon="i-mdi-refresh"
                size="btn-sm"
                variant="btn-ghost"
                outline={false}
                onclick={updateModelVersion}
              />
            </AdminContent>
          {/snippet}
        </DetailItem>
      {/if}
      {#if project.type === ProjectType.FlEx}
        <div class="space-y-2">
          <DetailItem title={$t('project_page.vernacular_langs')} wrap>
            <AdminContent>
              <IconButton
                loading={loadingLanguageList}
                icon="i-mdi-refresh"
                size="btn-sm"
                variant="btn-ghost"
                outline={false}
                onclick={updateLanguageList}
              />
            </AdminContent>
            <WritingSystemList writingSystems={vernacularLangTags} />
          </DetailItem>
          <DetailItem title={$t('project_page.analysis_langs')} wrap>
            <AdminContent>
              <IconButton
                loading={loadingLanguageList}
                icon="i-mdi-refresh"
                size="btn-sm"
                variant="btn-ghost"
                outline={false}
                onclick={updateLanguageList}
              />
            </AdminContent>
            <WritingSystemList writingSystems={analysisLangTags} />
          </DetailItem>
        </div>
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
    {/snippet}

    <div class="space-y-4">
      <OrgList
        {canManage}
        organizations={project.organizations}
        onRemoveProjectFromOrg={(org) => removeProjectFromOrg(org.orgId, org.orgName)}
      >
        {#snippet extraButtons()}
          {#if canManage}
            <AddOrganization projectId={project.id} userIsAdmin={user.isAdmin} />
          {/if}
        {/snippet}
        <DeleteModal
          bind:this={removeProjectFromOrgModal}
          entityName={$t('project_page.remove_project_from_org_title')}
          isRemoveDialog
        >
          {$t('project_page.confirm_remove_org', { orgName: orgToRemove })}
        </DeleteModal>
      </OrgList>
      <MembersList
        projectId={project.id}
        showObserver={project.type === ProjectType.FlEx && hasFeatureFlag(user, 'FwLiteBeta')}
        {members}
        canManageMember={(member) => canManage && (member.user?.id !== userId || user.isAdmin)}
        canManageList={canManage}
        {canViewOtherMembers}
        onOpenUserModal={(member) => userModal?.open(member.user)}
        onDeleteProjectUser={deleteProjectUser}
      >
        {#snippet extraButtons()}
          <BadgeButton
            variant="badge-success"
            icon="i-mdi-account-plus-outline"
            onclick={() => addProjectMember?.openModal(undefined, undefined)}
          >
            {$t('project_page.add_user.add_button')}
          </BadgeButton>

          <AddProjectMember bind:this={addProjectMember} {project} />
          <BulkAddProjectMembers projectId={project.id} />
        {/snippet}
        <UserModal bind:this={userModal} />

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
      <div class="divider"></div>
      <div class="space-y-2">
        <p class="text-2xl mb-4 flex gap-4 items-baseline">
          {$t('project_page.history')}
          <a class="btn btn-sm btn-outline btn-info" href={resolve(`/hg/${project.code}`)} target="_blank">
            {$t('project_page.hg.open_in_hgweb')}<span class="i-mdi-open-in-new text-2xl"></span>
          </a>
        </p>

        <div class="max-h-[75vh] overflow-auto border-b border-base-200">
          <HgLogView
            logEntries={$changesetStore.changesets}
            loading={$changesetStore.fetching}
            projectCode={project.code}
          />
        </div>
      </div>

      <div class="divider"></div>

      <MoreSettings column>
        <div class="flex gap-4 max-sm:flex-col-reverse">
          {#if canManage}
            <button class="btn btn-error" onclick={softDeleteProject}>
              {$t('delete_project_modal.submit')}
              <TrashIcon />
            </button>
            <Button outline variant="btn-warning" onclick={projectConfidentialityModal?.openModal}>
              {$t('project.confidential.set_confidentiality')}
              <Icon icon="i-mdi-shield-lock-outline" />
            </Button>
          {/if}
          <Button outline variant="btn-error" onclick={leaveProject}>
            {$t('project_page.leave.leave_project')}
            <Icon icon="i-mdi-exit-run" />
          </Button>
          <ConfirmModal
            bind:this={leaveModal}
            title={$t('project_page.leave.confirm_title')}
            submitText={$t('project_page.leave.leave_action')}
            submitIcon="i-mdi-exit-run"
            submitVariant="btn-error"
            cancelText={$t('project_page.leave.dont_leave')}
          >
            <p>{$t('project_page.leave.confirm_leave')}</p>
          </ConfirmModal>
        </div>
        <AdminContent>
          <div class="divider m-0"></div>
          <div class="flex gap-4 max-sm:flex-col-reverse">
            <button class="btn btn-accent" onclick={resetProject}>
              {$t('project_page.reset_project_modal.submit')}
              <CircleArrowIcon />
            </button>
            <ResetProjectModal bind:this={resetProjectModal} />
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
            <Button onclick={recover}>HG Recover</Button>
            <Button onclick={verify}>HG Verify</Button>
          </div>
        </AdminContent>
        <ConfirmDeleteModal bind:this={deleteProjectModal} i18nScope="delete_project_modal" />
      </MoreSettings>
    </div>
  </DetailsPage>
{/if}
