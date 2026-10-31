<script lang="ts" module>
  const subscriptDigits = '₀₁₂₃₄₅₆₇₈₉';

  /**
   * Plain-text form of {@link HomographNumber}, for places that only take a string (e.g. a `title`).
   * Display only: never append it to headwords used for search, sorting or matching.
   */
  export function homographSubscript(homographNumber: number): string {
    if (!(homographNumber > 0)) return '';
    return String(homographNumber).replace(/\d/g, (digit) => subscriptDigits[Number(digit)]);
  }
</script>

<script lang="ts">
  import type {HTMLAttributes} from 'svelte/elements';
  import {cn} from '$lib/utils';

  let {
    homographNumber,
    class: className,
    ...restProps
  }: HTMLAttributes<HTMLElement> & {
    /** FLEx-style: 0 means the entry has no homographs, so nothing is shown */
    homographNumber: number;
  } = $props();
</script>

<!-- Rendered directly after a headword, like FLEx: bank₁, bank₂. 0.6em matches the Unicode subscript digits; the preflight 75% is too big. -->
{#if homographNumber > 0}<sub data-testid="homograph-number" class={cn('text-[0.6em]', className)} {...restProps}
    >{homographNumber}</sub
  >{/if}
