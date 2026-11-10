<script lang="ts">
  import type {Snippet} from 'svelte';
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import * as Editor from '$lib/components/editor';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {watch} from 'runed';
  import {isSubmitEnter} from '$lib/utils/submit-on-enter';

  type Props = {
    open: boolean;
    title: string;
    description?: string;
    submitLabel: string;
    /** Messages for what is missing; the draft is only added when there are none */
    validate: () => string[];
    onsubmit: () => void;
    children: Snippet;
  };

  let {open = $bindable(), title, description, submitLabel, validate, onsubmit, children}: Props = $props();

  useBackHandler({addToStack: () => open, onBack: () => open = false, key: 'add-object-dialog'});

  let editor = $state<Editor.Root>();
  let errors = $state<string[]>([]);
  watch(() => open, () => { if (open) errors = []; });

  // guards against a second Enter/click while the commit below is awaited, which would add the draft twice
  let submitting = false;

  async function submit(e: Event) {
    e.preventDefault();
    e.stopPropagation();
    if (!open || submitting) return;
    submitting = true;
    try {
      // flush blur-only change handlers (stomp guards, rich-string WS) before reading the draft
      await editor?.commit();
      errors = validate();
      if (errors.length) return;
      onsubmit();
      open = false;
    } finally {
      submitting = false;
    }
  }

  function handleKeydown(event: KeyboardEvent) {
    if (isSubmitEnter(event)) void submit(event);
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
    {#if errors.length}
      <div class="text-end space-y-2">
        {#each errors as error (error)}
          <p class="text-destructive">{error}</p>
        {/each}
      </div>
    {/if}
    <Dialog.DialogFooter>
      <Button onclick={() => open = false} variant="secondary">{$t`Cancel`}</Button>
      <Button onclick={(e) => submit(e)}>{submitLabel}</Button>
    </Dialog.DialogFooter>
  </Dialog.DialogContent>
</Dialog.Root>
