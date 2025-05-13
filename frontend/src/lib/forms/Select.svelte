<script lang="ts">
  import type { Snippet } from 'svelte';
  import type { HelpLink } from '$lib/components/help';
  import FormField from './FormField.svelte';
  import { randomFormId } from './utils';

  export interface SelectProps {
    label: string;
    value: string | undefined;
    id?: string;
    autofocus?: boolean;
    error?: string | string[] | undefined;
    disabled?: boolean;
    helpLink?: HelpLink | undefined;
    children?: Snippet;
  }

  let {
    label,
    value = $bindable(),
    id = randomFormId(),
    autofocus = false,
    error = undefined,
    disabled = false,
    helpLink = undefined,
    children,
  }: SelectProps = $props();
</script>

<FormField {id} {label} {error} {autofocus} {helpLink}>
  <!-- svelte-ignore a11y_autofocus -->
  <select {disabled} bind:value {id} class="select select-bordered" {autofocus}>
    {@render children?.()}
  </select>
</FormField>
