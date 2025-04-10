<script lang="ts">
  import {useComplexFormTypes} from '$lib/complex-form-types';
  import type {IComplexFormType} from '$lib/dotnet-types';
  import {useWritingSystemService} from '../../writing-system-service.svelte';
  import MultiOptionEditor from './MultiOptionEditor.svelte';

  export let id: string;
  export let value: IComplexFormType[];
  export let readonly: boolean;

  const complexFormTypes = useComplexFormTypes();
  const writingSystemService = useWritingSystemService();
</script>

<MultiOptionEditor
  on:change
  bind:value
  preserveOrder
  options={$complexFormTypes}
  getOptionLabel={(cft) => writingSystemService.pickBestAlternative(cft.name, 'analysis')}
  {readonly}
  {id}
  wsType="first-analysis" />
