<script lang="ts">
  import type { ComponentProps } from 'svelte';
  import CrdtField from './CrdtField.svelte';
  import { TextField, autoFocus as autoFocusFunc } from 'svelte-ux';
  import {makeHasHadValueTracker} from '$lib/utils';

  export let value: string | number | null | undefined;
  export let unsavedChanges = false;
  export let label: string | undefined = undefined;
  export let labelPlacement: ComponentProps<TextField>['labelPlacement'] = undefined;
  export let placeholder: string | undefined = undefined;
  export let readonly: boolean | undefined = undefined;
  export let autofocus: boolean = false;
  let append: HTMLElement;

  let hasHadValue = makeHasHadValueTracker();

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
    actions={(el) => {
      addTitleToLabel(el);
      // autofocus requires a delay otherwise it won't work in a dialog
      // also we can't use the autofocus attribute because of https://github.com/techniq/svelte-ux/issues/532
      return autofocus ? [autoFocusFunc(el, {delay: 5})] : [];
    }}
    value={editorValue}
    disabled={readonly}
    class="ws-field gap-0 text-right"
    classes={{ root: `${hasHadValue.pushAndGet(editorValue) ? '' : 'unused'} ${readonly ? 'readonly' : ''}`, input: 'field-input', container: 'field-container', label: 'pr-2' }}
    {label}
    {labelPlacement}
    {placeholder}>
    <span bind:this={append} slot="append"></span>
  </TextField>
</CrdtField>
