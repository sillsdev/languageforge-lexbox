<script lang="ts">
  import { browser } from '$app/environment';
  import { page } from '$app/stores';
  import { error } from '$lib/error';
  import UnexpectedError from '$lib/error/UnexpectedError.svelte';
  import { Content, Page } from '$lib/layout';
  import { onDestroy } from 'svelte';

  
  if (browser) {
    onDestroy(
      page.subscribe((p) => {
        error.set(p.error);
      })
    );
  }
</script>

<Content>
  <Page>
    <UnexpectedError />
  </Page>
</Content>
