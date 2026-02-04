<script lang="ts" module>
  import {Context} from 'runed';

  type RecorderRootStateProps = {
    onStartRecording: () => Promise<void>;
    onStopRecording: () => void;
    recording: boolean;
  };

  const recorderRootContext = new Context<RecorderRootStateProps>('Recorder.Root');

  export function useRecorderRoot(props: RecorderRootStateProps): RecorderRootStateProps {
    return recorderRootContext.set(props);
  }

  type RecorderTriggerStateProps = RecorderRootStateProps;

  export function useRecorderTrigger(): RecorderTriggerStateProps {
    return recorderRootContext.get();
  }
</script>

<script lang="ts">
  import {onDestroy, type Snippet} from 'svelte';
  import type WaveSurfer from 'wavesurfer.js';
  import RecordPlugin from 'wavesurfer.js/dist/plugins/record.esm.js';
  import {watch} from 'runed';
  import {useWaveSurfer} from '../wavesurfer/wavesurfer-utils';

  type Props = {
    container: string | HTMLElement | undefined;
    children?: Snippet;
    recording?: boolean;
    duration?: number;
    onRecordingStart?: () => void;
    onRecordingStop?: () => void;
    onRecordingComplete?: (blob: Blob) => void;
  };

  let {
    recording = $bindable(false),
    duration = $bindable(),
    container,
    onRecordingStart,
    onRecordingStop,
    onRecordingComplete,
    children,
  }: Props = $props();

  useRecorderRoot({
    onStartRecording: async () => {
      if (!recorder) throw new Error('Plugin not initialized');
      if (recorder.isRecording()) throw new Error('Plugin already recording');
      if (recording) throw new Error("Component thinks it's already recording");

      // The alternative to prepareMic() could be to activate the mic
      // and then give it time to "settle" (e.g. to avoid initial crackle)
      // await navigator.mediaDevices.getUserMedia({ audio: true });
      // await delay(300);

      // Not using any browser recording features (e.g. echoCancellation, noiseSuppression, autoGainControl)
      // because ffmpeg can do that better
      await recorder.startRecording();
      recording = true;
      onRecordingStart?.();

      // the recorder has taken over, so we don't need our stream anymore
      cleanUpStream();
    },
    onStopRecording: () => {
      if (!recorder || !recorder.isRecording()) return;
      if (!recording) throw new Error("Component thinks it's not recording");

      recorder.stopRecording();
      recording = false;
      onRecordingStop?.();
    },
    get recording() {
      return recording;
    },
  });

  let wavesurfer: WaveSurfer | undefined;
  let recorder: RecordPlugin | undefined;
  let micActivatorStreamPromise: Promise<MediaStream> | undefined;

  watch(
    () => container,
    (newContainer) => {
      reset();
      if (newContainer) {
        initRecorder(newContainer);
      }
    },
  );

  onDestroy(() => {
    reset();
  });

  function reset() {
    duration = 0;
    recorder?.destroy();
    recorder = undefined;
    wavesurfer?.destroy();
    wavesurfer = undefined;
    cleanUpStream();
  }

  function onRecordingEnd(blob: Blob) {
    onRecordingComplete?.(blob);
  }

  async function prepareMic() {
    cleanUpStream();
    // Request access/activate mic early so it's ready on demand and there's no initial crackle
    // Note: I don't think any browser enforces getUserMedia() to be called in the context of a user gesture
    // except when the user has previously denied access, in which case this could be sub-optimal

    // assign the promise before we wait for it so onDestroy can always see it
    micActivatorStreamPromise = navigator.mediaDevices.getUserMedia({audio: true});
    await micActivatorStreamPromise;
  }

  function cleanUpStream() {
    void micActivatorStreamPromise?.then((stream) => {
      stream.getTracks().forEach((track) => track.stop());
    });
    micActivatorStreamPromise = undefined;
  }

  function initRecorder(container: string | HTMLElement) {
    void prepareMic();

    wavesurfer = useWaveSurfer({
      container,
    });

    if (wavesurfer.options.barWidth) {
      wavesurfer.setOptions({
        // prevents a weird gap on the right side of the waveform
        width: `calc(100% + ${wavesurfer.options.barWidth}px)`,
      });
    }

    recorder = wavesurfer.registerPlugin(
      RecordPlugin.create({
        renderRecordedAudio: false,
        scrollingWaveform: true,
        scrollingWaveformWindow: 3,
      }),
    );

    recorder.on('record-end', (blob) => onRecordingEnd(blob));
    recorder.on('record-progress', (time) => (duration = time));
  }
</script>

{@render children?.()}
