<script lang="ts">
  import type { OrgRole, ProjectRole } from '$lib/gql/types';
  import DevContent from '$lib/layout/DevContent.svelte';

  import OrgRoleSelect from './OrgRoleSelect.svelte';
  import ProjectRoleSelect from './ProjectRoleSelect.svelte';

  export let id = 'role';
  export let type: 'project' | 'org';
  export let value: ProjectRole | OrgRole;
  export let error: string[] | undefined;

  function isProjectRole(value: ProjectRole | OrgRole): value is ProjectRole {
    return type === 'project';
  }

  function isOrgRole(value: ProjectRole | OrgRole): value is OrgRole {
    return type === 'org';
  }
</script>

{#if isProjectRole(value)}
<ProjectRoleSelect {id} bind:value {error} />
{:else if isOrgRole(value)}
<OrgRoleSelect {id} bind:value {error} />
{:else}
<!-- Should never happen, so warn in dev mode so we see it and adjust the code -->
<DevContent>
  <p class="text-error">Invalid MemberRoleSelect, should be either 'project' or 'org'</p>
</DevContent>
{/if}
