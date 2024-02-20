<script lang="ts">
  import {FormatProjectType, ProjectTypeIcon} from '$lib/components/ProjectType';
  import { ProjectType } from '$lib/gql/types';
  import t from '$lib/i18n';
  import Select from './Select.svelte';

  export let value: ProjectType | undefined;
  export let error: string | string[] | undefined = undefined;
  export let undefinedOptionLabel: string | undefined = undefined;
let types = [
  ProjectType.FlEx,
  ProjectType.WeSay,
  ProjectType.OneStoryEditor,
  ProjectType.OurWord,
  ProjectType.AdaptIt
];
</script>
<div class="relative">
  <Select id="type" label={$t('project_type.type')} bind:value {error} on:change>
    {#if undefinedOptionLabel}
      <option value={undefined}>{undefinedOptionLabel}</option>
    {/if}
    {#each types as type}
      <option value={type}><FormatProjectType {type}/></option>
    {/each}
   </Select>
  <span class="absolute right-10 top-11 pointer-events-none">
    <ProjectTypeIcon type={value} size="h-8" />
  </span>
</div>
