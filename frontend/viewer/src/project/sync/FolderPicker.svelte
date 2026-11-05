<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import Input from '$lib/components/ui/input/input.svelte';
  import Label from '$lib/components/ui/label/label.svelte';
  import {usePlatformFeaturesService} from '$lib/services/platform-features-service';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';

  const platformFeatures = usePlatformFeaturesService();
  let open = $state(false);
  let path = $state('');
  let error = $state<string>();
  let checking = $state(false);
  let resolvePick: ((path: string | undefined) => void) | undefined;
  useBackHandler({addToStack: () => open, onBack: () => close(undefined), key: 'folder-picker-dialog'});

  /** The host's native folder picker, or a typed path where it has none (FwLiteWeb, e.g. on Linux). Undefined when cancelled. */
  export function pick(suggestedPath?: string): Promise<string | undefined> {
    if (platformFeatures.features.hasFolderPicker) return platformFeatures.service.pickFolder();
    path = suggestedPath ?? path;
    error = undefined;
    open = true;
    return new Promise(resolve => resolvePick = resolve);
  }

  function close(result: string | undefined) {
    resolvePick?.(result);
    resolvePick = undefined;
    open = false;
  }

  async function submit(e: Event) {
    e.preventDefault();
    const folder = path.trim();
    if (!folder) return;
    checking = true;
    try {
      if (await platformFeatures.service.folderExists(folder)) close(folder);
      else error = $t`This folder doesn't exist. Check the path, for example /media/yourname/USB.`;
    } finally {
      checking = false;
    }
  }
</script>

<Dialog.Root bind:open onOpenChange={isOpen => { if (!isOpen) close(undefined); }}>
  <Dialog.DialogContent>
    <form onsubmit={submit} class="contents">
      <Dialog.DialogHeader>
        <Dialog.DialogTitle>{$t`Choose a folder`}</Dialog.DialogTitle>
      </Dialog.DialogHeader>
      <div class="flex flex-col gap-2">
        <Label for="folder-picker-path">{$t`Folder path`}</Label>
        <Input id="folder-picker-path" bind:value={path} oninput={() => error = undefined}
          aria-invalid={!!error} aria-describedby={error ? 'folder-picker-error' : undefined} />
        {#if error}
          <p id="folder-picker-error" class="text-destructive text-sm">{error}</p>
        {/if}
      </div>
      <Dialog.DialogFooter>
        <Button type="button" variant="secondary" onclick={() => close(undefined)}>{$t`Cancel`}</Button>
        <Button type="submit" loading={checking} disabled={!path.trim()}>{$t`Use this folder`}</Button>
      </Dialog.DialogFooter>
    </form>
  </Dialog.DialogContent>
</Dialog.Root>
