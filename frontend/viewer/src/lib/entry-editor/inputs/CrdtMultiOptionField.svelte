<script lang="ts">
  import {mdiMagnify} from '@mdi/js';
  import {createEventDispatcher, type ComponentProps} from 'svelte';
  import {MultiSelectField, type MenuOption, type TextField} from 'svelte-ux';
  import CrdtField from './CrdtField.svelte';

  const dispatch = createEventDispatcher<{
    change: { value: string[] }; // Generics aren't working properly in CrdtField, so we make the type excplicit here
  }>();

  export let value: string[];
  export let unsavedChanges = false;
  export let options: MenuOption<string>[];
  export let label: string | undefined = undefined;
  export let labelPlacement: ComponentProps<TextField>['labelPlacement'] = undefined;
  export let placeholder: string | undefined = undefined;
  export let readonly: boolean | undefined = undefined;
  export let ordered = false;
  let append: HTMLElement;

  $: sortedOptions = options.toSorted((a, b) => a.label.localeCompare(b.label));
</script>

<CrdtField on:change={(e) => dispatch('change', { value: e.detail.value})} bind:value bind:unsavedChanges let:editorValue let:onEditorValueChange viewMergeButtonPortal={append}>
  <MultiSelectField
    on:change={(e) =>
      onEditorValueChange(e.detail.value ?? [], true)}
    value={editorValue}
    disabled={readonly}
    options={sortedOptions}
    icon={readonly ? undefined : mdiMagnify}
    formatSelected={({ value, options }) => {
      return (ordered
      // sorted by order of selection
      ? value?.map(v => options.find(o => o.value === v)?.label).filter(label => !!label).join(', ')
      // sorted according to the order of options (e.g. alphabetical or by semantic domain)
      : options.map(o => o.label).join(', ')) || 'None';
    }}
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
