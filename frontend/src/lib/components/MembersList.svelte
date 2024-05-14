<script context="module" lang="ts">
  import type { User } from './Users/UserModal.svelte'; // TODO: Mode this somewhere shared
  import type { ProjectRole } from '$lib/gql/types';

  export type Member = {
    id: string
    user: User
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

  export let members: Member[] = [];
  export let canManageMember: (member: Member) => boolean;
  export let openUserModal: (member: Member) => Promise<void>;
  export let changeMemberRole: (member: Member) => Promise<void>;
  export let deleteProjectUser: (member: Member) => Promise<void>;

  const DEFAULT_TRUNCATED_MEMBER_COUNT = 5;
  export let truncatedMemberCount = DEFAULT_TRUNCATED_MEMBER_COUNT;
  let showAllMembers = false;
  $: showMembers = showAllMembers ? members : members.slice(0, truncatedMemberCount);

</script>

<div>
  <p class="text-2xl mb-4">
    {$t('project_page.members.title')}
  </p>

  <BadgeList grid={showMembers.length > DEFAULT_TRUNCATED_MEMBER_COUNT}>
    {#each showMembers as member}
      {@const canManage = canManageMember(member)}
      <Dropdown disabled={!canManage}>
        <MemberBadge member={{ name: member.user.name, role: member.role }} canManage={canManage} />
        <ul slot="content" class="menu">
          <AdminContent>
            <li>
              <button on:click={() => openUserModal(member)}>
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

    {#if members.length > DEFAULT_TRUNCATED_MEMBER_COUNT}
      <div class="justify-self-start">
        <Button outline size="btn-sm" on:click={() => (showAllMembers = !showAllMembers)}>
          {showAllMembers ? $t('project_page.members.show_less') : $t('project_page.members.show_all')}
        </Button>
      </div>
    {/if}

    {#if $$slots.extraButtons}
      <div class="flex grow flex-wrap place-self-end gap-3 place-content-end" style="grid-column: -2 / -1">
        <slot name="extraButtons" />
      </div>
    {/if}
    <slot />

  </BadgeList>
</div>
