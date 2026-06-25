<script lang="ts">
  import type {IPicture} from '$lib/dotnet-types';
  import {ReadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ReadFileResult';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useWritingSystemService} from '$project/data';
  import {t} from 'svelte-i18n-lingui';

  type Props = {
    picture: IPicture;
  };
  const {picture}: Props = $props();

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

<figure class="flex flex-col items-center gap-1">
  {#if state.status === 'loaded'}
    <img src={state.url} alt={caption || $t`Picture`} class="max-h-64 max-w-full rounded-md object-contain" />
  {:else if state.status === 'loading'}
    <div class="bg-muted text-muted-foreground flex h-32 w-full items-center justify-center rounded-md">
      <span class="i-mdi-loading size-6 animate-spin"></span>
    </div>
  {:else}
    <div
      class="bg-muted text-muted-foreground flex h-32 w-full flex-col items-center justify-center gap-1 rounded-md"
    >
      <span class="i-mdi-image-broken-variant size-6"></span>
      <span class="text-sm">{state.message}</span>
    </div>
  {/if}
  {#if caption}
    <figcaption class="text-muted-foreground text-center text-sm">{caption}</figcaption>
  {/if}
</figure>
