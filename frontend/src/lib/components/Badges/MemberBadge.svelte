<script lang="ts">
  import { ProjectRole } from '$lib/gql/types';
  import FormatUserProjectRole from '../Projects/FormatUserProjectRole.svelte';
  import ActionBadge from './ActionBadge.svelte';
  import Badge from './Badge.svelte';

  interface Props {
    member: { name: string; role: ProjectRole };
    canManage?: boolean;
    onAction?: () => void;
    type?: 'existing' | 'new';
  }

  const { member, canManage = false, onAction, type = 'existing' }: Props = $props();
  let actionIcon = $derived(type === 'existing' ? ('i-mdi-dots-vertical' as const) : ('i-mdi-close' as const));
  let variant = $derived(member.role === ProjectRole.Manager ? ('btn-primary' as const) : ('btn-secondary' as const));
</script>

<ActionBadge {actionIcon} {variant} disabled={!canManage} {onAction}>
  <!-- x-ellipsis, not overflow-x-clip: `overflow: hidden` is what lets the name shrink below its own
  width, so a long name truncates instead of shoving the role badge and ⋮ menu out of the badge -->
  <span class="pr-3 whitespace-nowrap x-ellipsis" title={member.name}>
    {member.name}
  </span>

  <!-- justify the name left and the role right -->
  <span class="flex-grow"></span>

  <Badge>
    <FormatUserProjectRole role={member.role} />
  </Badge>
</ActionBadge>
