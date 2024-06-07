<script context="module" lang="ts">
  export type Member = {
    id: string
    user: { id: string; name: string; email?: string | null, username?: string | null}
    role: ProjectRole
  };
</script>

<script lang="ts">
  import t from '$lib/i18n';
  import { BadgeList, MemberBadge } from '$lib/components/Badges';
  import Dropdown from './Dropdown.svelte';
  import AdminContent from '$lib/layout/AdminContent.svelte';
  import { Icon, TrashIcon } from '$lib/icons';
  import { Button } from '$lib/forms';
  import type { ProjectRole } from '$lib/gql/types';
  import { createEventDispatcher } from 'svelte';
  import ChangeMemberRoleModal from '../../routes/(authenticated)/project/[project_code]/ChangeMemberRoleModal.svelte';
  import { DialogResponse } from './modals';
  import { useNotifications } from '$lib/notify';
  import type { UUID } from 'crypto';
  import PlainInput from '$lib/forms/PlainInput.svelte';

  export let members: Member[] = [];
  export let canManageMember: (member: Member) => boolean;
  export let canManageList: boolean;
  export let projectId: string;

  const dispatch = createEventDispatcher<{
    openUserModal: Member;
    deleteProjectUser: Member;
  }>();

  const TRUNCATED_MEMBER_COUNT = 5;
  let showAllMembers = false;

  let memberSearch = '';
  let filteredMembers: Member[] = members;
  $: {
    const search = memberSearch?.trim().toLowerCase();
    if (!search) {
      filteredMembers = members;
    } else {
      filteredMembers = members.filter((m) =>
        m.user.name.toLowerCase().includes(search) ||
        m.user.email?.toLowerCase().includes(search) ||
        m.user.username?.toLowerCase().includes(search));
    }
  }
  $: showMembers = showAllMembers ? filteredMembers : filteredMembers.slice(0, TRUNCATED_MEMBER_COUNT);

  const { notifySuccess } = useNotifications();

  let changeMemberRoleModal: ChangeMemberRoleModal;
  async function changeMemberRole(member: Member): Promise<void> {
    if (!member.user) return;
    const nameOrEmail = member.user.name ? member.user.name : member.user.email ?? '';
    console.log('About to change member role for', member);
    const { response, formState } = await changeMemberRoleModal.open({
      userId: member.user.id as UUID,
      name: nameOrEmail,
      role: member.role,
    });

    if (response === DialogResponse.Submit) {
      const notification = `project_page.notifications.role_change` as const;
      const role = formState.role.currentValue;
      notifySuccess(
        $t(notification, {
          name: member.user.name,
          role: role.toLowerCase(),
        }),
      );
    }
  }
</script>

<div>
  <p class="text-2xl mb-4 flex items-baseline gap-4 max-sm:flex-col">
    {$t('project_page.members.title')}
    {#if members?.length > TRUNCATED_MEMBER_COUNT}
      <div class="form-control max-w-full w-96">
        <PlainInput
          placeholder={$t('project_page.members.filter_members_placeholder')}
          bind:value={memberSearch} />
      </div>
    {/if}
  </p>

  <BadgeList grid={showMembers.length > TRUNCATED_MEMBER_COUNT}>

    {#if !members.length}
      <span class="text-secondary mx-2 my-1">{$t('common.none')}</span>
    {:else if !showMembers.length}
      <span class="text-secondary mx-2 my-1">{$t('project_page.members.no_matching')}</span>
    {/if}

    {#each showMembers as member (member.id)}
      {@const canManage = canManageMember(member)}
      <Dropdown disabled={!canManage}>
        <MemberBadge member={{ name: member.user.name, role: member.role }} canManage={canManage} />
        <ul slot="content" class="menu">
          <AdminContent>
            <li>
              <button on:click={() => dispatch('openUserModal', member)}>
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
            <button class="text-error" on:click={() => dispatch('deleteProjectUser', member)}>
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

    {#if canManageList}
      <div class="flex grow flex-wrap place-self-end gap-3 place-content-end" style="grid-column: -2 / -1">
        <slot name="extraButtons" />
      </div>
    {/if}
    <slot />

  </BadgeList>
  <ChangeMemberRoleModal {projectId} bind:this={changeMemberRoleModal} />
</div>
