<script lang="ts">
  import {useWaveSurfer, type WaveSurferAudio} from './wavesurfer-utils';
  import type WaveSurfer from 'wavesurfer.js';
  import {watch} from 'runed';
  import type {HTMLAttributes} from 'svelte/elements';

  type AudioUrl = string;

  type Props = HTMLAttributes<HTMLDivElement> & {
    audio?: WaveSurferAudio;
    audioApi?: WaveSurfer;
    playing?: boolean;
    showTimeline?: boolean;
    autoplay?: boolean;
    duration?: number | null;
  };

  let {
    audio,
    audioApi: wavesurfer = $bindable(),
    playing = $bindable(false),
    duration = $bindable(null),
    showTimeline,
    autoplay,
    ...rest
  }: Props = $props();

  let container = $state<HTMLElement>();

  watch(() => container, (newContainer) => {

    wavesurfer?.destroy();
    wavesurfer = undefined;

    if (newContainer) {
      wavesurfer = useWaveSurfer({container: newContainer, autoplay, showTimeline});
      // See error handling in audio-input.svelte for information regarding known errors
      wavesurfer.on('play', () => {
        const audioElem = wavesurfer?.getMediaElement();
        if (audioElem?.error) {
          // playing presumably won't work, so reload and then try
          audioElem.load();
          audioElem.play().then(() => playing = true).catch(err => {
            console.error('Error playing audio element after reload:', err);
            playing = false;
          });
        } else {
          playing = true;
        }
    });
      wavesurfer.on('pause', () => playing = false);
      wavesurfer.on('finish', () => playing = false);
      wavesurfer.on('error', (error) => {
        console.error('WaveSurfer error:', error);
        const audioElem = wavesurfer?.getMediaElement();
        if (audioElem && playing) {
          audioElem.load();
          audioElem.play().catch(err => {
            console.error('Error playing audio element after WaveSurfer error:', err);
            playing = false;
          });
        }
      });
    }
  });

  $effect(() => {
    if (audio && wavesurfer)
      void loadAudio(audio);
  });

  async function loadAudio(audio: AudioUrl | Blob) {
    if (!wavesurfer) throw new Error('WaveSurfer not initialized');

    if (typeof audio === 'string') {
      await wavesurfer.load(audio);
    } else if (audio instanceof Blob) {
      await wavesurfer.loadBlob(audio);
    } else {
      throw new Error('Invalid audio type');
    }

    duration = wavesurfer.getDuration();
  }
</script>

<div bind:this={container} {...rest}></div>
