<script lang="ts">
  import { mdiInformationVariantCircle } from '@mdi/js';
  import { Button, Dialog, Toggle } from 'svelte-ux';
  import type { Readable } from 'svelte/store';
  import NewTabLinkMarkdown from '../markdown/NewTabLinKMarkdown.svelte';
  import { onMount } from 'svelte';

  export let text: Readable<string>;

  let toggle: Toggle;

  onMount(() => {
    if (!localStorage.getItem('suppressAbout')) {
      toggle.on = true;
      localStorage.setItem('suppressAbout', 'true');
    }
  });
</script>

<Toggle bind:this={toggle} let:on={open} let:toggleOn let:toggleOff>
  <Button on:click={toggleOn} size="sm" variant="outline" icon={mdiInformationVariantCircle}>
    <div class="hidden sm:contents">
      About
    </div>
  </Button>
  <Dialog {open} on:close={toggleOff} class="w-[700px]">
    <div class="m-6 prose">
      <NewTabLinkMarkdown md={$text} />
    </div>
    <div class="flex-grow"></div>
    <div slot="actions">
      <Button>Close</Button>
    </div>
  </Dialog>
</Toggle>
