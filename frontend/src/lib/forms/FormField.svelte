<script lang="ts">
  import { onMount } from 'svelte';
  import FormFieldError from './FormFieldError.svelte';
  import { randomFieldId } from './utils';

  export let label: string;
  export let error: string | string[] | undefined = undefined;
  export let id: string = randomFieldId();
  /**
   * For login pages, EditableText, admin pages etc. auto focus is not a real accessibility problem.
   * So we allow/support it and disable a11y-autofocus warnings in generic places.
   */
  export let autofocus = false;

  let elem: HTMLDivElement;

  onMount(autofocusIfRequested);

  function autofocusIfRequested(): void {
    autofocus && elem.querySelector<HTMLElement>('input, select, textarea')?.focus();
  }
</script>

<div class="form-control w-full" bind:this={elem}>
  <label for={id} class="label">
    <span class="label-text">
      {label}
    </span>
  </label>
  <slot />
  <FormFieldError {id} {error} />
</div>
