<script lang="ts">
  import t from '$lib/i18n';
  import { Badge, BadgeList } from '$lib/components/Badges';
  import type { Organization } from '$lib/gql/types';

  type Org = Pick<Organization, 'id' | 'name'>;
  export let organizations: Org[] = [];

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
      <Badge>
        {org.name}
      </Badge>
    {/each}
  </BadgeList>
</div>
