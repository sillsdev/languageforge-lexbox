<script lang="ts">
  import type {Snippet} from 'svelte';
  import DialogsProvider from '$lib/DialogsProvider.svelte';
  import {initImageService} from '$lib/entry-editor/field-editors/image-service.svelte';

  let {children}: {children: Snippet} = $props();

  // Entry-scoped image cache (same as EntryEditor); uses InMemoryDemoApi from FWLiteDecorator.
  initImageService();
</script>

<DialogsProvider />
{@render children()}
