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
    /** Opens the fullscreen viewer; without it (or when readonly) the picture isn't interactive
        and no actions menu is offered. */
    onView?: () => void;
    onEdit?: () => void;
    onDownload?: () => void;
    onDelete?: () => void;
    /** Disables the actions while an operation is in flight. */
    busy?: boolean;
    showCaption?: boolean;
    /** 'thumbnail' (default) is the fixed-height field size; 'full' fills the viewer up to the
        viewport while never exceeding the image's native size. */
    size?: 'thumbnail' | 'full';
    readonly?: boolean;
  };
  const {picture, onView, onEdit, onDownload, onDelete, busy = false, showCaption = true, size = 'thumbnail', readonly = false}: Props = $props();

  const interactive = $derived(!readonly && !!onView);
  const imageClass = $derived(
    size === 'full'
      ? 'max-h-[80dvh] w-auto max-w-full rounded-md object-contain'
      : 'h-40 w-auto rounded-md object-contain',
  );

  function handleImageClick() {
    if (loadState.status === 'not-downloaded' || loadState.status === 'error') {
      imageService.download(mediaUri);
      return;
    }
    onView?.();
  }

  const projectContext = useProjectContext();
  const writingSystemService = useWritingSystemService();

  // Show a single writing system: the first non-empty caption searching vernacular writing
  // systems first, then analysis — which is exactly the default order of allWritingSystems().
  const caption = $derived(writingSystemService.first(picture.caption) ?? '');

  // On surfaces that render pictures without an entry-view scope (new-entry dialog, activity/subject
  // previews, stories), fall back to a component-local cache disposed with the component.
  const sharedImageService = useImageService();
  const localImageService = sharedImageService ? undefined : new ImageService(() => projectContext?.maybeApi);
  const imageService = sharedImageService ?? localImageService!;
  onDestroy(() => localImageService?.dispose());

  const mediaUri = $derived(picture.mediaUri);
  $effect(() => {
    imageService.ensureLocal(mediaUri);
  });
  const loadState = $derived(imageService.get(mediaUri));

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
        return $t`Unable to load image`;
    }
  }
</script>

{#snippet imageContent()}
  {#if loadState.status === 'loaded'}
    <img src={loadState.url} alt={caption || $t`Picture`} class={imageClass} />
  {:else if loadState.status === 'not-downloaded'}
    <div class="bg-muted text-muted-foreground flex h-40 w-40 flex-col items-center justify-center gap-1 rounded-md">
      <span class="i-mdi-download size-6"></span>
      <span class="text-sm">{loadLabel}</span>
    </div>
  {:else if loadState.status === 'loading'}
    <div class="bg-muted text-muted-foreground flex h-40 w-40 items-center justify-center rounded-md">
      <span class="i-mdi-loading size-6 animate-spin"></span>
    </div>
  {:else}
    <div class="bg-muted text-muted-foreground flex h-40 w-40 flex-col items-center justify-center gap-1 rounded-md text-center">
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

<figure class="flex flex-col items-start gap-1">
  <!-- `w-fit` shrinks the box to the image so the actions menu sits on the image's corner. -->
  <div class="relative w-fit">
    {#if showMenu}
      <!-- The context-menu flavor gives right-click and touch long-press on the picture itself. -->
      <PictureActionsMenu
        contextMenu
        disabled={busy}
        onEdit={() => onEdit?.()}
        onDownload={() => onDownload?.()}
        onDelete={() => onDelete?.()}
      >
        {@render pictureArea()}
      </PictureActionsMenu>
      <div class="absolute right-1 top-1 z-10">
        <PictureActionsMenu
          disabled={busy}
          triggerClass="text-foreground bg-background/70 hover:bg-background/90 rounded-full shadow-sm"
          onEdit={() => onEdit?.()}
          onDownload={() => onDownload?.()}
          onDelete={() => onDelete?.()}
        />
      </div>
    {:else}
      {@render pictureArea()}
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
