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
  import {useProjectContext} from '$project/project-context.svelte';
  import {AppNotification} from '$lib/notifications/notifications';
  import {Button} from '$lib/components/ui/button';
  import {Slider} from '$lib/components/ui/slider';
  import {formatDuration, normalizeDuration} from '$lib/components/ui/format';
  import {t} from 'svelte-i18n-lingui';
  import {ReadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ReadFileResult';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import AudioDialog from '$lib/components/audio/AudioDialog.svelte';
  import {tryUseFieldBody} from '$lib/components/editor/field/field-root.svelte';
  import {useSubjectContext} from '$lib/entry-editor/object-editors/subject-context';
  import LexiconEditorPrimitive from '$lib/entry-editor/object-editors/LexiconEditorPrimitive.svelte';
  import OverrideFields from '$lib/views/OverrideFields.svelte';

  const handled = Symbol();
  let {
    loader = defaultLoader,
    audioId = $bindable(),
    onchange = () => {},
    readonly = false,
    wsLabel = undefined,
  }: {
    loader?: (audioId: string) => Promise<{stream: ReadableStream, filename: string} | undefined | typeof handled>,
    audioId: string | undefined,
    onchange?: (audioId: string | undefined) => void;
    readonly?: boolean;
    wsLabel?: string;
  } = $props();
  watch(() => audioId, () => loadedAudioId = undefined);

  const projectContext = useProjectContext();
  const api = $derived(projectContext?.maybeApi);
  const supportsAudio = $derived(projectContext?.features.audio);
  const fieldProps = tryUseFieldBody();

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
    return {stream: await file.stream.stream(), filename: file.fileName ?? ''};
  }

  async function load() {
    if (!audio || loadedAudioId === audioId || !audioId) return !!audioId;
    playerState = 'loading';
    try {
      const result = await loader(audioId);
      if (result === handled) return false;
      if (!result) {
        AppNotification.error(`Failed to load audio ${audioId}`);
        return;
      }
      let blob = await new Response(result.stream).blob();
      if (audio.src) URL.revokeObjectURL(audio.src);
      loadedAudioId = undefined;
      audio.src = URL.createObjectURL(blob);
      filename = result.filename;
      loadedAudioId = audioId;
      return true;
    } finally {
      // some general resetting
      playerState = 'paused';
      sliderValue = 0;
      isDragging = false;
    }
  }

  async function play() {
    if (!await load() || !audio) return;

    // The only known error is the one referenced by audioHasKnownFlacSeekError()
    // So, all error handling is currently designed around that
    // but why not just try to generalize to all errors
    if (audio.error) {
      // We land here if the user drags the slider to a "bad position" while the audio is explicitly paused.
      console.log('Audio error. Trying to recover by reloading to beginning');
      audio.load();
    }

    try {
      await audio.play();
    } catch {
      // We land here if the user drags the slider to a "bad position" while the audio is only implicitly paused via dragging.
      // The "bad seek" is applied on mouse release and hasn't had time to trigger an error before we call audio.play().
      console.log('Error playing audio. Trying to recover by reloading and playing from beginning');
      audio.load();
      await audio.play();
    }
    playerState = 'playing';
  }

  function pause() {
    audio?.pause();
    playerState = 'paused';
  }

  async function togglePlay() {
    if (playerState === 'playing') pause();
    else await play();
  }

  let sliderValue = $state(0);
  let pausedViaDragging = $state(false);
  let lastEmittedSliderValue = $state(0);
  let isDragging = $state(false);
  async function onDraggingChange(dragging: boolean) {
    isDragging = dragging;
    if (dragging) {
      if (playing) {
        pause();
        pausedViaDragging = true;
        sliderValue = lastEmittedSliderValue;
      }
    } else if (pausedViaDragging) {
      try {
        if (audioRuned) audioRuned.currentTime = sliderValue;
        await play();
      } finally {
        pausedViaDragging = false;
      }
    }
  }

  watch(() => audioRuned?.currentTime, () => {
    if (!audioRuned) return;
    if (!playing) {
      // not writing to the state is the only way to not override the slider's value at the begining of a drag i.e. when simply clicking
      // because it doesn't emit a value before we just stomp on it again
      return;
    }
    sliderValue = audioRuned.currentTime;
  })


  let loadedAudioId = $state<string>();
  let filename = $state('');
  let audio = $state<HTMLAudioElement>();
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
  let loaded = $derived(audio && audioId && loadedAudioId === audioId);
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

  let audioDialogOpen = $state(false);
  function onAudioDialogSubmit(newAudioId: string) {
    audioId = newAudioId;
    onchange(newAudioId);
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

  async function onSaveAs() {
    if (!audio) return;
    await load();
    //todo sadly this only works on desktop, not mobile, but it's the same with save as with the audio editor.
    const a = document.createElement('a');
    a.href = audio.src;
    a.download = filename;
    a.click();
  }

  function onAudioError(event: Event) {
    if (audioHasKnownFlacSeekError()) {
      console.log('Ignoring known FLAC seek error. Will try to recover on next play.');
    } else if (audio?.error) {
      throw new Error('Audio error', { cause: audio.error });
    } else {
      throw new Error('Unknown audio error', { cause: event });
    }
  }

  function audioHasKnownFlacSeekError() {
    if (!audio?.error) return false;
    // The error gets triggered in Chrome when seeking (via drag or just clicking).
    // There's a problematic time range near (not at) the end of flac files that causes this error.
    return audio.error.code === MediaError.MEDIA_ERR_NETWORK &&
      audio.error.message?.includes('demuxer seek failed');
  }
  let dialogTitle = $derived(fieldProps?.label && wsLabel ? `${fieldProps.label}: ${wsLabel}` : fieldProps?.label || wsLabel);
  let subject = useSubjectContext();
</script>
{#if supportsAudio}
  {#if !readonly}
    <AudioDialog title={dialogTitle} bind:open={audioDialogOpen} onSubmit={onAudioDialogSubmit}>
      {#if subject?.current}
        <OverrideFields shownFields={fieldProps?.fieldId ? [fieldProps.fieldId] : []}>
          <LexiconEditorPrimitive object={subject.current}/>
        </OverrideFields>
      {/if}
    </AudioDialog>
  {/if}
  {#if !audioId}
    {#if !readonly}
      <Button variant="secondary"
              icon="i-mdi-microphone-plus"
              size="sm"
              iconProps={{class: 'size-5'}}
              onclick={() => audioDialogOpen = true}>
        {$t`Add audio`}
      </Button>
    {:else}
      <div class="text-muted-foreground p-1">
        {$t`No audio`}
      </div>
    {/if}
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
                disabled={!loaded}
                value={sliderValue}
                onValueChange={(value) => {
                  // store the value, because dragging (next line) is not necessarrily up to date when a drag starts
                  lastEmittedSliderValue = value;
                  // keep displayed time up to date while dragging
                  if (isDragging) sliderValue = value;
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
            {#if !readonly}
              <ResponsiveMenu.Item icon="i-mdi-microphone-plus" onSelect={() => audioDialogOpen = true}>
                {$t`Replace audio`}
              </ResponsiveMenu.Item>
              <ResponsiveMenu.Item icon="i-mdi-delete" onSelect={onRemoveAudio}>
                {$t`Remove audio`}
              </ResponsiveMenu.Item>
            {/if}
            <ResponsiveMenu.Item icon="i-mdi-download" onSelect={onSaveAs}>
              {$t`Save As`}
            </ResponsiveMenu.Item>
          </ResponsiveMenu.Content>
        </ResponsiveMenu.Root>
      {/if}
      {#key audioId}
        <audio bind:this={audio} onerror={onAudioError}>
        </audio>
      {/key}
    </div>
  {/if}
{/if}
