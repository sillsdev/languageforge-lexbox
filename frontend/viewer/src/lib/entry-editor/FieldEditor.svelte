<script lang="ts">
  import type { MultiString } from '../mini-lcm';
  import type { FieldConfig, OptionFieldConfig } from '../config-types';
  import MultiOptionEditor from './MultiOptionEditor.svelte';
  import SingleOptionEditor from './SingleOptionEditor.svelte';
  import SingleFieldEditor from './SingleFieldEditor.svelte';
  import MultiFieldEditor from './MultiFieldEditor.svelte';

  type T = $$Generic<unknown>;

  export let value: unknown;
  export let field: FieldConfig;

  $: state = {value, field};

  function isMultiString(value: unknown): value is MultiString {
    return field.type === 'multi';
  }
  function isSingleString(value: unknown): value is string {
    return field.type === 'single';
  }
  function isSingleOption(state: {value: unknown, field: FieldConfig}): state is {value: string, field: FieldConfig & OptionFieldConfig} {
    return field.type === 'option';
  }
  function isMultiOption(value: unknown): value is string[] {
    return field.type === 'multi-option';
  }
</script>

{#if isMultiString(value)}
  <MultiFieldEditor on:change {field} bind:value />
{:else if isSingleString(value)}
  <SingleFieldEditor on:change {field} bind:value />
{:else if isSingleOption(state)}
  <SingleOptionEditor on:change field={state.field} bind:value={state.value} />
{:else if isMultiOption(value)}
  <MultiOptionEditor on:change {field} bind:value />
{/if}

<style global>
  @import './field.postcss';
</style>
