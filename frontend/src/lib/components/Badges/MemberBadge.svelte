<script lang="ts">
  import type { ProjectRole } from '$lib/gql/types';
  import FormatUserProjectRole from '../FormatUserProjectRole.svelte';
  import Badge from './Badge.svelte';
  export let member: { name: string; role: ProjectRole };
  export let canManage = false;
</script>

<button on:click disabled={!canManage} class="button-badge group transition">
  <span class="badge badge-lg badge-primary p-4 pr-0 parent-badge">
    <span class="pr-2 whitespace-nowrap">
      {member.name}
    </span>

    <Badge>
      <FormatUserProjectRole projectRole={member.role} />
    </Badge>

    {#if canManage}
    <span class="flex justify-center p-1 ml-1 mr-1 group-hover:bg-neutral/50 transition duration-300 rounded-full">
      <span class="i-mdi-dots-vertical text-lg" />
    </span>
    {/if}
  </span>
</button>

<style>
  .parent-badge {
    border-right: none;
  }
</style>
