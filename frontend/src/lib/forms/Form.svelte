<script lang="ts">
  import type { Snippet } from 'svelte';
  import { createBubbler, preventDefault } from 'svelte/legacy';

  const bubble = createBubbler();
  import type { AnySuperForm } from './types';

  let formElem: HTMLFormElement = $state()!;

  interface Props {
    id?: string | undefined;
    enhance?: AnySuperForm['enhance'] | undefined;
    children?: Snippet;
  }

  let { id = undefined, enhance = undefined, children }: Props = $props();
  function enhanceIfRequested(...args: Parameters<AnySuperForm['enhance']>): void {
    // eslint-disable-next-line @typescript-eslint/no-unused-expressions
    enhance && enhance(...args);
  }

  export function requestSubmit(): void {
    formElem.requestSubmit();
  }
</script>

<!-- https://daisyui.com/components/input/#with-form-control-and-labels -->
<form
  bind:this={formElem}
  {id}
  use:enhanceIfRequested
  method="post"
  onsubmit={preventDefault(bubble('submit'))}
  class="flex flex-col"
>
  {@render children?.()}
</form>
<!-- see frontend/src/app.postcss for global styles related to forms -->
