<script lang="ts">
  import { onMount } from 'svelte';
  import FormFieldError from './FormFieldError.svelte';

  export let label: string;
  export let error: string | string[] | undefined;
  export let id: string;
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
