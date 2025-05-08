<script lang="ts">
  import { ProjectRole } from '$lib/gql/types';
  import FormatUserProjectRole from '../Projects/FormatUserProjectRole.svelte';
  import ActionBadge from './ActionBadge.svelte';
  import Badge from './Badge.svelte';

  interface Props {
    member: { name: string; role: ProjectRole };
    canManage?: boolean;
    type?: 'existing' | 'new';
  }

  const { member, canManage = false, type = 'existing' }: Props = $props();
  let actionIcon = ($derived(type === 'existing' ? 'i-mdi-dots-vertical' as const : 'i-mdi-close' as const));
  let variant = $derived(member.role === ProjectRole.Manager ? 'btn-primary' as const : 'btn-secondary' as const);
</script>

<ActionBadge {actionIcon} {variant} disabled={!canManage} on:action>
  <span class="pr-3 whitespace-nowrap overflow-ellipsis overflow-x-clip" title={member.name}>
    {member.name}
  </span>

  <!-- justify the name left and the role right -->
  <span class="flex-grow"></span>

  <Badge>
    <FormatUserProjectRole role={member.role} />
  </Badge>
</ActionBadge>
