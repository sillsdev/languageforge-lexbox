<script lang="ts">
  import { UserRole } from '$lib/gql/types';
  import t from '$lib/i18n';

  import { Select } from '.';

  interface Props {
    id?: string;
    value: UserRole;
    error: string[] | undefined;
    disabled?: boolean;
  }

  let {
    id = 'role',
    value = $bindable(),
    error,
    disabled = false
  }: Props = $props();
</script>

<div class="contents" class:text-accent={value === UserRole.Admin}>
  <Select {id} bind:value label={$t('system_role.label')} {error} {disabled}>
    <option value={UserRole.User} class="text-base-content">
      {$t('system_role.user_description')}
    </option>
    <option value={UserRole.Admin} class="text-accent">
      {$t('system_role.admin_description')}
    </option>
  </Select>
</div>
