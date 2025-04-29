<script lang="ts">
  import type { Snippet } from 'svelte';
  import { createBubbler } from 'svelte/legacy';

  const bubble = createBubbler();
  import type { HelpLink } from '$lib/components/help';
  import FormField from './FormField.svelte';
  import { randomFormId } from './utils';

  interface Props {
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
  }: Props = $props();
</script>

<FormField {id} {label} {error} {autofocus} {helpLink}>
  <!-- svelte-ignore a11y_autofocus -->
  <select {disabled} bind:value {id} class="select select-bordered" {autofocus} onchange={bubble('change')}>
    {@render children?.()}
  </select>
</FormField>
