<script lang="ts">
  import { UserRole } from '$lib/gql/types';
  import t from '$lib/i18n';

  import { Select } from '.';

  export let id = 'role';
  export let value: UserRole;
  export let error: string[] | undefined;
</script>

<div class="contents" class:text-accent={value === UserRole.Admin}>
  <Select {id} bind:value label={$t('system_role.label')} {error}>
    <option value={UserRole.User} class="text-base-content">
      {$t('system_role.user_description')}
    </option>
    <option value={UserRole.Admin} class="text-accent">
      {$t('system_role.admin_description')}
    </option>
  </Select>
</div>
