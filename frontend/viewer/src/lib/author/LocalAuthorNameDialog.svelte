<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import Input from '$lib/components/ui/input/input.svelte';
  import Label from '$lib/components/ui/label/label.svelte';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import type {StorageProp} from '$lib/storage/storage-prop.svelte';

  const {localAuthorName}: {localAuthorName: StorageProp} = $props();

  let open = $state(false);
  let name = $state('');
  let submitted = $state(false);
  let resolve: ((saved: boolean) => void) | undefined;
  useBackHandler({addToStack: () => open, onBack: () => finish(false), key: 'local-author-name-dialog'});

  const changing = $derived(!!localAuthorName.current);
  const nameError = $derived(name.trim() ? undefined : $t`Name is required`);
  const showError = $derived(submitted && !!nameError);

  /** Asks for the name; resolves true once one is saved, false when the user cancels. */
  export function prompt(): Promise<boolean> {
    name = localAuthorName.current;
    submitted = false;
    open = true;
    return new Promise(r => resolve = r);
  }

  function finish(saved: boolean) {
    const r = resolve;
    resolve = undefined;
    open = false;
    r?.(saved);
  }

  async function save(e: Event) {
    e.preventDefault();
    e.stopPropagation();
    submitted = true;
    if (nameError) return;
    await localAuthorName.set(name.trim());
    finish(true);
  }

  function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' && event.target instanceof HTMLInputElement) void save(event);
  }
</script>

<Dialog.Root bind:open={open} onOpenChange={(isOpen) => { if (!isOpen) finish(false); }}>
  <Dialog.DialogContent onkeydown={handleKeydown}>
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{changing ? $t`Change your name` : $t`Add your name`}</Dialog.DialogTitle>
      <Dialog.DialogDescription>
        {$t`People you share this project with will see this name next to your changes and comments. You can change it later in the sync dialog.`}
      </Dialog.DialogDescription>
    </Dialog.DialogHeader>
    <div class="grid gap-2">
      <Label for="local-author-name">{$t`Your name`}</Label>
      <Input
        id="local-author-name"
        bind:value={name}
        autofocus
        autocomplete="name"
        spellcheck="false"
        maxlength={100}
        aria-invalid={showError}
        aria-describedby={showError ? 'local-author-name-error' : undefined} />
      {#if showError}
        <p id="local-author-name-error" class="text-destructive text-sm">{nameError}</p>
      {/if}
    </div>
    <Dialog.DialogFooter>
      <Button onclick={() => finish(false)} variant="secondary">{$t`Cancel`}</Button>
      <Button onclick={save}>{changing ? $t`Save` : $t`Continue`}</Button>
    </Dialog.DialogFooter>
  </Dialog.DialogContent>
</Dialog.Root>
