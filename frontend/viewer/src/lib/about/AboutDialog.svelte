<script lang="ts">
  import * as Dialog from '$lib/components/ui/dialog';
  import { Button } from '$lib/components/ui/button';
  import NewTabLinkMarkdown from '../markdown/NewTabLinkMarkdown.svelte';
  import { onMount } from 'svelte';
  import { t } from 'svelte-i18n-lingui';

  export let open: boolean;
  export let text: string;

  onMount(() => {
    if (!localStorage.getItem('suppressAbout')) {
      open = true;
      localStorage.setItem('suppressAbout', 'true');
    }
  });
</script>

<Dialog.Root bind:open>
  <Dialog.Content>
    <Dialog.Header>
      <Dialog.Title>{$t`About`}</Dialog.Title>
    </Dialog.Header>
    <div class="prose">
      <NewTabLinkMarkdown md={text} />
    </div>
    <Dialog.Footer>
      <Button onclick={() => open = false}>{$t`Close`}</Button>
    </Dialog.Footer>
  </Dialog.Content>
</Dialog.Root>
