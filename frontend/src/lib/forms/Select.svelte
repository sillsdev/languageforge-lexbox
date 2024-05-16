<script lang="ts">
  import type { HelpLink } from '$lib/components/help';
  import FormField from './FormField.svelte';
  import { randomFormId } from './utils';

  export let label: string;
  export let value: string | undefined;
  export let id = randomFormId();
  export let autofocus = false;
  export let error: string | string[] | undefined = undefined;
  export let disabled = false;
  export let helpLink: HelpLink | undefined = undefined;
</script>

<FormField {id} {label} {error} {autofocus} {helpLink}>
  <!-- svelte-ignore a11y-autofocus -->
  <select {disabled} bind:value {id} class="select select-bordered" {autofocus} on:change>
    <slot />
  </select>
</FormField>
