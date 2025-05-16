<script lang="ts">
  import type { IMultiString } from '$lib/dotnet-types';
  import type {FieldConfig, OptionFieldConfig} from '../../config-types';
  import {watch} from 'runed';

  let {
    value = $bindable(),
    field,
    // readonly = false,
  }: {
    value: unknown;
    field: FieldConfig;
    // readonly?: boolean;
  } = $props();

  // having a single state object lets us do type predicates on the value and field at once
  // we just have to make sure they stay in sync
  const state = $state({value, field});
  watch(() => value, () => {
    if (state.value !== value) state.value = value;
  });
  watch(() => state.value, () => {
    if (state.value !== value) value = state.value;
  });

  function isMultiString(value: unknown): value is IMultiString {
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
  <!-- <MultiWsInput on:change id={field.id} wsType={field.ws} bind:value {readonly} /> -->
{:else if isSingleString(value)}
  <!-- <WsInput on:change id={field.id} wsType={field.ws} bind:value {readonly} /> -->
{:else if isSingleOption(state)}
  <!-- <Select on:change id={state.field.id} wsType={state.field.ws} bind:value={state.value} {readonly} /> -->
{:else if isMultiOption(state)}
  <!-- <MultiSelect on:change id={state.field.id} wsType={state.field.ws} bind:value={state.value} {readonly} /> -->
{/if}

