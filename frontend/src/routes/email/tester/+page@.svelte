<script lang="ts">
  import type { PageData } from './$types';

  export let data: PageData;
</script>

<div class="m-3 mockup-window border bg-white">
  <span class="text-black">Subject: {data.subject}</span>
  <!-- eslint-disable-next-line svelte/no-at-html-tags -->
  {@html data.html}
</div>
