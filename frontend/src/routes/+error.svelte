<script lang="ts">
  import UnexpectedError from '$lib/error/UnexpectedError.svelte';
  import { AppBar, Content, Page } from '$lib/layout';
</script>

<AppBar user={undefined} />

<Content>
  <Page>
    <UnexpectedError />
  </Page>
</Content>
