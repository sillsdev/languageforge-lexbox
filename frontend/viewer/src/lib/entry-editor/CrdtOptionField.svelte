<script lang="ts">
  import type { ComponentProps } from 'svelte';
  import CrdtField from './CrdtField.svelte';
  import { SelectField, TextField, type MenuOption } from 'svelte-ux';

  export let value: string;
  export let unsavedChanges = false;
  export let label: string | undefined = undefined;
  export let labelPlacement: ComponentProps<TextField>['labelPlacement'] = undefined;
  export let placeholder: string | undefined = undefined;
  let append: HTMLElement;

  let demoOptions: MenuOption[] | undefined;
  $: demoOptions = demoOptions ?? [{label: value, value: value}, {label: 'Another option', value: 'Another option'}];
</script>

<CrdtField on:change bind:value bind:unsavedChanges let:editorValue let:save let:onEditorValueChange viewMergeButtonPortal={append}>
  <SelectField
    on:change={(e) => onEditorValueChange(e.detail.value)}
    on:blur={save}
    value={editorValue}
    options={demoOptions ?? []}
    clearSearchOnOpen={false}
    clearable={false}
    search={() => Promise.resolve()}
    class="ws-field"
    classes={{ root: editorValue ? '' : 'empty', field: 'field-container' }}
    {label}
    {labelPlacement}
    {placeholder}>
    <span bind:this={append} slot="append" />
  </SelectField>
</CrdtField>

<style>
  :global(.unresolved-merge .field-container) {
    @apply !border-warning;
  }
</style>
