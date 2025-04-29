<script lang="ts">
  import { ProjectRole } from '$lib/gql/types';
  import t from '$lib/i18n';

  import { Select } from '.';

  interface Props {
    id?: string;
    value: ProjectRole;
    error: string[] | undefined;
  }

  let { id = 'role', value = $bindable(), error }: Props = $props();
</script>

<Select {id} bind:value label={$t('project_role.label')} {error}>
  <option value={ProjectRole.Editor}>
    {$t('project_role.editor_description')}
  </option>
  <option value={ProjectRole.Manager}>
    {$t('project_role.manager_description')}
  </option>
</Select>
