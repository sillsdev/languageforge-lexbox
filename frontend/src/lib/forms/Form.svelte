<script lang="ts">
  import type { Snippet } from 'svelte';

  import type { AnySuperForm } from './types';

  let formElem: HTMLFormElement | undefined = $state();

  interface Props {
    id?: string;
    enhance?: AnySuperForm['enhance'];
    children?: Snippet;
  }

  const { id, enhance, children }: Props = $props();
  function enhanceIfRequested(...args: Parameters<AnySuperForm['enhance']>): void {
    // eslint-disable-next-line @typescript-eslint/no-unused-expressions
    enhance && enhance(...args);
  }

  export function requestSubmit(): void {
    formElem?.requestSubmit();
  }
</script>

<!-- https://daisyui.com/components/input/#with-form-control-and-labels -->
<form
  bind:this={formElem}
  {id}
  use:enhanceIfRequested
  method="post"
  onsubmit={e => e.preventDefault()}
  class="flex flex-col"
>
  {@render children?.()}
</form>
<!-- see frontend/src/app.postcss for global styles related to forms -->
