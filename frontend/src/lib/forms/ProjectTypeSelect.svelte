<script lang="ts">
  import { ProjectTypeIcon } from '$lib/components/ProjectType';
  import { ProjectType } from '$lib/gql/types';
  import t from '$lib/i18n';
  import Select from './Select.svelte';

  export let value: ProjectType | undefined;
  export let error: string | string[] | undefined = undefined;
  export let optionalLabel: string | undefined = undefined;

</script>
<div class="relative">
  <Select id="type" label={$t('project_type.type')} bind:value {error} on:change>
    {#if optionalLabel}
      <option value={undefined}>{optionalLabel}</option>
    {/if}
    <option value={ProjectType.FlEx}>{$t('project_type.flex')}</option>
    <option value={ProjectType.WeSay}>{$t('project_type.weSay')}</option>
    <option value={ProjectType.OneStoryEditor}>{$t('project_type.oneStoryEditor')}</option>
    <option value={ProjectType.OurWord}>{$t('project_type.ourWord')}</option>
  </Select>
  <span class="absolute right-10 top-11 pointer-events-none">
    <ProjectTypeIcon type={value} size="h-8" />
  </span>
</div>
