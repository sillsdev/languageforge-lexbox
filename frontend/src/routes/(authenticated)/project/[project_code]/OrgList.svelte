<script lang="ts">
  import t from '$lib/i18n';
  import { Badge, BadgeList } from '$lib/components/Badges';
  import type { Organization } from '$lib/gql/types';
  import { createEventDispatcher } from 'svelte';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import { TrashIcon } from '$lib/icons';
  import ActionBadge from '$lib/components/Badges/ActionBadge.svelte';

  type Org = Pick<Organization, 'id' | 'name'>;
  export let canManage: boolean;
  export let organizations: Org[] = [];

  const dispatch = createEventDispatcher<{
    removeProjectFromOrg: { orgId: string; orgName: string };
  }>();

  const TRUNCATED_MEMBER_COUNT = 5;
</script>


<div>
  <p class="text-2xl mb-4 flex items-baseline gap-4 max-sm:flex-col">
    {$t('project_page.organization.title')}
  </p>

  <BadgeList grid={organizations.length > TRUNCATED_MEMBER_COUNT}>
    {#if !organizations.length}
      <span class="text-secondary mx-2 my-1">{$t('common.none')}</span>
      <div class="flex grow flex-wrap place-self-end gap-3 place-content-end" style="grid-column: -2 / -1">
        <slot name="extraButtons" />
      </div>
    {/if}
    {#each organizations as org (org.id)}
      {#if !canManage}
        <Badge>
          {org.name}
        </Badge>
      {:else}
        <Dropdown>
          <ActionBadge actionIcon="i-mdi-dots-vertical" on:action>
            <span class="pr-3 whitespace-nowrap overflow-ellipsis overflow-x-clip" title={org.name}>
              {org.name}
            </span>
          </ActionBadge>
          <ul slot="content" class="menu">
            <li>
              <a class="link" href={`/org/${org.id}`}>
                {$t('project_page.go_to_org', {orgName: org.name})}
              </a>
            </li>
            <li>
              <button class="text-error" on:click={() => dispatch('removeProjectFromOrg', {orgId: org.id, orgName: org.name})}>
                <TrashIcon />
                {$t('project_page.remove_project_from_org')}
              </button>
            </li>
          </ul>
        </Dropdown>
      {/if}
    {/each}
  </BadgeList>

  <slot />
</div>
