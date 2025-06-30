<script lang="ts">
  import {onDestroy} from 'svelte';
  import {useEventListener, watch} from 'runed';
  import {useProjectContext} from '$lib/project-context.svelte';
  import {AppNotification} from '$lib/notifications/notifications';
  import {Button} from '$lib/components/ui/button';
  import {Slider} from '$lib/components/ui/slider';
  import {createSubscriber} from 'svelte/reactivity';
  import {on} from 'svelte/events';
  const durationFormat = new Intl.DurationFormat(undefined, {style: 'digital'});

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
    playerState = 'loading';
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
    void audio?.play();
    playerState = 'playing';
  }

  function pause() {
    audio?.pause();
    playerState = 'paused';
  }

  function togglePlay() {
    if (playerState === 'playing') pause();
    else void play();
  }

  let loadedAudioId: string | undefined;
  let audio: HTMLAudioElement | undefined = $state(undefined);
  let audioRuned = $derived(audio ? new AudioRuned(audio) : null);
  useEventListener(() => audio, 'ended', () => playerState = 'paused');

  onDestroy(() => {
    if (!audio || !audio.src) return;
    URL.revokeObjectURL(audio.src);
  });
  let playerState = $state<'loading' | 'playing' | 'paused'>('paused');
  let loading = $derived(playerState === 'loading');
  let playIcon: 'i-mdi-play' | 'i-mdi-pause' = $derived(playerState === 'playing'
    ? 'i-mdi-pause'
    : 'i-mdi-play');
  class AudioRuned {
    #currentTimeSub = createSubscriber(update => {
      const off = on(this.audio, 'timeupdate', update);
      return () => off();
    });
    #durationSub = createSubscriber(update => on(this.audio, 'durationchange', update));
    constructor(private audio: HTMLAudioElement) {
    }
    get currentTime() {
      this.#currentTimeSub();
      return this.audio.currentTime;
    }
    set currentTime(value) {
      this.audio.currentTime = value;
    }
    get duration() {
      this.#durationSub();
      return this.audio.duration;
    }
  }
</script>
{#if supportsAudio}
  {#if audioId}
    <div class="flex space-x-2 items-center">
      {#if loading}
        <!--for some reason using the same button for loading, play and pause doesn't work and pause is never shown-->
        <Button loading size="icon"></Button>
      {:else}
        <Button onclick={togglePlay} icon={playIcon} size="icon"></Button>
      {/if}
      {#if audioRuned}
        <Slider type="single"
                class="pl-2"
                value={audioRuned.currentTime}
                onValueCommit={(v) => audioRuned.currentTime = v}
                max={audioRuned?.duration}
                step="0.01"/>
        {#if !isNaN(audioRuned.duration)}
          <span class="break-keep text-nowrap pr-2">
            {durationFormat.format({seconds: audioRuned.currentTime.toFixed(0)})} / {durationFormat.format({seconds: audioRuned.duration.toFixed(0)})}
          </span>
        {/if}
      {/if}
      <audio class="rounded"  bind:this={audio} onplay={load}>
      </audio>
    </div>
  {:else}
    <div class="text-gray-500">No Audio</div>
  {/if}
{/if}
