<script lang="ts" module>
  export {homographSubscript} from './homograph-subscript';
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
