<script lang="ts">
  import {initViewService, useViewService} from '$lib/views/view-service.svelte';
  import type {Snippet} from 'svelte';

  const {children}: {children: Snippet} = $props();

  // Activity previews always show every field with the user's own view labels (Lite/Classic wording preserved),
  // regardless of any custom field-hiding they have set. A non-persisted scoped service keeps this override
  // off their real selection; the effect keeps it in sync if they switch views while a preview is open.
  const parentView = useViewService();
  const service = initViewService({persist: false});
  $effect(() => {
    const v = parentView.currentView;
    service.overrideView({
      ...v,
      entryFields: v.entryFields.map(f => ({...f, show: true})),
      senseFields: v.senseFields.map(f => ({...f, show: true})),
      exampleFields: v.exampleFields.map(f => ({...f, show: true})),
    });
  });
</script>

{@render children?.()}
