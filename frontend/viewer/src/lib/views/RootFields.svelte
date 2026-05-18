<script lang="ts">
  import {initViewService, useViewService} from '$lib/views/view-service.svelte';
  import type {Snippet} from 'svelte';

  let {children}: {children?: Snippet} = $props();

  const viewService = useViewService();
  const overrideService = initViewService({persist: false});

  $effect.pre(() => {
    // use the root view of what is potentially a custom-view
    overrideService.overrideView(viewService.rootView);
  });
</script>

{@render children?.()}
