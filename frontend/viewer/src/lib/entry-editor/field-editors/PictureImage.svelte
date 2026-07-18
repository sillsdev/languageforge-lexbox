<script lang="ts">
  import type {IPicture} from '$lib/dotnet-types';
  import {ReadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ReadFileResult';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useWritingSystemService} from '$project/data';
  import {t} from 'svelte-i18n-lingui';
  import {onDestroy} from 'svelte';
  import PictureActionsMenu from './PictureActionsMenu.svelte';

  type Props = {
    picture: IPicture;
    /** When provided (and not readonly), clicking the picture opens the edit dialog and a three-dots
        actions menu is shown; the menu also opens on a long-press of the picture. */
    onEdit?: () => void;
    /** Downloads the picture; wired into the actions menu (and long-press menu). */
    onDownload?: () => void;
    /** Deletes the picture (with confirmation); wired into the actions menu. */
    onDelete?: () => void;
    /** Disables the actions affordance while an operation is in flight. */
    busy?: boolean;
    /** Whether to render the caption beneath the picture (hidden inside the edit dialog). */
    showCaption?: boolean;
    readonly?: boolean;
  };
  const {picture, onEdit, onDownload, onDelete, busy = false, showCaption = true, readonly = false}: Props = $props();

  // With an edit handler wired (and not readonly) the picture is interactive: clicking opens the
  // edit dialog and a three-dots menu offers the actions.
  const interactive = $derived(!readonly && !!onEdit);

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
    onEdit?.();
  }

  onDestroy(cancelLongPress);

  const projectContext = useProjectContext();
  const api = $derived(projectContext?.maybeApi);
  const writingSystemService = useWritingSystemService();

  // Show a single writing system: the first non-empty caption searching vernacular writing
  // systems first, then analysis — which is exactly the default order of allWritingSystems().
  const caption = $derived(writingSystemService.first(picture.caption) ?? '');

  type LoadState =
    | {status: 'loading'}
    | {status: 'loaded'; url: string}
    | {status: 'error'; message: string};

  // getFileStream signals failures via the `result` enum (not exceptions), so we
  // branch on it rather than wrapping in try/catch (the global handler covers throws).
  async function loadImage(mediaUri: string): Promise<Exclude<LoadState, {status: 'loading'}>> {
    if (!api) return {status: 'error', message: $t`Unable to load image`};
    const file = await api.getFileStream(mediaUri);
    if (!file.stream) {
      switch (file.result) {
        case ReadFileResult.NotFound:
          return {status: 'error', message: $t`Image not found`};
        case ReadFileResult.Offline:
          return {status: 'error', message: $t`Offline, unable to download image`};
        default:
          return {status: 'error', message: file.errorMessage ?? $t`Unable to load image`};
      }
    }
    const blob = await new Response(await file.stream.stream()).blob();
    return {status: 'loaded', url: URL.createObjectURL(blob)};
  }

  let loadState = $state<LoadState>({status: 'loading'});

  const mediaUri = $derived(picture.mediaUri);
  $effect(() => {
    loadState = {status: 'loading'};
    let revoked = false;
    let createdUrl: string | undefined;
    void loadImage(mediaUri).then((result) => {
      if (revoked) {
        // Component/effect was torn down before the image finished loading.
        if (result.status === 'loaded') URL.revokeObjectURL(result.url);
        return;
      }
      if (result.status === 'loaded') createdUrl = result.url;
      loadState = result;
    });
    return () => {
      revoked = true;
      if (createdUrl) URL.revokeObjectURL(createdUrl);
    };
  });
</script>

{#snippet imageContent()}
  {#if loadState.status === 'loaded'}
    <!-- Fixed height, flexible width: the image keeps its aspect ratio and its width varies. -->
    <img src={loadState.url} alt={caption || $t`Picture`} class="h-40 w-auto rounded-md object-contain" />
  {:else if loadState.status === 'loading'}
    <div class="bg-muted text-muted-foreground flex h-40 w-40 items-center justify-center rounded-md">
      <span class="i-mdi-loading size-6 animate-spin"></span>
    </div>
  {:else}
    <div class="bg-muted text-muted-foreground flex h-40 w-40 flex-col items-center justify-center gap-1 rounded-md">
      <span class="i-mdi-image-broken-variant size-6"></span>
      <span class="text-sm">{loadState.message}</span>
    </div>
  {/if}
{/snippet}

<figure class="flex flex-col items-start gap-1">
  <!-- `w-fit` shrinks the box to the image so the actions menu sits on the image's corner. -->
  <div class="relative w-fit">
    {#if interactive}
      <button
        type="button"
        class="block cursor-pointer select-none appearance-none rounded-md border-0 bg-transparent p-0 focus-visible:outline-2 disabled:cursor-default"
        aria-label={$t`Edit Picture`}
        disabled={busy}
        onclick={handleImageClick}
        onpointerdown={startLongPress}
        onpointerup={cancelLongPress}
        onpointercancel={cancelLongPress}
        onpointerleave={cancelLongPress}
        oncontextmenu={(e) => e.preventDefault()}
      >
        {@render imageContent()}
      </button>
      {#if onDownload && onDelete}
        <!-- Three-dots actions menu anchored to the image's top-right corner; also opened by a
             long-press on the image (startLongPress) for touch users. -->
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
    {:else}
      {@render imageContent()}
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
