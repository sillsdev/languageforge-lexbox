<script lang="ts">
  import type { ComponentProps } from 'svelte';
  import CrdtField from './CrdtField.svelte';
  import { TextField } from 'svelte-ux';

  export let value: string | number | null;
  export let unsavedChanges = false;
  export let label: string | undefined = undefined;
  export let labelPlacement: ComponentProps<TextField>['labelPlacement'] = undefined;
  export let placeholder: string | undefined = undefined;
  export let readonly: boolean | undefined = undefined;
  let append: HTMLElement;

  // Labels don't always fit (beause WS's can be long and ugly), so a title might be important sometimes
  function addTitleToLabel(textElem: HTMLElement): void {
    const labelElem = textElem.closest('label')?.querySelector('.label');
    if (label && labelElem) labelElem.setAttribute('title', label);
  }
</script>

<CrdtField on:change bind:value bind:unsavedChanges let:editorValue let:save let:onEditorValueChange viewMergeButtonPortal={append}>
  <TextField
    on:change={(e) => onEditorValueChange(e.detail.inputValue)}
    on:blur={(e) => {if (e.target) save()}}
    actions={(el) => { addTitleToLabel(el); return []; }}
    value={editorValue}
    disabled={readonly}
    class="ws-field gap-2 text-right"
    classes={{ root: `${editorValue ? '' : 'empty'} ${readonly ? 'readonly' : ''}`, input: 'field-input', container: 'field-container' }}
    {label}
    {labelPlacement}
    {placeholder}>
    <span bind:this={append} slot="append" />
  </TextField>
</CrdtField>
