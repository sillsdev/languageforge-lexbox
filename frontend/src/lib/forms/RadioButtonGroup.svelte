<script lang="ts" context="module">
  export type SingleRadioButton = {
    label: string
    value: string
  }
</script>

<script lang="ts">
  import {NewTabLinkMarkdown} from '$lib/components/Markdown';
  import FormFieldError from './FormFieldError.svelte';
  import { randomFormId } from './utils';

  export let buttons: SingleRadioButton[];
  export let label: string; // This is for the group as a whole
  export let value: string = ''; // Should be used with `bind:value={localVariable}`
  export let id = randomFormId();
  export let error: string | string[] | undefined = undefined;
  export let description: string | undefined = undefined;
  export let variant: 'radio-warning' | undefined = undefined;
  export let labelColor: 'text-warning' | undefined = undefined;
  export let divClass: string | undefined = undefined;
</script>

<div
  role="radiogroup"
  class={divClass ?? ''}
  aria-labelledby={`label-${id}`}
  id={`group-${id}`}
  >
  <div class="legend" id={`label-${id}`}>
    {label}
  </div>
  {#each buttons as button}
  <div class="form-control w-full">
    <label class="label cursor-pointer justify-normal pb-0">
      <input {id} type="radio" bind:group={value} value={button.value} class="radio mr-2 {variant ?? ''}" />
      <span class="label-text inline-flex items-center gap-2 {labelColor ?? ''}">
        {button.label}
      </span>
    </label>
  </div>
  {/each}
  {#if description}
    <label for={id} class="label pb-0">
      <span class="label-text-alt">
        <NewTabLinkMarkdown md={description} />
      </span>
    </label>
  {/if}
  <FormFieldError {error} {id} />
</div>
