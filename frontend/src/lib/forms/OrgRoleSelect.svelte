<script lang="ts">
  import { OrgRole } from '$lib/gql/types';
  import t from '$lib/i18n';
  import Select, { type SelectProps } from './Select.svelte';

  interface Props extends SelectProps {
    id?: string;
    value: OrgRole;
  }

  let { id = 'role', value = $bindable(), ...rest }: Props = $props();
</script>

<Select {...rest} {id} bind:value label={$t('org_role.label')}>
  <option value={OrgRole.User}>
    {$t('org_role.user_description')}
  </option>
  <option value={OrgRole.Admin}>
    {$t('org_role.admin_description')}
  </option>
</Select>
