<script lang="ts">
  import { Button, Dialog } from 'svelte-ux';
  import NewTabLinkMarkdown from '../markdown/NewTabLinKMarkdown.svelte';
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
    <NewTabLinkMarkdown md={text} />
  </div>
  <div class="flex-grow"></div>
  <div slot="actions">
    <Button on:click={() => open = false}>Close</Button>
  </div>
</Dialog>
