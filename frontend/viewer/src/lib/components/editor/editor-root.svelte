<script lang="ts">
  import {cn} from '$lib/utils';
  import type {WithElementRef} from 'bits-ui';
  import {tick} from 'svelte';
  import type {HTMLAttributes} from 'svelte/elements';

  type EditorRootProps = WithElementRef<HTMLAttributes<HTMLDivElement>>;

  let {class: className, children, ref = $bindable(null), ...restProps}: EditorRootProps = $props();

  export async function commit() {
    if (ref && document.activeElement instanceof HTMLElement && ref.contains(document.activeElement)) {
      // We have change handlers that only trigger on blur and
      // (1) add WS's to spans (i.e. finalize rich-strings. It's maybe a bad idea that we currently do that lazily) and
      // (2) commit stomp-guards
      // (3) run into errors if triggered too late (e.g. via onDestroy)
      // this is a simple way to trigger all of that
      document.activeElement.blur();
      await tick();
    }
  }
</script>

<div class={cn('@container/editor', className)} {...restProps} bind:this={ref}>
  {@render children?.()}
</div>
