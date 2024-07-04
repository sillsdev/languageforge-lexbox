<script context="module" lang="ts">
  // export type Org = {
  //   id: string
  //   name: string
  // };
</script>

<script lang="ts">
  import t from '$lib/i18n';
  import { Badge, BadgeList } from '$lib/components/Badges';
  import type { Organization } from '$lib/gql/types';

  export let organizations: Partial<Organization>[] = [];

  const TRUNCATED_MEMBER_COUNT = 5;
</script>


<div>
  <p class="text-2xl mb-4 flex items-baseline gap-4 max-sm:flex-col">
    {'Organizations'}
  </p>

  <BadgeList grid={organizations.length > TRUNCATED_MEMBER_COUNT}>
    {#if !organizations.length}
      <span class="text-secondary mx-2 my-1">{$t('common.none')}</span>
    {/if}
    {#each organizations as org (org.id)}
      <Badge>
        {org.name}
      </Badge>
    {/each}
  </BadgeList>
</div>
