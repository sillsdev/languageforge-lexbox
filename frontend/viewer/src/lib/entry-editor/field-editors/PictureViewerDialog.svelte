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
    /** The picture currently shown; bindable so the prev/next arrows can move through `pictures`. */
    pictureId: string | undefined;
    /** Disables the actions menu while a picture operation is in flight (guards double-invoke). */
    busy?: boolean;
    onEdit: (picture: IPicture) => void;
    onDownload: (picture: IPicture) => void;
    onDelete: (picture: IPicture) => void;
  };
  let {open = $bindable(false), pictureId = $bindable(), pictures, busy = false, onEdit, onDownload, onDelete}: Props = $props();

  useBackHandler({addToStack: () => open, onBack: () => (open = false), key: 'picture-viewer-dialog'});
  const writingSystemService = useWritingSystemService();

  // Track the shown picture by id (not index) so it survives reordering.
  const currentIndex = $derived(pictures.findIndex((p) => p.id === pictureId));
  const current = $derived(currentIndex >= 0 ? pictures[currentIndex] : undefined);

  // If the shown picture disappears (e.g. deleted from the menu), move to the nearest remaining
  // picture; close only when none are left.
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

  // Captions collapse to just the first non-empty one on click; every shown picture starts expanded.
  let collapsed = $state(false);
  $effect(() => {
    // eslint-disable-next-line @typescript-eslint/no-unused-expressions
    pictureId; // rerun when the shown picture changes
    collapsed = false;
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
  const shownCaptions = $derived(collapsed ? captions.slice(0, 1) : captions);
</script>

<Dialog.Root bind:open>
  <!-- Fixed stage so paging through pictures of different sizes/caption counts doesn't move the
       dialog or the prev/next arrows. Mobile is already full-screen via the base dialog. -->
  <!-- Own the close button (hideClose) so the actions menu and the X share one flex cluster and
       line up, instead of the menu having to mirror the built-in X's absolute position. -->
  <Dialog.DialogContent
    hideClose
    class="flex flex-col gap-3 overflow-hidden sm:h-[85dvh] sm:w-[min(92vw,48rem)] sm:min-w-0 sm:max-w-none"
  >
    <Dialog.DialogHeader class="shrink-0">
      <Dialog.DialogTitle class="flex items-baseline gap-3">
        {$t`Picture`}
        {#if hasMultiple && currentIndex >= 0}
          <span class="text-muted-foreground text-sm font-normal">{currentIndex + 1} / {pictures.length}</span>
        {/if}
      </Dialog.DialogTitle>
    </Dialog.DialogHeader>

    <!-- Top-right chrome: the picture's actions menu next to the close button, both icon-xs so they
         align by flexbox. Anchored to the dialog, never over the letterboxed image. -->
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
      <div class="flex min-h-0 flex-1 items-center justify-center gap-1 sm:gap-2">
        {#if hasMultiple}
          <Button
            variant="ghost"
            size="icon"
            icon="i-mdi-chevron-left"
            aria-label={$t`Previous picture`}
            disabled={currentIndex <= 0}
            onclick={showPrevious}
          />
        {/if}
        <div class="flex h-full min-h-0 min-w-0 flex-1 items-center justify-center">
          <PictureImage picture={current} size="full" showCaption={false} />
        </div>
        {#if hasMultiple}
          <Button
            variant="ghost"
            size="icon"
            icon="i-mdi-chevron-right"
            aria-label={$t`Next picture`}
            disabled={currentIndex >= pictures.length - 1}
            onclick={showNext}
          />
        {/if}
      </div>

      <!-- Reserve the caption band even when empty so paging between pictures with and without
           captions doesn't resize the stage above (which would move the nav arrows). -->
      <div class="h-16 shrink-0 overflow-y-auto">
        {#if captions.length > 0}
          <!-- Read-only captions, styled like RichMultiWsInput (WS abbreviation label + text). -->
          <button
            type="button"
            class="flex w-full cursor-pointer appearance-none items-start gap-2 border-0 bg-transparent p-0 text-left text-sm"
            aria-label={$t`Toggle captions`}
            aria-expanded={!collapsed}
            onclick={() => (collapsed = !collapsed)}
          >
            <div class="grid min-w-0 flex-1 grid-cols-[auto_1fr] items-baseline gap-x-3 gap-y-1">
              {#each shownCaptions as {ws, text} (ws.wsId)}
                <span class="text-muted-foreground font-medium leading-none" title={`${ws.name} (${ws.wsId})`}>
                  {ws.abbreviation}
                </span>
                <span class="break-words" class:line-clamp-1={collapsed}>{text}</span>
              {/each}
            </div>
            <span
              class="i-mdi-chevron-down text-muted-foreground mt-0.5 size-4 shrink-0 transition-transform"
              class:rotate-180={!collapsed}
              aria-hidden="true"
            ></span>
          </button>
        {/if}
      </div>
    {/if}
  </Dialog.DialogContent>
</Dialog.Root>
