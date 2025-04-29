<script lang="ts">
  import { OrgRole } from '$lib/gql/types';
  import t from '$lib/i18n';
  import Select from './Select.svelte';

  interface Props {
    id?: string;
    value: OrgRole;
    error?: string | string[] | undefined;
  }

  let { id = 'role', value = $bindable(), error = undefined }: Props = $props();
</script>

<Select {id} bind:value label={$t('org_role.label')} {error} on:change>
  <option value={OrgRole.User}>
    {$t('org_role.user_description')}
  </option>
  <option value={OrgRole.Admin}>
    {$t('org_role.admin_description')}
  </option>
</Select>
