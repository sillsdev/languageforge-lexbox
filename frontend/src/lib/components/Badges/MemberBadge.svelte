<script lang="ts">
  import { ProjectRole } from '$lib/gql/types';
  import FormatUserProjectRole from '../FormatUserProjectRole.svelte';
  import ActionBadge from './ActionBadge.svelte';
  import Badge from './Badge.svelte';
  export let member: { name: string; role: ProjectRole };
  export let canManage = false;

  $: variant = member.role === ProjectRole.Manager ? 'btn-primary' as const : 'btn-secondary' as const;
</script>

<ActionBadge actionIcon="i-mdi-dots-vertical" {variant} disabled={!canManage}>
  <span class="pr-3 whitespace-nowrap overflow-ellipsis overflow-hidden" title={member.name}>
    {member.name}
  </span>

  <!-- justify the name left and the role right -->
  <span class="flex-grow" />

  <Badge>
    <FormatUserProjectRole projectRole={member.role} />
  </Badge>
</ActionBadge>
