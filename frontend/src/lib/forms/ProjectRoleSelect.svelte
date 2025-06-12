<script lang="ts">
  import { ProjectRole } from '$lib/gql/types';
  import t from '$lib/i18n';

  import Select, { type SelectProps } from './Select.svelte';

  interface Props extends Omit<SelectProps, 'label'> {
    id?: string;
    value: ProjectRole;
    showObserver?: boolean;
  }

  let { id = 'role', value = $bindable(), showObserver = false, ...rest }: Props = $props();
</script>

<Select {...rest} {id} bind:value label={$t('project_role.label')}>
  <option value={ProjectRole.Editor}>
    {$t('project_role.editor_description')}
  </option>
  <option value={ProjectRole.Manager}>
    {$t('project_role.manager_description')}
  </option>
  {#if showObserver}
    <option value={ProjectRole.Observer}>
      {$t('project_role.observer_description')}
    </option>
  {/if}
</Select>
