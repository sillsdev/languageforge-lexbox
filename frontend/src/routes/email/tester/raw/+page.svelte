<script lang="ts">
  import type { PageData } from './$types';

  export let data: PageData;
</script>

<span class="text-black">Subject: {data.subject}</span>
<!-- eslint-disable-next-line svelte/no-at-html-tags -->
{@html data.html}
