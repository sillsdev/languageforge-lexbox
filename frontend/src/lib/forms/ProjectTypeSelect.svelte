<script lang="ts">
  import {FormatProjectType, ProjectTypeIcon} from '$lib/components/ProjectType';
  import { ProjectType } from '$lib/gql/types';
  import t from '$lib/i18n';
  import Select from './Select.svelte';

  export let value: ProjectType | undefined;
  export let error: string | string[] | undefined = undefined;
  export let undefinedOptionLabel: string | undefined = undefined;
  export let includeUnknown = false;
  const types = [
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
    {#if includeUnknown}
      <option value={ProjectType.Unknown}><FormatProjectType type={ProjectType.Unknown}/></option>
    {/if}
  </Select>
  <span class="absolute right-10 top-[3.75rem] -translate-y-1/2 pointer-events-none leading-0">
    <ProjectTypeIcon type={value} size="h-8" />
  </span>
</div>
