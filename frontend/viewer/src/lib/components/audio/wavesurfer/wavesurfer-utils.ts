import WaveSurfer, {type WaveSurferOptions} from 'wavesurfer.js';
import {onDestroy} from 'svelte';
import {theme} from 'mode-watcher';
import {toStore} from 'svelte/store';
import TimelinePlugin from 'wavesurfer.js/dist/plugins/timeline.esm.js';

type AudioUrl = string;
export type WaveSurferAudio = AudioUrl | Blob;

function getPrimaryColor(element: HTMLElement | null = null): string {
  return `hsl(${getComputedStyle(element ?? document.documentElement)
    .getPropertyValue('--primary')
    .trim()})`;
}

function darkenColor(color: string): string {
  return `oklch(from ${color} calc(l * .50) c h)`;
}

function setThemeColors(wavesurfer: WaveSurfer) {
  const primaryColor = getPrimaryColor(
    typeof wavesurfer.options.container === 'string' ? null : wavesurfer.options.container,
  );
  const darkPrimaryColor = darkenColor(primaryColor);

  wavesurfer.setOptions({
    waveColor: primaryColor,
    progressColor: darkPrimaryColor,
    cursorColor: darkPrimaryColor,
  });
}

function addTimelinePlugin(wavesurfer: WaveSurfer) {
  return wavesurfer.registerPlugin(
    TimelinePlugin.create({
      insertPosition: 'afterend',
      height: 12,
      timeInterval: 0.25,
      primaryLabelInterval: 1,
      secondaryLabelInterval: 0.5,
      style: {
        fontSize: '10px',
      },
    }),
  );
}

type WaveSurferConfig = WaveSurferOptions & {
  showTimeline?: boolean;
};

export function useWaveSurfer(config: WaveSurferConfig): WaveSurfer {
  const {showTimeline, ...waveSurferOptions} = config;
  const wavesurfer = WaveSurfer.create({
    height: 'auto',
    barWidth: 4,
    barGap: 2,
    barRadius: 5,
    minPxPerSec: 50,
    cursorWidth: 2,
    normalize: true,
    // sample audio
    // url: 'https://cdn.freesound.org/previews/815/815388_16624953-lq.mp3', // 2.8s
    // url: 'https://dl.espressif.com/dl/audio/gs-16b-2c-44100hz.aac', // 16s
    // url: 'https://dl.espressif.com/dl/audio/ff-16b-2c-44100hz.aac', // 3m 7s
    ...waveSurferOptions,
  });

  setThemeColors(wavesurfer);

  if (showTimeline) {
    addTimelinePlugin(wavesurfer);
  }

  const unsub = toStore(() => theme.current).subscribe(() => {
    setThemeColors(wavesurfer);
  });

  onDestroy(() => {
    unsub();
    wavesurfer.destroy();
  });
  wavesurfer.on('destroy', unsub);

  return wavesurfer;
}
