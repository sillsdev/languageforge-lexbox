<script lang="ts">
  import type {IPicture} from '$lib/dotnet-types';
  import {ReadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ReadFileResult';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useWritingSystemService} from '$project/data';
  import {t} from 'svelte-i18n-lingui';

  type Props = {
    picture: IPicture;
    /** When provided, the whole picture is clickable (with a pencil hint) to open the edit dialog. */
    onEdit?: () => void;
    /** Disables the edit affordance while an operation is in flight. */
    busy?: boolean;
    /** Whether to render the caption beneath the picture (hidden inside the edit dialog). */
    showCaption?: boolean;
  };
  const {picture, onEdit, busy = false, showCaption = true}: Props = $props();

  // When an edit handler is wired the picture becomes a button that opens the edit dialog. The
  // whole picture is clickable; the pencil is only a hover-independent hint (works on touch).
  const editable = $derived(!!onEdit);

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

  let state = $state<LoadState>({status: 'loading'});

  const mediaUri = $derived(picture.mediaUri);
  $effect(() => {
    state = {status: 'loading'};
    let revoked = false;
    let createdUrl: string | undefined;
    void loadImage(mediaUri).then((result) => {
      if (revoked) {
        // Component/effect was torn down before the image finished loading.
        if (result.status === 'loaded') URL.revokeObjectURL(result.url);
        return;
      }
      if (result.status === 'loaded') createdUrl = result.url;
      state = result;
    });
    return () => {
      revoked = true;
      if (createdUrl) URL.revokeObjectURL(createdUrl);
    };
  });
</script>

{#snippet imageContent()}
  {#if state.status === 'loaded'}
    <!-- Fixed height, flexible width: the image keeps its aspect ratio and its width varies. -->
    <img src={state.url} alt={caption || $t`Picture`} class="h-40 w-auto rounded-md object-contain" />
  {:else if state.status === 'loading'}
    <div class="bg-muted text-muted-foreground flex h-40 w-40 items-center justify-center rounded-md">
      <span class="i-mdi-loading size-6 animate-spin"></span>
    </div>
  {:else}
    <div class="bg-muted text-muted-foreground flex h-40 w-40 flex-col items-center justify-center gap-1 rounded-md">
      <span class="i-mdi-image-broken-variant size-6"></span>
      <span class="text-sm">{state.message}</span>
    </div>
  {/if}
{/snippet}

<figure class="flex flex-col items-start gap-1">
  <!-- `w-fit` shrinks the box to the image so the trash button sits on the image's corner. -->
  <div class="relative w-fit">
    {#if editable}
      <button
        type="button"
        class="block cursor-pointer appearance-none rounded-md border-0 bg-transparent p-0 focus-visible:outline-2 disabled:cursor-default"
        aria-label={$t`Edit Picture`}
        disabled={busy}
        onclick={() => onEdit?.()}
      >
        {@render imageContent()}
        <!-- Pencil hint that the picture is clickable; the click is handled by the button itself. -->
        <span class="text-foreground bg-background/70 pointer-events-none absolute right-1 top-1 z-10 rounded-full p-1 shadow-sm">
          <span class="i-mdi-pencil block size-5"></span>
        </span>
      </button>
    {:else}
      {@render imageContent()}
    {/if}
  </div>
  {#if showCaption && caption}
    <!-- `w-0 min-w-full` pins the caption to the image's width (the box above), so its `max-width`
         is the picture width; line-clamp-2 caps it at two lines with an ellipsis. -->
    <figcaption class="text-muted-foreground line-clamp-2 w-0 min-w-full break-words text-sm">
      {caption}
    </figcaption>
  {/if}
</figure>
