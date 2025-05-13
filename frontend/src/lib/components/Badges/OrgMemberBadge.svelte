<script lang="ts">
  import { OrgRole } from '$lib/gql/types';
  import FormatUserOrgRole from '../Orgs/FormatUserOrgRole.svelte';
  import ActionBadge from './ActionBadge.svelte';
  import Badge from './Badge.svelte';

  interface Props {
    member: { name: string; role: OrgRole };
    canManage?: boolean;
    type?: 'existing' | 'new';
  }

  let { member, canManage = false, type = 'existing' }: Props = $props();
  let actionIcon = ($derived(type === 'existing' ? 'i-mdi-dots-vertical' as const : 'i-mdi-close' as const));
  let variant = $derived(member.role === OrgRole.Admin ? 'btn-primary' as const : 'btn-secondary' as const);
</script>

<ActionBadge {actionIcon} {variant} disabled={!canManage} on:action>
  <span class="pr-3 whitespace-nowrap overflow-ellipsis overflow-x-clip" title={member.name}>
    {member.name}
  </span>

  <!-- justify the name left and the role right -->
  <span class="flex-grow"></span>

  <Badge>
    <FormatUserOrgRole role={member.role} />
  </Badge>
</ActionBadge>
