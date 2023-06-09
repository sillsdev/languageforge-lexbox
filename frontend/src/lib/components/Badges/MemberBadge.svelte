<script lang="ts">
  import FormatUserProjectRole from '../FormatUserProjectRole.svelte';
  import Badge from './Badge.svelte';
  export let member: { name: string; role: number };
  export let canManage = false;
</script>

<button on:click disabled={!canManage} class="button-badge">
  <span class="badge badge-lg badge-primary p-4 pr-0 parent-badge">
    <span class="pr-2 whitespace-nowrap">
      {member.name}
    </span>

    <Badge>
      <FormatUserProjectRole projectRole={member.role} />
    </Badge>

    {#if canManage}
      <span class="i-mdi-dots-vertical text-lg ml-1 mr-3" />
    {/if}
  </span>
</button>

<style>
  .parent-badge {
    border-right: none;
  }
</style>
