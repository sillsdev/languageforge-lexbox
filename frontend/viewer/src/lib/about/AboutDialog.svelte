<script lang="ts">
  import { Button, Dialog } from 'svelte-ux';
  import Markdown from 'svelte-exmarkdown';
  import NewTabLinkRenderer from './NewTabLinkRenderer.svelte';
  import { onMount } from 'svelte';

  export let open: boolean;
  export let text: string;

  onMount(() => {
    if (!localStorage.getItem('suppressAbout')) {
      open = true;
      localStorage.setItem('suppressAbout', 'true');
    }
  });
</script>

<Dialog bind:open>
  <div class="m-6 prose">
    <Markdown md={text} plugins={[{ renderer: { a: NewTabLinkRenderer } }]} />
  </div>
  <div class="flex-grow"></div>
  <div slot="actions">
    <Button on:click={() => open = false}>Close</Button>
  </div>
</Dialog>
