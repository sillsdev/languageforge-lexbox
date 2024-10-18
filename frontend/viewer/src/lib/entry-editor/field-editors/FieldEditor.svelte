<script lang="ts">
  import type { MultiString } from '../../mini-lcm';
  import type {FieldConfig, OptionFieldConfig} from '../../config-types';
  import SingleFieldEditor from './SingleFieldEditor.svelte';
  import MultiFieldEditor from './MultiFieldEditor.svelte';

  export let value: unknown;
  export let field: FieldConfig;
  export let readonly: boolean;

  // having a single state object lets us do type predicates on the value and field at once
  // we just have to make sure they stay in sync
  const state = {value, field};
  $: syncToState(value);
  function syncToState(_: unknown): void {
    if (state.value !== value) state.value = value;
  }
  $: syncToValue(state.value);
  function syncToValue(_: unknown): void {
    if (state.value !== value) value = state.value;
  }

  function isMultiString(value: unknown): value is MultiString {
    return field.type === 'multi';
  }
  function isSingleString(value: unknown): value is string {
    return field.type === 'single';
  }
  function isSingleOption(state: {value: unknown, field: FieldConfig}): state is {value: string, field: FieldConfig & OptionFieldConfig} {
    return field.type === 'option';
  }
  function isMultiOption(state: {value: unknown, field: FieldConfig}): state is {value: string[], field: FieldConfig & OptionFieldConfig} {
    return field.type === 'multi-option';
  }
</script>

{#if isMultiString(value)}
  <MultiFieldEditor on:change id={field.id} wsType={field.ws} bind:value {readonly} />
{:else if isSingleString(value)}
  <SingleFieldEditor on:change id={field.id} wsType={field.ws} bind:value {readonly} />
{:else if isSingleOption(state)}
  <!-- <SingleOptionEditor on:change id={state.field.id} wsType={state.field.ws} bind:value={state.value} {readonly} /> -->
{:else if isMultiOption(state)}
  <!-- <MultiOptionEditor on:change id={state.field.id} wsType={state.field.ws} bind:value={state.value} {readonly} /> -->
{/if}

