<script lang="ts">
  import type { ProjectRole } from '$lib/gql/types';
  import FormatUserProjectRole from '../FormatUserProjectRole.svelte';
  import ActionBadge from './ActionBadge.svelte';
  import Badge from './Badge.svelte';
  export let member: { name: string; role: ProjectRole };
  export let canManage = false;
</script>

<ActionBadge actionIcon="i-mdi-dots-vertical" variant="badge-primary" disabled={!canManage}>
  <span class="pr-3 whitespace-nowrap">
    {member.name}
  </span>

  <Badge>
    <FormatUserProjectRole projectRole={member.role} />
  </Badge>
</ActionBadge>
