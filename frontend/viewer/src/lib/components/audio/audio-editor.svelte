<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import Button from '../ui/button/button.svelte';
  import Waveform from './wavesurfer/waveform.svelte';
  import type WaveSurfer from 'wavesurfer.js';
  import {formatDigitalDuration} from '../ui/format/format-duration';
  import DevContent from '$lib/layout/DevContent.svelte';
  import {Label} from '../ui/label';
  import {FFmpegApi} from './ffmpeg';
  import Loading from '$lib/components/Loading.svelte';
  import {resource, watch} from 'runed';
  import {onDestroy} from 'svelte';

  type Props = {
    audio: File;
    finalAudio: File | undefined;
    onDiscard: () => void;
  };

  let {audio, finalAudio = $bindable(undefined), onDiscard}: Props = $props();

  let audioApi = $state<WaveSurfer>();
  let playing = $state(false);
  let duration = $state<number | null>(null);
  const mb = $derived(!finalAudio ? '0' : (finalAudio.size / 1024 / 1024).toFixed(2));
  const formattedDuration = $derived(duration ? formatDigitalDuration({seconds: duration}) : 'unknown');
  let ffmpegApi: FFmpegApi | undefined;

  let ffmpegFile = resource(
    () => audio,
    async (audio, _, {signal}) => {
      ffmpegApi ??= await FFmpegApi.create();
      return await ffmpegApi.toFFmpegFile(audio, AbortSignal.any([signal, abortController.signal]));
    },
  );

  let flacFile = resource(
    () => [ffmpegFile.current],
    async ([file], _, {signal}) => {
      if (!file) return;
      ffmpegApi ??= await FFmpegApi.create();
      return await ffmpegApi.convertToFlac(file, AbortSignal.any([signal, abortController.signal]));
    },
  );

  let readFile = resource(
    () => [flacFile.current],
    async ([file], _, {signal}) => {
      if (!file) return;
      ffmpegApi ??= await FFmpegApi.create();
      return await ffmpegApi.readFile(file, AbortSignal.any([signal, abortController.signal]));
    },
  );

  watch(
    () => [readFile.current, readFile.loading] as const,
    ([file, loading]) => {
      if (loading || !file) {
        finalAudio = undefined;
      } else {
        finalAudio = file;
      }
    },
  );

  const loading = $derived(ffmpegFile.loading || flacFile.loading || readFile.loading);
  const error = $derived((ffmpegFile.error || flacFile.error || readFile.error)?.toString());

  const abortController = new AbortController();
  onDestroy(() => {
    abortController.abort();
    ffmpegApi?.terminate();
  });

  function downloadAudio() {
    if (!finalAudio) throw new Error('No audio to download');
    const url = URL.createObjectURL(finalAudio);
    try {
      //todo only works on desktop, not mobile
      const a = document.createElement('a');
      a.href = url;
      a.download = `${finalAudio.name}`;
      a.click();
    } finally {
      URL.revokeObjectURL(url);
    }
  }
</script>

<div class="flex flex-col gap-4 items-center justify-center">
  {#if loading || !finalAudio}
    <Loading class="self-center justify-self-center size-16" />
  {:else}
    <span class="inline-grid grid-cols-[auto_auto_1rem_auto_auto] gap-2 items-baseline">
      <Label class="justify-self-end">{$t`Length:`}</Label> <span>{$t`${formattedDuration}`}</span>
      <span></span>
      <Label class="justify-self-end">{$t`Size:`}</Label> <span>{$t`${mb} MB`}</span>
      {#if finalAudio.name}
        <Label class="justify-self-end">{$t`File name:`}</Label>
        <span class="col-span-4">{$t`${finalAudio.name}`}</span>
      {/if}
      <DevContent>
        <Label class="justify-self-end">{$t`Type:`}</Label>
        <span class="col-span-4">{$t`${finalAudio.type}`}</span>
      </DevContent>
    </span>
    <!-- contain-inline-size prevents wavesurfer from freaking out inside a grid -->
    <!-- pb ensures the waveform timeline is in the bounds of this container -->
    <div class="w-full h-32 pb-3 contain-inline-size border-y">
      <Waveform audio={finalAudio} bind:playing bind:audioApi bind:duration showTimeline autoplay class="size-full" />
    </div>
    <div class="flex gap-2">
      <Button onclick={() => downloadAudio()} disabled={!finalAudio} variant="secondary" icon="i-mdi-download"
        >{$t`Download`}</Button
      >
      <Button variant="secondary" icon="i-mdi-close" onclick={onDiscard} disabled={!audio}>{$t`Discard`}</Button>
      <Button
        icon={playing ? 'i-mdi-pause' : 'i-mdi-play'}
        onclick={() => (playing ? audioApi?.pause() : audioApi?.play())}
        disabled={!audioApi}
        size="icon"
      />
    </div>
  {/if}
  {#if error}
    <p class="text-destructive">{error}</p>
  {/if}
</div>
