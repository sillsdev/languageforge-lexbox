<script lang="ts">
  import {NewTabLinkMarkdown} from '$lib/components/Markdown';
  import FormFieldError from './FormFieldError.svelte';
  import { randomFormId } from './utils';

  interface Props {
    label: string;
    value: boolean;
    id?: any;
    error?: string | string[] | undefined;
    description?: string | undefined;
    variant?: 'checkbox-warning' | undefined;
    labelColor?: 'text-warning' | undefined;
  }

  let {
    label,
    value = $bindable(),
    id = randomFormId(),
    error = undefined,
    description = undefined,
    variant = undefined,
    labelColor = undefined
  }: Props = $props();
</script>

<div class="form-control w-full">
  <label class="label cursor-pointer justify-normal pb-0">
    <input {id} type="checkbox" bind:checked={value} class="checkbox mr-4 {variant ?? ''}" />
    <span class="label-text inline-flex items-center gap-2 {labelColor ?? ''}">
      {label}
    </span>
  </label>
  {#if description}
    <label for={id} class="label pb-0">
      <span class="label-text-alt">
        <NewTabLinkMarkdown md={description} />
      </span>
    </label>
  {/if}
  <FormFieldError {error} {id} />
</div>
