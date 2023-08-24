<script lang="ts">
  import { ProjectRole } from '$lib/gql/types';
  import t from '$lib/i18n';

  import { Select } from '.';

  export let id = 'role';
  export let value: ProjectRole;
  export let error: string[] | undefined;
</script>

<Select {id} bind:value label={$t('project_role.label')} {error}>
  <option value={ProjectRole.Editor}>
    {$t('project_role.editor_description')}
  </option>
  <option value={ProjectRole.Manager}>
    {$t('project_role.manager_description')}
  </option>
</Select>
