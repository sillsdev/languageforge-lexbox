<script lang="ts">
  import {getContext} from 'svelte';
  import RenderElement from '$lib/layout/Breadcrumbs/RenderElement.svelte';
  import type {Writable} from 'svelte/store';

  let crumbs: Writable<Element[]> = getContext('breadcrumb-store');

</script>

<div class="text-sm breadcrumbs p-0">
  <ul>
    {#each $crumbs as crumb}
      <RenderElement tag="li" el={crumb}/>
    {/each}
  </ul>
</div>
