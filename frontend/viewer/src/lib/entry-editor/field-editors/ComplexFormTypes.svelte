<script lang="ts">
  import {useComplexFormTypes} from '../../complex-form-types';
  import type {ComplexFormType} from '../../mini-lcm';
  import {pickBestAlternative} from '../../utils';
  import {useWritingSystems} from '../../writing-systems';
  import MultiOptionEditor from './MultiOptionEditor.svelte';

  export let id: string;
  export let value: ComplexFormType[];
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
