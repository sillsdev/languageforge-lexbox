<script lang="ts">
  import FormFieldError from './FormFieldError.svelte';
  import { randomFormId } from './utils';

  export let label: string;
  export let value: boolean;
  export let id = randomFormId();
  export let error: string | string[] | undefined = undefined;
</script>

<div class="form-control w-full">
  <label class="label cursor-pointer justify-normal pb-0">
    <input {id} type="checkbox" bind:checked={value} class="checkbox mr-4" />
    <span class="label-text">{label}</span>
  </label>
  <FormFieldError {error} {id} />
</div>
