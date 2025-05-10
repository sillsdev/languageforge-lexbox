<script lang="ts">
  import { randomFormId } from './utils';
  import FormField from './FormField.svelte';

  interface Props {
    label: string;
    description?: string;
    value?: string;
    id?: string;
    autofocus?: boolean;
    readonly?: boolean;
    error?: string | string[];
    placeholder?: string;
  }

  let {
    label,
    description,
    value = $bindable(),
    id = randomFormId(),
    autofocus = false,
    readonly = false,
    error,
    placeholder = '',
  }: Props = $props();
</script>

<!-- https://daisyui.com/components/input -->
<FormField {id} {error} {label} {autofocus} {description}>
  <!-- svelte-ignore a11y_autofocus -->
  <textarea
    {id}
    bind:value
    class="textarea textarea-bordered h-24"
    class:textarea-error={error}
    {placeholder}
    {readonly}
    {autofocus}
  ></textarea>
</FormField>
