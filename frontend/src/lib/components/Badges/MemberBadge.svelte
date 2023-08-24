<script lang="ts">
  import type { ProjectRole } from '$lib/gql/types';
  import FormatUserProjectRole from '../FormatUserProjectRole.svelte';
  import Badge from './Badge.svelte';
  export let member: { name: string; role: ProjectRole };
  export let canManage = false;
</script>

<!-- svelte-ignore a11y-no-noninteractive-tabindex -->
<!-- svelte-ignore a11y-label-has-associated-control -->
<!-- https://daisyui.com/components/dropdown/#method-2-using-label-and-css-focus -->
<label tabindex={canManage ? -1 : null} class="button-badge group transition">
  <span class="badge badge-lg badge-primary p-4 pr-0 parent-badge">
    <span class="pr-3 whitespace-nowrap">
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
</label>

<style>
  .parent-badge {
    border-right: none;
  }
</style>
