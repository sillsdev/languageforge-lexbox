<script lang="ts">
  import {getContext} from 'svelte';
  import RenderElement from './RenderElement.svelte';
  import type {Writable} from 'svelte/store';

  const crumbs: Writable<Element[]> = getContext('breadcrumb-store');

</script>

<div class="text-sm breadcrumbs p-0 max-xs:invisible">
  <ul>
    {#each $crumbs as crumb}
      <RenderElement tag="li" el={crumb}/>
    {/each}
  </ul>
</div>

<style lang="postcss">
  .breadcrumbs > ul > :global(li + ::before) {
    --tw-translate-y: 1px;
  }

  .breadcrumbs > ul > :global(li):not(:is(:last-child, :first-child)) {
    /* Only show first and last breadcrumbs on smaller screens */
    @apply max-lg:hidden;
  }
</style>
