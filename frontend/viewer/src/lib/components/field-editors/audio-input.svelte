<script lang="ts">
  import {onDestroy} from 'svelte';
  import {watch} from 'runed';
  import {useProjectContext} from '$lib/project-context.svelte';
  import {AppNotification} from '$lib/notifications/notifications';

  let {
    loader = defaultLoader,
    audioId,
  }: {
    loader?: (audioId: string) => Promise<ReadableStream | undefined>,
    audioId: string,
  } = $props();

  watch(() => audioId, () => {
    if (!audio || !audio.src) return;
    URL.revokeObjectURL(audio.src);
  });
  const projectContext = useProjectContext();
  const api = $derived(projectContext?.maybeApi);
  const supportsAudio = $derived(projectContext?.features.audio);

  async function defaultLoader(audioId: string) {
    if (!api) throw new Error('No api, unable to load audio');
    const dotnetStream = await api.getFileStream(audioId);
    if (!dotnetStream) return;
    return await dotnetStream.stream();
  }

  async function load() {
    if (!audio || loadedAudioId === audioId) return;
    const stream = await loader(audioId);
    if (!stream) {
      AppNotification.display(`Failed to load audio ${audioId}`, 'error', 'long');
      return;
    }
    let blob = await new Response(stream).blob();
    if (audio.src) URL.revokeObjectURL(audio.src);
    audio.src = URL.createObjectURL(blob);
    loadedAudioId = audioId;
  }

  async function play() {
    await load();
    audio?.play();
  }

  let loadedAudioId: string | undefined;
  let audio: HTMLAudioElement | undefined = $state(undefined);
  onDestroy(() => {
    if (!audio || !audio.src) return;
    URL.revokeObjectURL(audio.src);
  });
</script>
{#if supportsAudio}
  {#if audioId}
    <div class="flex gap-2">
      <audio class="rounded" controls bind:this={audio} onplay={load}>
      </audio>
      <!--chrome doesn't enable the play button when there's no src so we need this hack, shouldn't be needed once we implement our own player-->
      <button onclick={play}>Play Chrome</button>
    </div>
  {:else}
    <div class="text-gray-500">No Audio</div>
  {/if}
{/if}
