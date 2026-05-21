<script lang="ts">
  import {initViewService, useViewService} from '$lib/views/view-service.svelte';
  import type {Snippet} from 'svelte';

  let {children}: {children?: Snippet} = $props();

  const viewService = useViewService();
  const overrideService = initViewService({persist: false});

  $effect.pre(() => {
    const rootView = viewService.rootView;
    const currentView = viewService.currentView;
    // show all root fields, but keep the current view's writing-system selection
    overrideService.overrideView({
      ...currentView,
      entryFields: rootView.entryFields,
      senseFields: rootView.senseFields,
      exampleFields: rootView.exampleFields,
    });
  });
</script>

{@render children?.()}
