<script lang="ts">
  import {useComplexFormTypes} from '$lib/complex-form-types';
  import type {IComplexFormType} from '$lib/dotnet-types';
  import {pickBestAlternative} from '../../utils';
  import {useWritingSystems} from '../../writing-systems';
  import MultiOptionEditor from './MultiOptionEditor.svelte';

  export let id: string;
  export let value: IComplexFormType[];
  export let readonly: boolean;

  const complexFormTypes = useComplexFormTypes();
  const writingSystems = useWritingSystems();
</script>

<MultiOptionEditor
  on:change
  bind:value
  preserveOrder
  options={$complexFormTypes}
  getOptionLabel={(cft) => pickBestAlternative(cft.name, 'analysis', $writingSystems)}
  {readonly}
  {id}
  wsType="first-analysis" />
