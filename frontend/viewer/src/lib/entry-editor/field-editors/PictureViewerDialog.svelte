<script lang="ts">
  import type {IPicture} from '$lib/dotnet-types';
  import * as Dialog from '$lib/components/ui/dialog';
  import {Button, XButton} from '$lib/components/ui/button';
  import PictureImage from './PictureImage.svelte';
  import PictureActionsMenu from './PictureActionsMenu.svelte';
  import {useWritingSystemService, asString} from '$project/data';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {useEventListener} from 'runed';
  import {t} from 'svelte-i18n-lingui';

  type Props = {
    open: boolean;
    pictures: IPicture[];
    pictureId: string | undefined;
    busy?: boolean;
    onEdit: (picture: IPicture) => void;
    onDownload: (picture: IPicture) => void;
    onDelete: (picture: IPicture) => void;
  };
  let {open = $bindable(false), pictureId = $bindable(), pictures, busy = false, onEdit, onDownload, onDelete}: Props = $props();

  useBackHandler({addToStack: () => open, onBack: () => (open = false), key: 'picture-viewer-dialog'});
  const writingSystemService = useWritingSystemService();

  const currentIndex = $derived(pictures.findIndex((p) => p.id === pictureId));
  const current = $derived(currentIndex >= 0 ? pictures[currentIndex] : undefined);

  let lastIndex = 0;
  $effect(() => {
    if (currentIndex >= 0) lastIndex = currentIndex;
  });
  $effect(() => {
    if (!open || pictureId === undefined || current) return;
    if (pictures.length > 0) {
      pictureId = pictures[Math.min(lastIndex, pictures.length - 1)].id;
    } else {
      open = false;
    }
  });

  let captionsCollapsed = $state(false);
  $effect(() => {
    // eslint-disable-next-line @typescript-eslint/no-unused-expressions
    pictureId; // rerun when the shown picture changes
    captionsCollapsed = false;
  });

  const hasMultiple = $derived(pictures.length > 1);
  // Window-level (not on the dialog) so arrows keep working when nothing has focus — e.g. after a
  // nav button disables itself at the end of the list, dropping focus to the body.
  useEventListener(
    () => (open ? window : null),
    'keydown',
    (e) => {
      if (e.key === 'ArrowLeft') showPrevious();
      else if (e.key === 'ArrowRight') showNext();
    },
  );
  function showPrevious() {
    if (currentIndex > 0) {
      pictureId = pictures[currentIndex - 1].id;
    }
  }
  function showNext() {
    if (currentIndex >= 0 && currentIndex < pictures.length - 1) {
      pictureId = pictures[currentIndex + 1].id;
    }
  }

  const captions = $derived.by(() => {
    if (!current) return [];
    const caption = current.caption;
    return writingSystemService
      .uniqueWritingSystems()
      .map((ws) => ({ws, text: asString(caption[ws.wsId]) ?? ''}))
      .filter((c) => c.text.length > 0);
  });
  const shownCaptions = $derived(captionsCollapsed ? captions.slice(0, 1) : captions);
</script>

<Dialog.Root bind:open>
  <Dialog.DialogContent
    hideClose
    class="flex flex-col gap-3 overflow-hidden max-sm:p-0 sm:h-[85dvh] sm:w-[min(92vw,48rem)] sm:min-w-0 sm:max-w-none"
  >
    <Dialog.DialogHeader class="shrink-0 max-sm:px-4 max-sm:pt-4">
      <Dialog.DialogTitle class="flex items-baseline gap-3">
        {$t`Picture`}
        {#if hasMultiple && currentIndex >= 0}
          <span class="text-muted-foreground text-sm font-normal">{currentIndex + 1} / {pictures.length}</span>
        {/if}
      </Dialog.DialogTitle>
    </Dialog.DialogHeader>

    <div class="absolute end-4 top-4 z-10 flex items-center gap-2">
      {#if current}
        <PictureActionsMenu
          size="icon-xs"
          disabled={busy}
          onEdit={() => current && onEdit(current)}
          onDownload={() => current && onDownload(current)}
          onDelete={() => current && onDelete(current)}
        />
      {/if}
      <Dialog.Close>
        {#snippet child({props})}
          <XButton {...props} />
        {/snippet}
      </Dialog.Close>
    </div>

    {#if current}
      <div class="relative flex min-h-0 flex-1 items-center justify-center py-2">
        <PictureImage picture={current} size="full" showCaption={false} />
        {#if hasMultiple}
          <Button
            variant="ghost"
            size="icon"
            icon="i-mdi-chevron-left"
            aria-label={$t`Previous picture`}
            disabled={currentIndex <= 0}
            onclick={showPrevious}
            class="absolute start-2 top-1/2 z-10 -translate-y-1/2 rounded-full bg-background/80 shadow-sm ring-1 ring-border backdrop-blur-sm hover:bg-background"
          />
          <Button
            variant="ghost"
            size="icon"
            icon="i-mdi-chevron-right"
            aria-label={$t`Next picture`}
            disabled={currentIndex >= pictures.length - 1}
            onclick={showNext}
            class="absolute end-2 top-1/2 z-10 -translate-y-1/2 rounded-full bg-background/80 shadow-sm ring-1 ring-border backdrop-blur-sm hover:bg-background"
          />
        {/if}
      </div>

      {#if captions.length > 0}
        <div class="max-h-[30dvh] shrink-0 overflow-y-auto max-sm:px-4 max-sm:pb-4">
          <button
            type="button"
            class="flex w-full cursor-pointer appearance-none items-start gap-2 border-0 bg-transparent p-0 text-left text-sm"
            aria-label={$t`Show or hide captions`}
            aria-expanded={!captionsCollapsed}
            onclick={() => (captionsCollapsed = !captionsCollapsed)}
          >
            <div class="grid min-w-0 flex-1 grid-cols-[auto_1fr] items-baseline gap-x-3 gap-y-1">
              {#each shownCaptions as {ws, text} (ws.wsId)}
                <span class="text-muted-foreground font-medium leading-none" title={`${ws.name} (${ws.wsId})`}>
                  {ws.abbreviation}
                </span>
                <span class="break-words" class:line-clamp-1={captionsCollapsed}>{text}</span>
              {/each}
            </div>
            <span
              class="i-mdi-chevron-down text-muted-foreground mt-0.5 size-4 shrink-0 transition-transform"
              class:rotate-180={!captionsCollapsed}
              aria-hidden="true"
            ></span>
          </button>
        </div>
      {/if}
    {/if}
  </Dialog.DialogContent>
</Dialog.Root>
