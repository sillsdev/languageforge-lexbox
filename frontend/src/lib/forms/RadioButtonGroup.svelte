<script lang="ts" module>
  export type SingleRadioButton = {
    label: string;
    value: string;
  };
</script>

<script lang="ts">
  import { NewTabLinkMarkdown } from '$lib/components/Markdown';
  import FormFieldError from './FormFieldError.svelte';
  import { randomFormId } from './utils';

  interface Props {
    buttons: SingleRadioButton[];
    label: string; // This is for the group as a whole
    value?: string; // Should be used with `bind:value={localVariable}`
    id?: string;
    error?: string | string[];
    description?: string;
    variant?: 'radio-warning';
    labelColor?: 'text-warning';
    divClass?: string;
  }

  let {
    buttons,
    label,
    value = $bindable(''),
    id = randomFormId(),
    error,
    description,
    variant,
    labelColor,
    divClass,
  }: Props = $props();
</script>

<div role="radiogroup" class={divClass ?? ''} aria-labelledby={`label-${id}`} id={`group-${id}`}>
  <div class="legend" id={`label-${id}`}>
    {label}
  </div>
  {#each buttons as button (button.value)}
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
