<script lang="ts">
  import { t } from 'svelte-i18n-lingui';
  import Button from '../ui/button/button.svelte';
  import Waveform from './wavesurfer/waveform.svelte';
  import type WaveSurfer from 'wavesurfer.js';
  import {formatDigitalDuration} from '../ui/format/format-duration';
  import DevContent from '$lib/layout/DevContent.svelte';
  import {Label} from '../ui/label';

  type Props = {
    audio: Blob;
    onDiscard: () => void;
  };

  let { audio, onDiscard }: Props = $props();

  let audioApi = $state<WaveSurfer>();
  let playing = $state(false);
  const name = $derived(audio instanceof File ? audio.name : undefined);
  let duration = $state<number | null>(null);
  const mb = $derived((audio.size / 1024 / 1024).toFixed(2));
  const formatedDuration = $derived(duration ? formatDigitalDuration({ seconds: duration }) : '');

</script>

<div class="flex flex-col gap-4 items-center justify-center">
  <span class="inline-grid grid-cols-[auto_auto_1rem_auto_auto] gap-2 items-baseline">
    <Label class="justify-self-end">{$t`Length:`}</Label> {$t`${formatedDuration}`}
    <span></span>
    <Label class="justify-self-end">{$t`Size:`}</Label> {$t`${mb} MB`}
    {#if name}
      <Label class="justify-self-end">{$t`File name:`}</Label>
      <span class="col-span-4">{$t`${name}`}</span>
    {/if}
    <DevContent>
      <Label class="justify-self-end">{$t`Type:`}</Label>
      <span class="col-span-4">{$t`${audio.type}`}</span>
    </DevContent>
  </span>
  <!-- contain-inline-size prevents wavesurfer from freaking out inside a grid -->
  <!-- pb-8 ensures the timeline is in the bounds of the container -->
  <div class="w-full grow max-h-32 pb-3 contain-inline-size border-y">
    <Waveform {audio} bind:playing bind:audioApi bind:duration showTimeline autoplay class="size-full" />
  </div>
  <div class="flex gap-2">
    <Button variant="secondary" icon="i-mdi-close" onclick={onDiscard} disabled={!audio}>{$t`Discard`}</Button>
    <Button
      icon={playing ? 'i-mdi-pause' : 'i-mdi-play'}
      onclick={() => (playing ? audioApi?.pause() : audioApi?.play())}
      disabled={!audioApi}
      size="icon"
    />
  </div>
</div>
