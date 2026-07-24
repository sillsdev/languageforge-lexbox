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
    onView?: () => void;
    onEdit?: () => void;
    onDownload?: () => void;
    onDelete?: () => void;
    busy?: boolean;
    showCaption?: boolean;
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
  let displayState = $state<DisplayState>({status: 'loading'});
  const mediaUri = $derived(picture.mediaUri);
  const cached = $derived(imageService?.cached(mediaUri));
  const loadState = $derived(cached ?? displayState);

  function load(download: boolean) {
    if (!imageService) {
      displayState = {status: 'error', reason: 'unknown'};
      return;
    }
    const uri = mediaUri;
    displayState = {status: 'loading'};
    void imageService.loadImage(uri, {downloadIfMissing: download}).then((state) => {
      // Ignore a resolution for a picture we've since navigated away from.
      if (uri === picture.mediaUri) displayState = state;
    });
  }
  watch(() => mediaUri, () => load(false));

  let menuOpen = $state(false);
  function handleImageClick() {
    // A touch on the corner actions menu can fire a stray click on the image behind it once the menu
    // is open; ignore image taps while a menu is open so they don't also open the viewer/download.
    if (menuOpen) return;
    if (loadState.status === 'not-downloaded' || loadState.status === 'error') {
      load(true);
      return;
    }
    onView?.();
  }

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
    <img src={loadState.url} alt={caption || $t`Picture`} class={imageClass} />
  {:else if loadState.status === 'not-downloaded'}
    <div class="bg-muted text-muted-foreground hover:text-foreground flex h-40 w-40 flex-col items-center justify-center gap-1 rounded-md transition-colors">
      <span class="i-mdi-download size-6"></span>
      <span class="text-sm">{loadLabel}</span>
    </div>
  {:else if loadState.status === 'loading'}
    <div class="bg-muted text-muted-foreground flex h-40 w-40 items-center justify-center rounded-md">
      <span class="i-mdi-loading size-6 animate-spin"></span>
    </div>
  {:else}
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
    {@render pictureArea()}
  {:else}
    <!-- `w-fit` hugs the box to the image so the actions menu sits on its corner. -->
    <div class="relative w-fit">
      {#if showMenu}
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
