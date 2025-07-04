<script lang="ts" module>
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

  const zeroDuration = $derived(formatDuration({seconds: 0}, 'milliseconds', {
    hoursDisplay: 'auto',
    minutesDisplay: 'auto',
    style: 'digital',
    secondsDisplay: 'always',
    fractionalDigits: 2,
  }));

  const missingDuration = $derived(zeroDuration.replaceAll('0', '‒')); // <=  this "figure dash" is supposed to be the dash closest to the width of a number
</script>
<script lang="ts">
  import {onDestroy} from 'svelte';
  import {useEventListener, watch} from 'runed';
  import {useProjectContext} from '$lib/project-context.svelte';
  import {AppNotification} from '$lib/notifications/notifications';
  import {Button} from '$lib/components/ui/button';
  import {Slider} from '$lib/components/ui/slider';
  import {createSubscriber} from 'svelte/reactivity';
  import {on} from 'svelte/events';
  import {formatDuration, normalizeDuration} from '$lib/components/ui/format';
  import {t} from 'svelte-i18n-lingui';

  let {
    loader = defaultLoader,
    audioId,
  }: {
    loader?: (audioId: string) => Promise<ReadableStream | undefined>,
    audioId: string | undefined,
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
    if (!audio || loadedAudioId === audioId || !audioId) return;
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

  let sliderValue = $state(0);
  let pausedViaDragging = $state(false);
  let lastEmittedSliderValue = $state(0);
  async function onDraggingChange(dragging: boolean) {
    if (dragging) {
      if (playing) {
        pause();
        pausedViaDragging = true;
        sliderValue = lastEmittedSliderValue;
      }
    } else if (pausedViaDragging) {
      pausedViaDragging = false;
      if (audioRuned) audioRuned.currentTime = sliderValue;
      await play();
    }
  }

  watch(() => audioRuned?.currentTime, () => {
    if (!audioRuned) return;
    if (pausedViaDragging) {
      // not writing to the state is the only way to not override the slider's value at the begining of a drag i.e. when simply clicking
      // because it doesn't emit a value before we just stomp on it again
      return;
    }
    sliderValue = audioRuned.currentTime;
  })


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
  let playing = $derived(playerState === 'playing');
  let playIcon: 'i-mdi-play' | 'i-mdi-pause' = $derived(playing || pausedViaDragging ? 'i-mdi-pause' : 'i-mdi-play');
  let totalLength = $derived({
    hours: 0,
    minutes: 0,
    ...normalizeDuration({seconds: audioRuned?.duration})
  });
  let formatOpts = $derived<Intl.DurationFormatOptions>({
    style: 'digital',
    hoursDisplay: totalLength.hours > 0 ? 'always' : 'auto',
    minutesDisplay: totalLength.minutes > 0 ? 'always' : 'auto',
    secondsDisplay: 'always',
    fractionalDigits: 2,
  });
  let smallestUnit = $derived(totalLength.minutes > 0 ? 'seconds' as const : 'milliseconds' as const);
</script>
{#if supportsAudio}
  {#if audioId}
    <div class="flex space-x-3 items-center">
      {#if loading}
        <!--for some reason using the same button for loading, play and pause doesn't work and pause is never shown-->
        <Button loading size="sm-icon"></Button>
      {:else}
        <Button onclick={togglePlay} icon={playIcon} size="sm-icon"></Button>
      {/if}
      {#if audioRuned}
        <Slider type="single"
                class="pl-2"
                value={sliderValue}
                onValueChange={(value) => {
                  // store the value, because !playing is not necessarrily up to date when a drag starts
                  lastEmittedSliderValue = value;
                  // keep displayed time up to date while dragging
                  if (!playing) audioRuned.currentTime = sliderValue = value;
                }}
                onValueCommit={(value) => {
                  // sometimes all value change events are fired before pausedViaDragging === true
                  // then we need this
                  audioRuned.currentTime = sliderValue = value;
                }}
                {onDraggingChange}
                max={audioRuned?.duration}
                step={0.01} />
          <span class="break-keep text-nowrap pr-2 flex flex-nowrap gap-1">
            {#if !isNaN(audioRuned.duration)}
              <time>{formatDuration({seconds: sliderValue}, smallestUnit, formatOpts)}</time> / <time>{formatDuration(totalLength, smallestUnit, formatOpts)}</time>
            {:else}
              <time>{zeroDuration}</time> / <time class="text-muted-foreground">{missingDuration}</time>
            {/if}
        </span>
      {/if}
      <audio bind:this={audio} onplay={load}>
      </audio>
    </div>
  {:else}
    <div class="text-muted-foreground p-1">{$t`No audio`}</div>
  {/if}
{/if}
