<script lang="ts">
  import type {IPicture} from '$lib/dotnet-types';
  import {ReadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ReadFileResult';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useWritingSystemService} from '$project/data';
  import {t} from 'svelte-i18n-lingui';

  type Props = {
    picture: IPicture;
    /** When provided, clicking the image triggers a replace. */
    onReplace?: () => void;
    /** When provided, a trash button is shown in the image's top-right corner. */
    onDelete?: () => void;
    /** Disables the edit affordances while an operation is in flight. */
    busy?: boolean;
  };
  const {picture, onReplace, onDelete, busy = false}: Props = $props();

  // The image doubles as edit UI (tap to replace, trash to delete) only when handlers are wired.
  // Kept media-agnostic and hover-independent so it works on touch screens.
  const editable = $derived(!!onReplace || !!onDelete);

  const projectContext = useProjectContext();
  const api = $derived(projectContext?.maybeApi);
  const writingSystemService = useWritingSystemService();

  const caption = $derived(writingSystemService.first(picture.caption, writingSystemService.analysis) ?? '');

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

  $effect(() => {
    const mediaUri = picture.mediaUri;
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
    <img src={state.url} alt={caption || $t`Picture`} class="max-h-64 max-w-full rounded-md object-contain" />
  {:else if state.status === 'loading'}
    <div class="bg-muted text-muted-foreground flex h-32 w-64 items-center justify-center rounded-md">
      <span class="i-mdi-loading size-6 animate-spin"></span>
    </div>
  {:else}
    <div class="bg-muted text-muted-foreground flex h-32 w-64 flex-col items-center justify-center gap-1 rounded-md">
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
        aria-label={$t`Replace Picture`}
        disabled={busy}
        onclick={() => onReplace?.()}
      >
        {@render imageContent()}
      </button>
      <button
        type="button"
        class="text-destructive bg-background/70 hover:bg-background absolute right-1 top-1 z-10 rounded-full p-1 shadow-sm transition-colors disabled:opacity-50"
        aria-label={$t`Delete Picture`}
        disabled={busy}
        onclick={() => onDelete?.()}
      >
        <span class="i-mdi-delete size-5"></span>
      </button>
    {:else}
      {@render imageContent()}
    {/if}
  </div>
  {#if caption}
    <figcaption class="text-muted-foreground text-center text-sm">{caption}</figcaption>
  {/if}
</figure>
