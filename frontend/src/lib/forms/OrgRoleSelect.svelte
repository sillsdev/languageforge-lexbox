<script lang="ts">
  import { OrgRole } from '$lib/gql/types';
  import t from '$lib/i18n';
  import Select from './Select.svelte';

  export let id = 'role';
  export let value: OrgRole;
  export let error: string | string[] | undefined = undefined;
</script>

<Select {id} bind:value label={$t('org_role.label')} {error} on:change>
  <option value={OrgRole.User}>
    {$t('org_role.user_description')}
  </option>
  <option value={OrgRole.Admin}>
    {$t('org_role.admin_description')}
  </option>
</Select>
