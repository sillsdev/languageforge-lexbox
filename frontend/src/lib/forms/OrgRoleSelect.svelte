<script lang="ts">
  import { OrgRole } from '$lib/gql/types';
  import t from '$lib/i18n';

  export let id = 'role';
  export let value: OrgRole;
  export let verbose = false;
</script>

<select {id} bind:value class="select select-bordered" on:change>
  <option value={OrgRole.User}>
    {$t(verbose ? 'org_role.user_description' : 'org_role.user')}
  </option>
  <option value={OrgRole.Admin}>
    {$t(verbose ? 'org_role.admin_description' : 'org_role.admin')}
  </option>
</select>
