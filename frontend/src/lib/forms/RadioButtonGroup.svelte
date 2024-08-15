<script lang="ts" context="module">
  export type SingleRadioButton = {
    label: string
    value: string
  }
</script>

<script lang="ts">
  import Markdown from 'svelte-exmarkdown';
  import FormFieldError from './FormFieldError.svelte';
  import { randomFormId } from './utils';
  import { NewTabLinkRenderer } from '$lib/components/Markdown';

  export let buttons: SingleRadioButton[];
  export let label: string; // This is for the group as a whole
  export let value: string = ''; // Should be used with `bind:value={localVariable}`
  export let id = randomFormId();
  export let error: string | string[] | undefined = undefined;
  export let description: string | undefined = undefined;
  export let variant: 'radio-warning' | undefined = undefined;
  export let labelColor: 'text-warning' | undefined = undefined;
</script>

<div
  role="radiogroup"
  class={$$props.class ?? ''}
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
        <Markdown md={description} plugins={[{ renderer: { a: NewTabLinkRenderer } }]} />
      </span>
    </label>
  {/if}
  <FormFieldError {error} {id} />
</div>
