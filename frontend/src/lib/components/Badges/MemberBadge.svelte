<script lang="ts">
  import { OrgRole, ProjectRole } from '$lib/gql/types';
  import FormatUserRole from '../FormatUserRole.svelte';
  import ActionBadge from './ActionBadge.svelte';
  import Badge from './Badge.svelte';
  export let member: { name: string; role: ProjectRole | OrgRole };
  export let canManage = false;

  export let type: 'existing' | 'new' = 'existing';
  $: actionIcon = (type === 'existing' ? 'i-mdi-dots-vertical' as const : 'i-mdi-close' as const);

  $: variant = member.role === ProjectRole.Manager || member.role === OrgRole.Admin ? 'btn-primary' as const : 'btn-secondary' as const;
</script>

<ActionBadge {actionIcon} {variant} disabled={!canManage} on:action>
  <span class="pr-3 whitespace-nowrap overflow-ellipsis overflow-x-clip" title={member.name}>
    {member.name}
  </span>

  <!-- justify the name left and the role right -->
  <span class="flex-grow" />

  <Badge>
    <FormatUserRole projectRole={member.role} />
  </Badge>
</ActionBadge>
