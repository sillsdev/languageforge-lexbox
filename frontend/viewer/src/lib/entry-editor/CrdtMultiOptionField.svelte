<script lang="ts">
  import type { ComponentProps } from 'svelte';
  import CrdtField from './CrdtField.svelte';
  import { TextField, type MenuOption, MultiSelectField } from 'svelte-ux';

  export let value: string[];
  let stringValue: string;
  $: {
    if (!stringValue) {
      stringValue = value.join(',');
    } else {
      value = stringValue.split(',');
    }
  }
  export let unsavedChanges = false;
  export let label: string | undefined = undefined;
  export let labelPlacement: ComponentProps<TextField>['labelPlacement'] = undefined;
  export let placeholder: string | undefined = undefined;
  let append: HTMLElement;

  let demoOptions: MenuOption[] | undefined;
  $: demoOptions = demoOptions ?? [...value.map(v => ({label: v, value: v})), {label: 'Another option', value: 'Another option'}];

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

<CrdtField on:change bind:value={stringValue} bind:unsavedChanges let:editorValue let:save let:onEditorValueChange viewMergeButtonPortal={append}>
  <MultiSelectField
    on:change={(e) => onEditorValueChange(asOptions(e.detail.value).map((o) => o.value).join(','))}
    on:blur={save}
    value={editorValue.split(',')}
    options={demoOptions ?? []}
    valueProp="value"
    labelProp="label"
    formatSelected={({ options }) =>
      options.map((o) => o.label).join(", ") || "None"}
    clearSearchOnOpen={false}
    clearable={false}
    search={() => Promise.resolve()}
    class="ws-field"
    classes={{ root: editorValue ? '' : 'empty', field: 'field-container' }}
    {label}
    {labelPlacement}
    {placeholder}>
    <span bind:this={append} slot="append" />
  </MultiSelectField>
</CrdtField>

<style>
  :global(.unresolved-merge .field-container) {
    @apply !border-warning;
  }
</style>
