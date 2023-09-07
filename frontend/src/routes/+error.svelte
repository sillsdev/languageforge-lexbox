<script lang="ts">
  import UnexpectedError from '$lib/error/UnexpectedError.svelte';
  import { AppBar, Content, Page } from '$lib/layout';
</script>

<AppBar />

<Content>
  <Page>
    <UnexpectedError />
  </Page>
</Content>
