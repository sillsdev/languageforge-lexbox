<script lang="ts">
  import type { ComponentProps } from 'svelte';
  import CrdtField from './CrdtField.svelte';
  import { TextField, type MenuOption, MultiSelectField } from 'svelte-ux';

  export let value: any[];

  export let unsavedChanges = false;
  export let options: MenuOption[] = [];
  export let label: string | undefined = undefined;
  export let labelPlacement: ComponentProps<TextField>['labelPlacement'] = undefined;
  export let placeholder: string | undefined = undefined;
  export let readonly: boolean | undefined = undefined;
  let append: HTMLElement;

  function asOption(value: any): MenuOption {
    if (!(typeof value === 'object' && 'label' in value && 'value' in value)) {
      throw new Error('Invalid option');
    }
    return value;
  }

  function asOptions(values: any[]): MenuOption[] {
    return values?.map(asOption) ?? [];
  }
</script>

<CrdtField on:change bind:value bind:unsavedChanges let:editorValue let:onEditorValueChange viewMergeButtonPortal={append}>
  <MultiSelectField
    on:change={(e) => {
      console.log(e);
      onEditorValueChange(e.detail.value, true);
    }}
    value={editorValue}
    disabled={readonly}
    {options}
    valueProp="value"
    labelProp="label"
    formatSelected={({ options }) =>
      options.map((o) => o.label).join(", ") || "None"}
    infiniteScroll
    clearSearchOnOpen={false}
    clearable={false}
    class="ws-field"
    classes={{ root: `${editorValue ? '' : 'empty'} ${readonly ? 'readonly' : ''}`, field: 'field-container' }}
    {label}
    {labelPlacement}
    {placeholder}>
    <span bind:this={append} slot="append" />
  </MultiSelectField>
</CrdtField>

<style lang="postcss">
  :global(.unresolved-merge .field-container) {
    @apply !border-warning;
  }
</style>
