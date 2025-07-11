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

  watch([() => container, () => audio], ([newContainer, newAudio], [oldContainer, oldAudio]) => {
    let createdWavesurfer = false;
    if (newContainer !== oldContainer) {
      wavesurfer?.destroy();
      wavesurfer = undefined;

      if (newContainer) {
        wavesurfer = useWaveSurfer({container: newContainer, autoplay, showTimeline});
        wavesurfer.on('play', () => playing = true);
        wavesurfer.on('pause', () => playing = false);
        wavesurfer.on('finish', () => playing = false);
        createdWavesurfer = true;
      }
    }

    if (!wavesurfer) return;

    if (newAudio && (newAudio !== oldAudio || createdWavesurfer)) {
      void loadAudio(newAudio);
    }
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
