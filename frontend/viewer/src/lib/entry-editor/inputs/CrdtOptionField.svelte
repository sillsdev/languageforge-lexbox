<script lang="ts">
  import type { ComponentProps } from 'svelte';
  import CrdtField from './CrdtField.svelte';
  import { SelectField, type TextField, type MenuOption } from 'svelte-ux';
  import {makeHasHadValueTracker} from '$lib/utils';

  export let value: string | undefined;
  export let unsavedChanges = false;
  export let options: MenuOption[] = [];
  export let label: string | undefined = undefined;
  export let labelPlacement: ComponentProps<TextField>['labelPlacement'] = undefined;
  export let placeholder: string | undefined = undefined;
  export let readonly: boolean | undefined = undefined;
  let append: HTMLElement;

  $: sortedOptions = [...options].sort((a, b) => a.label.localeCompare(b.label));

  let hasHadValue = makeHasHadValueTracker();
</script>

<CrdtField on:change bind:value bind:unsavedChanges let:editorValue let:onEditorValueChange viewMergeButtonPortal={append}>
  {#key options}
  <SelectField
    on:change={(e) => onEditorValueChange(e.detail.value, true)}
    value={editorValue}
    disabled={readonly}
    options={sortedOptions}
    clearSearchOnOpen={false}
    clearable={false}
    search={() => Promise.resolve()}
    class="ws-field"
    classes={{ root: `${hasHadValue.pushAndGet(editorValue) ? '' : 'unused'} ${readonly ? 'readonly' : ''}`, field: 'field-container' }}
    {label}
    {labelPlacement}
    {placeholder}>
    <span bind:this={append} slot="append"></span>
  </SelectField>
  {/key}
</CrdtField>
