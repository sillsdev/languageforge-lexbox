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

  let {
    homographNumber,
    ...restProps
  }: HTMLAttributes<HTMLElement> & {
    /** FLEx-style: 0 means the entry has no homographs, so nothing is shown */
    homographNumber: number;
  } = $props();
</script>

<!-- Rendered directly after a headword, like FLEx: bank₁, bank₂ -->
{#if homographNumber > 0}<sub data-testid="homograph-number" {...restProps}>{homographNumber}</sub>{/if}
