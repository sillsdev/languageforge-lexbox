<script lang="ts">
  import {NewTabLinkMarkdown} from '$lib/components/Markdown';
  import FormFieldError from './FormFieldError.svelte';
  import { randomFormId } from './utils';

  export let label: string;
  export let value: boolean;
  export let id = randomFormId();
  export let error: string | string[] | undefined = undefined;
  export let description: string | undefined = undefined;
  export let variant: 'checkbox-warning' | undefined = undefined;
  export let labelColor: 'text-warning' | undefined = undefined;
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
