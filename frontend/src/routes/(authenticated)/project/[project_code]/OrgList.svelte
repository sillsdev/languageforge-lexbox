<script lang="ts">
  import {resolve} from '$app/paths';
  import type {Snippet} from 'svelte';
  import t from '$lib/i18n';
  import {Badge, BadgeList} from '$lib/components/Badges';
  import type {Organization} from '$lib/gql/types';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import {Icon, TrashIcon} from '$lib/icons';
  import ActionBadge from '$lib/components/Badges/ActionBadge.svelte';

  type Org = Pick<Organization, 'id' | 'name'>;
  interface Props {
    canManage: boolean;
    organizations?: Org[];
    extraButtons?: Snippet;
    children?: Snippet;
    onRemoveProjectFromOrg?: (org: { orgId: string; orgName: string }) => void;
  }

  const { canManage, organizations = [], extraButtons, children, onRemoveProjectFromOrg }: Props = $props();

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
        {@render extraButtons?.()}
      </div>
    {/if}
    {#each organizations as org (org.id)}
      {#if !canManage}
        <Badge>
          {org.name}
        </Badge>
      {:else}
        <Dropdown>
          <ActionBadge actionIcon="i-mdi-dots-vertical">
            <span class="pr-3 whitespace-nowrap overflow-ellipsis overflow-x-clip" title={org.name}>
              {org.name}
            </span>
          </ActionBadge>
          {#snippet content()}
            <ul class="menu">
              <li>
                <a href={resolve(`/org/${org.id}`)}>
                  <Icon icon="i-mdi-link" />
                  {$t('project_page.view_org', { orgName: org.name })}
                </a>
              </li>
              <li>
                <button
                  class="text-error"
                  onclick={() => onRemoveProjectFromOrg?.({ orgId: org.id, orgName: org.name })}
                >
                  <TrashIcon />
                  {$t('project_page.remove_project_from_org')}
                </button>
              </li>
            </ul>
          {/snippet}
        </Dropdown>
      {/if}
    {/each}
  </BadgeList>

  {@render children?.()}
</div>
