<script lang="ts">
  import { ProjectRole } from '$lib/gql/types';
  import FormatUserProjectRole from '../Projects/FormatUserProjectRole.svelte';
  import ActionBadge from './ActionBadge.svelte';
  import Badge from './Badge.svelte';
  export let member: { name: string; role: ProjectRole };
  export let canManage = false;

  export let type: 'existing' | 'new' = 'existing';
  $: actionIcon = (type === 'existing' ? 'i-mdi-dots-vertical' as const : 'i-mdi-close' as const);
  $: variant = member.role === ProjectRole.Manager ? 'btn-primary' as const : 'btn-secondary' as const;
</script>

<ActionBadge {actionIcon} {variant} disabled={!canManage} on:action>
  <span class="pr-3 whitespace-nowrap overflow-ellipsis overflow-x-clip" title={member.name}>
    {member.name}
  </span>

  <!-- justify the name left and the role right -->
  <span class="flex-grow" />

  <Badge>
    <FormatUserProjectRole role={member.role} />
  </Badge>
</ActionBadge>
