<script lang="ts">
  import type { AnySuperForm } from './types';

  let formElem: HTMLFormElement;
  export let id: string | undefined = undefined;

  export let enhance: AnySuperForm['enhance'] | undefined = undefined;
  function enhanceIfRequested(...args: Parameters<AnySuperForm['enhance']>): void {
    // eslint-disable-next-line @typescript-eslint/no-unused-expressions
    enhance && enhance(...args);
  }

  export function requestSubmit(): void {
    formElem.requestSubmit();
  }
</script>

<!-- https://daisyui.com/components/input/#with-form-control-and-labels -->
<form bind:this={formElem} {id} use:enhanceIfRequested method="post" on:submit|preventDefault class="flex flex-col">
  <slot />
</form>
<!-- see frontend/src/app.postcss for global styles related to forms -->
