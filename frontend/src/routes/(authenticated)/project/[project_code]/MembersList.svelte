<script module lang="ts">
  import type { User } from './+page';
  import type { ProjectRole } from '$lib/gql/types';

  export type Member = {
    id: string;
    user: User;
    role: ProjectRole;
  };
</script>

<script lang="ts">
  import type { Snippet } from 'svelte';
  import t from '$lib/i18n';
  import { BadgeList, MemberBadge } from '$lib/components/Badges';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import AdminContent from '$lib/layout/AdminContent.svelte';
  import { Icon, TrashIcon } from '$lib/icons';
  import { Button } from '$lib/forms';
  import ChangeMemberRoleModal from './ChangeMemberRoleModal.svelte';
  import { useNotifications } from '$lib/notify';
  import type { UUID } from 'crypto';
  import PlainInput from '$lib/forms/PlainInput.svelte';
  import { DialogResponse } from '$lib/components/modals';
  import {formatProjectRole} from '$lib/components/Projects/FormatUserProjectRole.svelte';

  interface Props {
    members?: Member[];
    canManageMember: (member: Member) => boolean;
    canManageList: boolean;
    projectId: string;
    showObserver?: boolean;
    canViewOtherMembers: boolean;
    extraButtons?: Snippet;
    children?: Snippet;
    onOpenUserModal: (member: Member) => void;
    onDeleteProjectUser: (member: Member) => void;
  }

  const {
    members = [],
    canManageMember,
    canManageList,
    projectId,
    showObserver,
    canViewOtherMembers,
    extraButtons,
    children,
    onOpenUserModal,
    onDeleteProjectUser,
  }: Props = $props();

  const TRUNCATED_MEMBER_COUNT = 5;
  let showAllMembers = $state(false);

  let memberSearch = $state('');
  let filteredMembers: Member[] = $derived.by(() => {
    const search = memberSearch?.trim().toLowerCase();
    if (!search) {
      return members;
    } else {
      return members.filter(
        (m) =>
          m.user.name.toLowerCase().includes(search) ||
          m.user.email?.toLowerCase().includes(search) ||
          m.user.username?.toLowerCase().includes(search),
      );
    }
  });
  let showMembers = $derived(showAllMembers ? filteredMembers : filteredMembers.slice(0, TRUNCATED_MEMBER_COUNT));

  const { notifySuccess } = useNotifications();

  let changeMemberRoleModal: ChangeMemberRoleModal | undefined = $state();
  async function changeMemberRole(member: Member): Promise<void> {
    if (!member.user || !changeMemberRoleModal) return;
    const nameOrEmail = member.user.name ? member.user.name : (member.user.email ?? '');
    const { response, formState } = await changeMemberRoleModal.open({
      userId: member.user.id as UUID,
      name: nameOrEmail,
      role: member.role,
    });

    if (response === DialogResponse.Submit) {
      const notification = 'project_page.notifications.role_change';
      const role = formState.role.currentValue;
      notifySuccess(
        $t(notification, {
          name: member.user.name,
          role: formatProjectRole(role, $t),
        }),
      );
    }
  }
</script>

<div>
  <div class="text-2xl mb-4 flex items-baseline gap-4 max-sm:flex-col">
    <h2>
      {$t('project_page.members.title')}
      {#if !canViewOtherMembers}
        <span
          class="tooltip tooltip-warning text-warning shrink-0 leading-0"
          data-tip={$t('project_page.members.membership_confidential')}
        >
          <Icon icon="i-mdi-shield-lock-outline" size="text-xl" />
        </span>
      {/if}
    </h2>
    {#if members?.length > TRUNCATED_MEMBER_COUNT}
      <div class="form-control max-w-full w-96">
        <PlainInput placeholder={$t('project_page.members.filter_members_placeholder')} bind:value={memberSearch} />
      </div>
    {/if}
  </div>

  <BadgeList grid={showMembers.length > TRUNCATED_MEMBER_COUNT}>
    {#if !members.length}
      <span class="text-secondary mx-2 my-1">{$t('common.none')}</span>
    {:else if !showMembers.length}
      <span class="text-secondary mx-2 my-1">{$t('project_page.members.no_matching')}</span>
    {/if}

    {#each showMembers as member (member.id)}
      {@const canManage = canManageMember(member)}
      <Dropdown disabled={!canManage}>
        <MemberBadge member={{ name: member.user.name, role: member.role }} {canManage} />
        {#snippet content()}
          <ul class="menu">
            <AdminContent>
              <li>
                <button onclick={() => onOpenUserModal(member)}>
                  <Icon icon="i-mdi-card-account-details-outline" size="text-2xl" />
                  {$t('project_page.view_user_details')}
                </button>
              </li>
            </AdminContent>
            <li>
              <button onclick={() => changeMemberRole(member)}>
                <span class="i-mdi-account-lock text-2xl"></span>
                {$t('project_page.change_role')}
              </button>
            </li>
            <li>
              <button class="text-error" onclick={() => onDeleteProjectUser(member)}>
                <TrashIcon />
                {$t('project_page.remove_user')}
              </button>
            </li>
          </ul>
        {/snippet}
      </Dropdown>
    {/each}

    {#if members.length > TRUNCATED_MEMBER_COUNT}
      <div class="justify-self-start">
        <Button outline size="btn-sm" onclick={() => (showAllMembers = !showAllMembers)}>
          {showAllMembers ? $t('project_page.members.show_less') : $t('project_page.members.show_all')}
        </Button>
      </div>
    {/if}

    {#if canManageList}
      <div class="flex grow flex-wrap place-self-end gap-3 place-content-end" style="grid-column: -2 / -1">
        {@render extraButtons?.()}
      </div>
    {/if}

    {@render children?.()}
  </BadgeList>
  <ChangeMemberRoleModal {projectId} bind:this={changeMemberRoleModal} {showObserver} />
</div>
