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

<div class={cn('@container/editor editor-root', className)} {...restProps} bind:this={ref}>
  {@render children?.()}
</div>

<style lang="postcss">
  @reference "#app.css";

  /*
    Fields read as plain values separated by a divider; the input box (and focus ring) only appears while a
    field is focused, open or invalid. Excluding those states lets each input's own focus styles apply untouched.
  */
  .editor-root
    :global(
      :is(input[data-slot='input'], [role='textbox'][contenteditable], button[role='combobox'], [data-audio-add]):not(
        :focus,
        :focus-visible,
        [aria-expanded='true'],
        [aria-invalid='true']
      )
    ) {
    @apply rounded-none border-transparent border-b-border bg-transparent shadow-none;
  }

  .editor-root
    :global(
      :is(
          input[data-slot='input']:not([readonly]),
          [role='textbox'][contenteditable='true'],
          button[role='combobox']:not(:disabled),
          [data-audio-add]
        ):not(:focus, :focus-visible, [aria-expanded='true']):hover
    ) {
    @apply bg-muted/50;
  }

  /* the add-audio button has no border of its own */
  .editor-root :global([data-audio-add]) {
    @apply border-b;
  }
</style>
