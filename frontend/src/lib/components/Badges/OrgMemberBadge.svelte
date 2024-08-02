<script lang="ts">
  import { OrgRole } from '$lib/gql/types';
  import FormatUserOrgRole from '../Orgs/FormatUserOrgRole.svelte';
  import ActionBadge from './ActionBadge.svelte';
  import Badge from './Badge.svelte';
  export let member: { name: string; role: OrgRole };
  export let canManage = false;

  export let type: 'existing' | 'new' = 'existing';
  $: actionIcon = (type === 'existing' ? 'i-mdi-dots-vertical' as const : 'i-mdi-close' as const);
  $: variant = member.role === OrgRole.Admin ? 'btn-primary' as const : 'btn-secondary' as const;
</script>

<ActionBadge {actionIcon} {variant} disabled={!canManage} on:action>
  <span class="pr-3 whitespace-nowrap overflow-ellipsis overflow-x-clip" title={member.name}>
    {member.name}
  </span>

  <!-- justify the name left and the role right -->
  <span class="flex-grow" />

  <Badge>
    <FormatUserOrgRole role={member.role} />
  </Badge>
</ActionBadge>
