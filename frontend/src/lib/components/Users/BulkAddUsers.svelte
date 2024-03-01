<script lang="ts">
  import { type UUID } from 'crypto';
  import { _bulkAddProjectMembers } from '../../../routes/(authenticated)/project/[project_code]/+page';
  import { ProjectRole } from '$lib/gql/types';
  import ProjectRoleSelect from '$lib/forms/ProjectRoleSelect.svelte';
  import Input from '$lib/forms/Input.svelte';
  import { hash } from '$lib/util/hash';

  export let projectId: UUID;
  let value: string = '';
  let role = ProjectRole.Editor;
  let roleError: string[] | undefined;
  let password: string = '';

  async function bulkAddRequest(): Promise<void> {
    const usernames = value.split('\n').map(s => s.trim());
    const passwordHash = await hash(password);
    const result = await _bulkAddProjectMembers({ projectId, passwordHash, role, usernames });
    console.log(result);
  }
</script>

<div>
  Bulk add users test
  <ProjectRoleSelect bind:value={role} bind:error={roleError} />
  <Input label="Shared password" type="password" bind:value={password} />
  <textarea bind:value />
  <button disabled={!password} class="btn btn-primary" type="button" on:click={bulkAddRequest}>Bulk add</button>
</div>
