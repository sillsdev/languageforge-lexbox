<script lang="ts">
  import { UserRole } from '$lib/gql/types';
  import t from '$lib/i18n';

  import Select, { type Props as SelectProps } from './Select.svelte';

  interface Props extends SelectProps {
    id?: string;
    value: UserRole;
  }

  let {
    id = 'role',
    value = $bindable(),
    ...rest
  }: Props = $props();
</script>

<div class="contents" class:text-accent={value === UserRole.Admin}>
  <Select {...rest} {id} bind:value label={$t('system_role.label')} >
    <option value={UserRole.User} class="text-base-content">
      {$t('system_role.user_description')}
    </option>
    <option value={UserRole.Admin} class="text-accent">
      {$t('system_role.admin_description')}
    </option>
  </Select>
</div>
