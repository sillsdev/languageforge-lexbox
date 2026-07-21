<script lang="ts">
  import type {IPicture} from '$lib/dotnet-types';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useWritingSystemService} from '$project/data';
  import {t} from 'svelte-i18n-lingui';
  import {onDestroy} from 'svelte';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import PictureActionsMenu from './PictureActionsMenu.svelte';
  import {ImageService, useImageService, type ImageLoadState} from './image-service.svelte';

  type Props = {
    picture: IPicture;
    /** When provided (and not readonly), clicking the picture opens the fullscreen viewer and a
        three-dots actions menu is shown; the menu also opens on a long-press of the picture. */
    onView?: () => void;
    /** Opens the edit dialog; wired into the actions menu (the "Edit" item). */
    onEdit?: () => void;
    /** Downloads the picture; wired into the actions menu (and long-press menu). */
    onDownload?: () => void;
    /** Deletes the picture (with confirmation); wired into the actions menu. */
    onDelete?: () => void;
    /** Disables the actions affordance while an operation is in flight. */
    busy?: boolean;
    /** Whether to render the caption beneath the picture (hidden inside the edit/viewer dialogs). */
    showCaption?: boolean;
    /** 'thumbnail' (default) is the fixed-height field size; 'full' fills the viewer up to the
        viewport while never exceeding the image's native size. */
    size?: 'thumbnail' | 'full';
    readonly?: boolean;
  };
  const {picture, onView, onEdit, onDownload, onDelete, busy = false, showCaption = true, size = 'thumbnail', readonly = false}: Props = $props();

  // With a view handler wired (and not readonly) the picture is interactive: clicking opens the
  // viewer and a three-dots menu offers the actions.
  const interactive = $derived(!readonly && !!onView);
  const imageClass = $derived(
    size === 'full'
      ? 'max-h-[80dvh] w-auto max-w-full rounded-md object-contain'
      : 'h-40 w-auto rounded-md object-contain',
  );

  // Long-press opens the actions menu, so touch users don't have to hit the small three-dots target.
  let menuOpen = $state(false);
  let longPressTimer: ReturnType<typeof setTimeout> | undefined;
  let longPressed = false;

  function startLongPress() {
    cancelLongPress();
    longPressed = false;
    longPressTimer = setTimeout(() => {
      longPressed = true;
      menuOpen = true;
    }, 500);
  }

  function cancelLongPress() {
    if (longPressTimer !== undefined) {
      clearTimeout(longPressTimer);
      longPressTimer = undefined;
    }
  }

  function handleImageClick(event: MouseEvent) {
    // A long-press already opened the menu; swallow the click it would otherwise fire.
    if (longPressed) {
      longPressed = false;
      event.preventDefault();
      return;
    }
    // A picture not available locally downloads on click; one that errored retries on click; once
    // loaded, a click opens the viewer.
    if (loadState.status === 'not-downloaded' || loadState.status === 'error') {
      imageService.download(mediaUri);
      return;
    }
    onView?.();
  }

  onDestroy(cancelLongPress);

  const projectContext = useProjectContext();
  const writingSystemService = useWritingSystemService();

  // Show a single writing system: the first non-empty caption searching vernacular writing
  // systems first, then analysis — which is exactly the default order of allWritingSystems().
  const caption = $derived(writingSystemService.first(picture.caption) ?? '');

  // Load through the project-scoped image cache so a mediaUri shared by several pictures — shown in
  // a dialog, or revisited after navigating entries — is fetched once. On surfaces without a project
  // scope (stories), fall back to a component-local cache disposed with the component.
  const sharedImageService = useImageService();
  const localImageService = sharedImageService ? undefined : new ImageService(() => projectContext?.maybeApi);
  const imageService = sharedImageService ?? localImageService!;
  onDestroy(() => localImageService?.dispose());

  // A picture already available locally loads automatically; one that would have to be downloaded
  // from the remote media service shows a "click/tap to load" placeholder instead and is fetched
  // only when clicked. The cache is shared, so a mediaUri loaded once (here, in a dialog, or in
  // another entry) displays immediately everywhere.
  const mediaUri = $derived(picture.mediaUri);
  $effect(() => {
    imageService.ensureLocal(mediaUri);
  });
  const loadState = $derived(imageService.get(mediaUri));

  // Clickable to download (not available locally), to retry (after an error), or to open the viewer
  // (loaded and interactive).
  const needsDownload = $derived(loadState.status === 'not-downloaded');
  const hasError = $derived(loadState.status === 'error');
  const clickable = $derived(needsDownload || hasError || (loadState.status === 'loaded' && interactive));
  const showMenu = $derived(interactive && !!onEdit && !!onDownload && !!onDelete);
  const loadLabel = $derived(IsMobile.value ? $t`Tap to load` : $t`Click to load`);
  const retryLabel = $derived(IsMobile.value ? $t`Tap to retry` : $t`Click to retry`);
  const clickLabel = $derived(needsDownload ? loadLabel : hasError ? retryLabel : $t`View Picture`);

  function errorText(state: Extract<ImageLoadState, {status: 'error'}>): string {
    switch (state.reason) {
      case 'not-found':
        return $t`Image not found`;
      case 'offline':
        return $t`Offline, unable to download image`;
      default:
        return state.detail ?? $t`Unable to load image`;
    }
  }
</script>

{#snippet imageContent()}
  {#if loadState.status === 'loaded'}
    <!-- The image keeps its aspect ratio; thumbnail fixes the height, full caps to the viewport. -->
    <img src={loadState.url} alt={caption || $t`Picture`} class={imageClass} />
  {:else if loadState.status === 'not-downloaded'}
    <!-- Only available remotely: a click/tap downloads it (handled by the enclosing button). -->
    <div class="bg-muted text-muted-foreground flex h-40 w-40 flex-col items-center justify-center gap-1 rounded-md">
      <span class="i-mdi-download size-6"></span>
      <span class="text-sm">{loadLabel}</span>
    </div>
  {:else if loadState.status === 'loading'}
    <div class="bg-muted text-muted-foreground flex h-40 w-40 items-center justify-center rounded-md">
      <span class="i-mdi-loading size-6 animate-spin"></span>
    </div>
  {:else}
    <!-- Errored: a click/tap retries the load (handled by the enclosing button). -->
    <div class="bg-muted text-muted-foreground flex h-40 w-40 flex-col items-center justify-center gap-1 rounded-md text-center">
      <span class="i-mdi-image-broken-variant size-6"></span>
      <span class="text-sm">{errorText(loadState)}</span>
      <span class="text-xs">{retryLabel}</span>
    </div>
  {/if}
{/snippet}

<figure class="flex flex-col items-start gap-1">
  <!-- `w-fit` shrinks the box to the image so the actions menu sits on the image's corner. -->
  <div class="relative w-fit">
    {#if clickable}
      <button
        type="button"
        class="block cursor-pointer select-none appearance-none rounded-md border-0 bg-transparent p-0 focus-visible:outline-2 disabled:cursor-default"
        aria-label={clickLabel}
        disabled={busy}
        onclick={handleImageClick}
        onpointerdown={showMenu ? startLongPress : undefined}
        onpointerup={showMenu ? cancelLongPress : undefined}
        onpointercancel={showMenu ? cancelLongPress : undefined}
        onpointerleave={showMenu ? cancelLongPress : undefined}
        oncontextmenu={showMenu ? (e) => e.preventDefault() : undefined}
      >
        {@render imageContent()}
      </button>
    {:else}
      {@render imageContent()}
    {/if}
    {#if showMenu}
      <!-- Three-dots actions menu anchored to the image's top-right corner; also opened by a
           long-press on the image (startLongPress) for touch users. Available regardless of whether
           the image itself has been loaded yet. -->
      <div class="absolute right-1 top-1 z-10">
        <PictureActionsMenu
          bind:open={menuOpen}
          disabled={busy}
          triggerClass="text-foreground bg-background/70 hover:bg-background/90 rounded-full shadow-sm"
          onEdit={() => onEdit?.()}
          onDownload={() => onDownload?.()}
          onDelete={() => onDelete?.()}
        />
      </div>
    {/if}
  </div>
  {#if showCaption && caption}
    <!-- `w-0 min-w-full` pins the caption to the image's width (the box above), so its `max-width`
         is the picture width; line-clamp-2 caps it at two lines with an ellipsis. -->
    <figcaption class="text-muted-foreground line-clamp-2 w-0 min-w-full break-words text-center text-sm">
      {caption}
    </figcaption>
  {/if}
</figure>
