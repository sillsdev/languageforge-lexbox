<script lang="ts">
  import type { ComponentProps } from 'svelte';
  import CrdtField from './CrdtField.svelte';
  import { SelectField, TextField, type MenuOption } from 'svelte-ux';

  export let value: string | undefined;
  export let unsavedChanges = false;
  export let options: MenuOption[] | undefined = undefined;
  export let label: string | undefined = undefined;
  export let labelPlacement: ComponentProps<TextField>['labelPlacement'] = undefined;
  export let placeholder: string | undefined = undefined;
  export let readonly: boolean | undefined = undefined;
  let append: HTMLElement;
</script>

<CrdtField on:change bind:value bind:unsavedChanges let:editorValue let:onEditorValueChange viewMergeButtonPortal={append}>
  {#key options}
  <SelectField
    on:change={(e) => onEditorValueChange(e.detail.value, true)}
    value={editorValue}
    disabled={readonly}
    {options}
    clearSearchOnOpen={false}
    clearable={false}
    search={() => Promise.resolve()}
    class="ws-field"
    classes={{ root: `${editorValue ? '' : 'empty'} ${readonly ? 'readonly' : ''}`, field: 'field-container' }}
    {label}
    {labelPlacement}
    {placeholder}>
    <span bind:this={append} slot="append" />
  </SelectField>
  {/key}
</CrdtField>

<style>
  :global(.unresolved-merge .field-container) {
    @apply !border-warning;
  }
</style>
