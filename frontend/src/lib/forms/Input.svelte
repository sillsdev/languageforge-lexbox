<script lang="ts">
  import { onMount } from 'svelte';
  import { randomFieldId } from './utils';

  export let label: string;
  export let value = '';
  export let type = 'text';
  export let required = false;
  export let autofocus = false;
  export let readonly = false;
  export let error: string | string[] = '';
  export let placeholder = '';

  export let id = randomFieldId();
  let input: HTMLInputElement;

  onMount(autofocusIfRequested);

  function autofocusIfRequested(): void {
    autofocus && input?.focus();
  }

  // works around "svelte(invalid-type)" warning, i.e., can't have a dynamic type AND bind:value...keep an eye on https://github.com/sveltejs/svelte/issues/3921
  function typeWorkaround(node: HTMLInputElement): void {
    node.type = type;
  }
</script>

<!-- https://daisyui.com/components/input -->
<div class="form-control w-full">
  <label for={id} class="label">
    <span class="label-text">
      {label}
    </span>
  </label>
  <input
    {id}
    use:typeWorkaround
    bind:value
    {required}
    class:input-error={error}
    {placeholder}
    bind:this={input}
    class="input input-bordered"
    {readonly}
  />
  {#if error}
    <label for={id} class="label">
      <span class="label-text-alt text-error mb-2">{typeof error === 'string' ? error : error.join(', ')}</span>
    </label>
  {/if}
</div>
