<script lang="ts" module>
  import {createSubscriber} from 'svelte/reactivity';
  import {on} from 'svelte/events';
  class AudioRuned {
    #currentTimeSub = createSubscriber(update => on(this.audio, 'timeupdate', update));
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
      let duration = this.audio.duration;
      //avoids bug: https://github.com/huntabyte/bits-ui/issues/1663
      if (duration === Infinity) duration = NaN;
      return duration;
    }
  }

  const zeroDuration = $derived(formatDuration({seconds: 0}, 'milliseconds', {
    hoursDisplay: 'auto',
    minutesDisplay: 'auto',
    style: 'digital',
    secondsDisplay: 'always',
    fractionalDigits: 2,
  }));

  function isNotFoundAudioId(audioId: string) {
    return audioId === 'sil-media://not-found/00000000-0000-0000-0000-000000000000';
  }

  const missingDuration = $derived(zeroDuration.replaceAll('0', '‒')); // <=  this "figure dash" is supposed to be the dash closest to the width of a number
</script>
<script lang="ts">
  import {onDestroy} from 'svelte';
  import {useEventListener, watch} from 'runed';
  import {useProjectContext} from '$lib/project-context.svelte';
  import {AppNotification} from '$lib/notifications/notifications';
  import {Button} from '$lib/components/ui/button';
  import {Slider} from '$lib/components/ui/slider';
  import {formatDuration, normalizeDuration} from '$lib/components/ui/format';
  import {t} from 'svelte-i18n-lingui';
  import {ReadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ReadFileResult';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {isDev} from '$lib/layout/DevContent.svelte';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';

  const handled = Symbol();
  let {
    loader = defaultLoader,
    audioId = $bindable(),
    onchange = () => {},
  }: {
    loader?: (audioId: string) => Promise<ReadableStream | undefined | typeof handled>,
    audioId: string | undefined,
    onchange?: (audioId: string | undefined) => void;
  } = $props();

  const projectContext = useProjectContext();
  const api = $derived(projectContext?.maybeApi);
  const supportsAudio = $derived(projectContext?.features.audio);
  const dialogService = useDialogsService();

  async function defaultLoader(audioId: string) {
    if (!api) throw new Error('No api, unable to load audio');
    const file = await api.getFileStream(audioId);
    if (!file.stream) {
      switch (file.result){
        case ReadFileResult.NotFound:
          AppNotification.display($t`File not found`, 'warning');
          break;
        case ReadFileResult.Offline:
          AppNotification.display($t`Offline, unable to download`, 'warning');
          break;
        default:
          AppNotification.error($t`Unknown error ${file.errorMessage ?? file.result}`);
          break;
      }

      return handled;
    }
    return await file.stream.stream();
  }

  async function load() {
    if (!audio || loadedAudioId === audioId || !audioId) return !!audioId;
    playerState = 'loading';
    try {
      const stream = await loader(audioId);
      if (stream === handled) return false;
      if (!stream) {
        AppNotification.error(`Failed to load audio ${audioId}`);
        return;
      }
      let blob = await new Response(stream).blob();
      if (audio.src) URL.revokeObjectURL(audio.src);
      audio.src = URL.createObjectURL(blob);
      loadedAudioId = audioId;
      return true;
    } finally {
      playerState = 'paused';
    }
  }

  async function play() {
    if (!await load()) return;
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
  watch(() => audio, (current, previous) => {
    if (previous?.src) URL.revokeObjectURL(previous.src);
    playerState = 'paused';
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

  async function onGetAudioClick() {
    const result = await dialogService.getAudio();
    if (result) {
      audioId = result;
      onchange(audioId)
    }
  }

  function onRemoveAudio() {
    try {
      audioId = undefined;
      onchange(audioId);
    } finally {
      if (audio && audio.src) {
        URL.revokeObjectURL(audio.src);
        audio.src = '';
      }
    }
  }
</script>
{#if supportsAudio}
  {#if !audioId}
    <Button variant="secondary" icon="i-mdi-microphone-plus" size="sm" iconProps={{class: 'size-5'}} onclick={onGetAudioClick}>
      {$t`Add audio`}
    </Button>
  {:else if isNotFoundAudioId(audioId)}
    <div class="text-muted-foreground p-1">
      {$t`Audio file not included in Send & Receive`}
    </div>
  {:else}
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
                step={0.01}/>
        <span class="break-keep text-nowrap flex flex-nowrap gap-1">
          {#if !isNaN(audioRuned.duration)}
            <time>{formatDuration({seconds: sliderValue}, smallestUnit, formatOpts)}</time> / <time>{formatDuration(totalLength, smallestUnit, formatOpts)}</time>
          {:else}
            <time>{zeroDuration}</time> / <time class="text-muted-foreground">{missingDuration}</time>
          {/if}
        </span>
        <ResponsiveMenu.Root>
          <ResponsiveMenu.Trigger>
            {#snippet child({props})}
              <Button variant="secondary" icon="i-mdi-dots-vertical" size="sm-icon" {...props} />
            {/snippet}
          </ResponsiveMenu.Trigger>
          <ResponsiveMenu.Content>
            <ResponsiveMenu.Item icon="i-mdi-microphone-plus" onSelect={onGetAudioClick}>
              {$t`Replace audio`}
            </ResponsiveMenu.Item>
            <ResponsiveMenu.Item icon="i-mdi-delete" onSelect={onRemoveAudio}>
              {$t`Remove audio`}
            </ResponsiveMenu.Item>
          </ResponsiveMenu.Content>
        </ResponsiveMenu.Root>
      {/if}
      {#key audioId}
        <audio bind:this={audio}>
        </audio>
      {/key}
    </div>
  {/if}
{/if}
