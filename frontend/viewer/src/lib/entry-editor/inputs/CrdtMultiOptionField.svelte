<script lang="ts">
  import type { ComponentProps } from 'svelte';
  import CrdtField from './CrdtField.svelte';
  import { TextField, type MenuOption, MultiSelectField } from 'svelte-ux';
  import type {OptionFieldValue} from '../../config-types';
  import {mdiMagnify} from '@mdi/js';

  export let value: OptionFieldValue[];

  export let unsavedChanges = false;
  export let options: MenuOption[] = [];
  export let label: string | undefined = undefined;
  export let labelPlacement: ComponentProps<TextField>['labelPlacement'] = undefined;
  export let placeholder: string | undefined = undefined;
  export let readonly: boolean | undefined = undefined;
  let append: HTMLElement;


  function asMultiSelectValues(values: any[]): string[] {
    return values?.map(v => v.id) ?? [];
  }
  function asObjectValues(values: string[]) {
    return values.map(v => ({id: v}));
  }
</script>

<CrdtField on:change bind:value bind:unsavedChanges let:editorValue let:onEditorValueChange viewMergeButtonPortal={append}>
  <MultiSelectField
    on:change={(e) => {
      onEditorValueChange(asObjectValues(e.detail.value), true);
    }}
    value={asMultiSelectValues(editorValue)}
    disabled={readonly}
    {options}
    icon={mdiMagnify}
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
