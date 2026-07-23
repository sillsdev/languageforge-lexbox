<script lang="ts">
  import type {IPicture} from '$lib/dotnet-types';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useWritingSystemService} from '$project/data';
  import {t} from 'svelte-i18n-lingui';
  import {onDestroy} from 'svelte';
  import {watch} from 'runed';
  import PictureActionsMenu from './PictureActionsMenu.svelte';
  import {ImageService, useImageService, type ImageState} from './image-service.svelte';

  type Props = {
    picture: IPicture;
    /** Opens the fullscreen viewer; without it (or when readonly) the picture isn't interactive
        and no actions menu is offered. */
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
    /** 'thumbnail' (default) is the fixed-height field size; 'full' fills its container (the
        viewer's fixed stage) while never exceeding the image's native size. */
    size?: 'thumbnail' | 'full';
    readonly?: boolean;
  };
  const {picture, onView, onEdit, onDownload, onDelete, busy = false, showCaption = true, size = 'thumbnail', readonly = false}: Props = $props();

  const interactive = $derived(!readonly && !!onView);
  const imageClass = $derived(
    size === 'full'
      ? 'max-h-full max-w-full h-auto w-auto object-contain'
      : 'h-40 w-auto rounded-md object-contain',
  );

  const projectContext = useProjectContext();
  const writingSystemService = useWritingSystemService();
  const imageService = getImageService();

  // Show a single writing system: the first non-empty caption searching vernacular writing
  // systems first, then analysis — which is exactly the default order of allWritingSystems().
  const caption = $derived(writingSystemService.first(picture.caption) ?? '');

  function getImageService() {
    const sharedImageService = useImageService();
    // prefer the shared service, which can cache across components
    if (sharedImageService) return sharedImageService;
    if (!projectContext) return undefined;
    const localImageService = new ImageService(() => projectContext.api);
    onDestroy(() => localImageService.dispose());
    return localImageService;
  }

  type DisplayState = {status: 'loading'} | ImageState;
  let loadState = $state<DisplayState>({status: 'loading'});

  // A picture already available locally loads automatically; one that would have to be downloaded
  // from the remote media service resolves to 'not-downloaded' (the "Load picture" placeholder) and
  // is fetched only when clicked (download=true). A click on an errored picture retries the same way.
  const mediaUri = $derived(picture.mediaUri);
  function load(download: boolean) {
    if (!imageService) {
      loadState = {status: 'error', reason: 'unknown'};
      return;
    }
    const uri = mediaUri;
    loadState = {status: 'loading'};
    void imageService.loadImage(uri, {downloadIfMissing: download}).then((state) => {
      // Ignore a resolution for a picture we've since navigated away from.
      if (uri === picture.mediaUri) loadState = state;
    });
  }
  watch(() => mediaUri, () => load(false));

  // A touch on the corner actions menu can fire a stray click on the image behind it once the menu
  // is open; ignore image taps while a menu is open so they don't also open the viewer/download.
  let menuOpen = $state(false);

  function handleImageClick() {
    if (menuOpen) return;
    if (loadState.status === 'not-downloaded' || loadState.status === 'error') {
      load(true);
      return;
    }
    onView?.();
  }

  // Clickable to download (not available locally), to retry (after an error), or to open the viewer
  // (loaded and interactive).
  const needsDownload = $derived(loadState.status === 'not-downloaded');
  const hasError = $derived(loadState.status === 'error');
  const clickable = $derived(needsDownload || hasError || (loadState.status === 'loaded' && interactive));
  const showMenu = $derived(interactive && !!onEdit && !!onDownload && !!onDelete);
  const loadLabel = $derived($t`Load picture`);
  const retryLabel = $derived($t`Try again`);
  const clickLabel = $derived(needsDownload ? loadLabel : hasError ? retryLabel : $t`View Picture`);

  function errorText(state: Extract<ImageState, {status: 'error'}>): string {
    switch (state.reason) {
      case 'not-found':
        return $t`Picture not found`;
      case 'offline':
        return $t`You're offline`;
      default:
        return $t`Unable to load picture`;
    }
  }
</script>

{#snippet imageContent()}
  {#if loadState.status === 'loaded'}
    <!-- The image keeps its aspect ratio; thumbnail fixes the height, full fits its container. -->
    <img src={loadState.url} alt={caption || $t`Picture`} class={imageClass} />
  {:else if loadState.status === 'not-downloaded'}
    <!-- Only available remotely: a click/tap downloads it (handled by the enclosing button). -->
    <div class="bg-muted text-muted-foreground hover:text-foreground flex h-40 w-40 flex-col items-center justify-center gap-1 rounded-md transition-colors">
      <span class="i-mdi-download size-6"></span>
      <span class="text-sm">{loadLabel}</span>
    </div>
  {:else if loadState.status === 'loading'}
    <div class="bg-muted text-muted-foreground flex h-40 w-40 items-center justify-center rounded-md">
      <span class="i-mdi-loading size-6 animate-spin"></span>
    </div>
  {:else}
    <!-- Errored: a click/tap retries the load (handled by the enclosing button). -->
    <div class="bg-muted text-muted-foreground hover:text-foreground flex h-40 w-40 flex-col items-center justify-center gap-1 rounded-md text-center transition-colors">
      <span class="i-mdi-image-broken-variant size-6"></span>
      <span class="text-sm">{errorText(loadState)}</span>
      <span class="text-xs">{retryLabel}</span>
    </div>
  {/if}
{/snippet}

{#snippet pictureArea()}
  {#if clickable}
    <button
      type="button"
      class="block cursor-pointer appearance-none rounded-md border-0 bg-transparent p-0 focus-visible:outline-2 disabled:cursor-default"
      aria-label={clickLabel}
      disabled={busy}
      onclick={handleImageClick}
    >
      {@render imageContent()}
    </button>
  {:else}
    {@render imageContent()}
  {/if}
{/snippet}

<figure class={size === 'full' ? 'flex size-full min-h-0 min-w-0 items-center justify-center' : 'flex flex-col items-start gap-1'}>
  {#if size === 'full'}
    <!-- The image caps to the figure (a definite-height flex box); a `w-fit`/auto-height wrapper
         would leave the height unconstrained and let a tall image overflow. No corner menu in this
         mode — the viewer dialog owns the actions menu (in its header). -->
    {@render pictureArea()}
  {:else}
    <!-- `w-fit` hugs the box to the image so the actions menu sits on its corner. -->
    <div class="relative w-fit">
      {#if showMenu}
        <!-- The context-menu flavor gives right-click and touch long-press on the picture itself. -->
        <PictureActionsMenu
          contextMenu
          disabled={busy}
          onOpenChange={(o) => (menuOpen = o)}
          onEdit={() => onEdit?.()}
          onDownload={() => onDownload?.()}
          onDelete={() => onDelete?.()}
        >
          {@render pictureArea()}
        </PictureActionsMenu>
        <div class="absolute right-1 top-1">
          <PictureActionsMenu
            disabled={busy}
            triggerClass="rounded-full not-hover:bg-background/50 shadow-sm backdrop-blur-sm hover:bg-background/90"
            onOpenChange={(o) => (menuOpen = o)}
            onEdit={() => onEdit?.()}
            onDownload={() => onDownload?.()}
            onDelete={() => onDelete?.()}
          />
        </div>
      {:else}
        {@render pictureArea()}
      {/if}
    </div>
  {/if}
  {#if showCaption && caption}
    <!-- `w-0 min-w-full` pins the caption to the image's width (the box above), so its `max-width`
         is the picture width; line-clamp-2 caps it at two lines with an ellipsis. -->
    <figcaption class="text-muted-foreground line-clamp-2 w-0 min-w-full wrap-break-word text-center text-sm">
      {caption}
    </figcaption>
  {/if}
</figure>
