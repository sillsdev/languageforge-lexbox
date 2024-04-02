<script lang="ts">
  import type { ComponentProps } from 'svelte';
  import CrdtField from './CrdtField.svelte';
  import { TextField } from 'svelte-ux';

  export let value: string;
  export let unsavedChanges = false;
  export let label: string | undefined = undefined;
  export let labelPlacement: ComponentProps<TextField>['labelPlacement'] = undefined;
  export let placeholder: string | undefined = undefined;
  let append: HTMLElement;
</script>

<CrdtField on:change bind:value bind:unsavedChanges let:editorValue let:save let:onEditorValueChange viewMergeButtonPortal={append}>
  <TextField
    on:change={(e) => onEditorValueChange(e.detail.inputValue)}
    on:blur={save}
    value={editorValue}
    class="ws-field"
    classes={{ root: editorValue ? '' : 'empty', input: 'field-input', container: 'field-container' }}
    {label}
    {labelPlacement}
    {placeholder}>
    <span bind:this={append} slot="append" />
  </TextField>
</CrdtField>

<style>
  :global(.unresolved-merge .field-container) {
    @apply !border-warning;
  }
</style>
