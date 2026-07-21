<script lang="ts">
  import type {IPicture} from '$lib/dotnet-types';
  import * as Dialog from '$lib/components/ui/dialog';
  import {Button} from '$lib/components/ui/button';
  import PictureImage from './PictureImage.svelte';
  import PictureActionsMenu from './PictureActionsMenu.svelte';
  import {useWritingSystemService, asString} from '$project/data';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
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

  // Track the shown picture by id (not index) so it survives reordering; the index is derived for
  // the prev/next arrows against the live list.
  const currentIndex = $derived(pictures.findIndex((p) => p.id === pictureId));
  const current = $derived(currentIndex >= 0 ? pictures[currentIndex] : undefined);

  // If the shown picture disappears (e.g. deleted from the menu), close the dialog.
  $effect(() => {
    if (open && pictureId !== undefined && !current) open = false;
  });

  // Captions collapse to just the first non-empty one on click; each picture starts expanded.
  let collapsed = $state(false);

  const hasMultiple = $derived(pictures.length > 1);
  function showPrevious() {
    if (currentIndex > 0) {
      pictureId = pictures[currentIndex - 1].id;
      collapsed = false;
    }
  }
  function showNext() {
    if (currentIndex >= 0 && currentIndex < pictures.length - 1) {
      pictureId = pictures[currentIndex + 1].id;
      collapsed = false;
    }
  }

  // The current picture's non-empty captions, in the project's writing-system order.
  const captions = $derived.by(() => {
    if (!current) return [];
    const caption = current.caption;
    return writingSystemService
      .uniqueWritingSystems()
      .map((ws) => ({ws, text: asString(caption[ws.wsId]) ?? ''}))
      .filter((c) => c.text.length > 0);
  });
  // Collapsed shows only the first caption (its text clamped to one line); expanded shows all.
  const shownCaptions = $derived(collapsed ? captions.slice(0, 1) : captions);
</script>

<Dialog.Root bind:open>
  <!-- Shrink-to-fit up to the viewport: override the default min-width so a small picture yields a
       small dialog, while the image itself is capped to the viewport (see PictureImage size="full"). -->
  <Dialog.DialogContent
    class="w-fit min-w-0 max-w-[calc(100vw-2rem)] max-sm:min-h-0 sm:min-w-0"
    onOpenAutoFocus={(e) => e.preventDefault()}
  >
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{$t`Picture`}</Dialog.DialogTitle>
    </Dialog.DialogHeader>

    {#if current}
      <div class="flex items-center justify-center gap-2">
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
        <div class="relative flex justify-center">
          <PictureImage picture={current} size="full" showCaption={false} />
          <div class="absolute right-1 top-1 z-10">
            <PictureActionsMenu
              disabled={busy}
              triggerClass="text-foreground bg-background/70 hover:bg-background/90 rounded-full shadow-sm"
              onEdit={() => current && onEdit(current)}
              onDownload={() => current && onDownload(current)}
              onDelete={() => current && onDelete(current)}
            />
          </div>
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

      {#if captions.length > 0}
        <!-- Read-only captions (WS abbreviation label + text, like RichMultiWsInput). Click to
             collapse to just the first caption (line-clamp-1), click again to show them all. The
             disclosure chevron signals it's toggleable. -->
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
          <!-- Points down when collapsed (more below), rotates to up when expanded. -->
          <span
            class="i-mdi-chevron-down text-muted-foreground mt-0.5 size-4 shrink-0 transition-transform"
            class:rotate-180={!collapsed}
            aria-hidden="true"
          ></span>
        </button>
      {/if}
    {/if}
  </Dialog.DialogContent>
</Dialog.Root>
