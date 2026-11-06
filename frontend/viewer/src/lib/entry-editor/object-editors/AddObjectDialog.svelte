<script lang="ts">
  import type {Snippet} from 'svelte';
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import * as Editor from '$lib/components/editor';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';

  type Props = {
    open: boolean;
    title: string;
    description?: string;
    submitLabel: string;
    onsubmit: () => void;
    children: Snippet;
  };

  let {open = $bindable(), title, description, submitLabel, onsubmit, children}: Props = $props();

  useBackHandler({addToStack: () => open, onBack: () => open = false, key: 'add-object-dialog'});

  let editor = $state<Editor.Root>();

  async function submit(e: Event) {
    e.preventDefault();
    e.stopPropagation();
    if (!open) return;
    // flush blur-only change handlers (stomp guards, rich-string WS) before reading the draft
    await editor?.commit();
    onsubmit();
    open = false;
  }

  function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !IsMobile.value) {
      void submit(event);
    }
  }
</script>

<Dialog.Root bind:open>
  <Dialog.DialogContent onkeydown={handleKeydown} class="sm:w-[min(calc(100%-32px),50rem)] max-md:px-2">
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{title}</Dialog.DialogTitle>
      {#if description}
        <Dialog.DialogDescription>{description}</Dialog.DialogDescription>
      {/if}
    </Dialog.DialogHeader>
    <Editor.Root bind:this={editor} class="min-w-0">
      <Editor.Grid>
        {@render children()}
      </Editor.Grid>
    </Editor.Root>
    <Dialog.DialogFooter>
      <Button onclick={() => open = false} variant="secondary">{$t`Cancel`}</Button>
      <Button onclick={(e) => submit(e)}>{submitLabel}</Button>
    </Dialog.DialogFooter>
  </Dialog.DialogContent>
</Dialog.Root>
