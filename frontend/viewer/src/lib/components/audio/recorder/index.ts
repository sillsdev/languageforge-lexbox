import Root from './recorder.svelte';
import Trigger from './recorder-trigger.svelte';

/** False where WebKit compiles out media capture (e.g. Mac Catalyst's WKWebView), so there's nothing to record with. */
export const recordingSupported =
  typeof navigator !== 'undefined' && !!navigator.mediaDevices?.getUserMedia && typeof MediaRecorder !== 'undefined';

export {
  Root,
  Trigger,
  //
  Root as Recorder,
  Trigger as RecorderTrigger,
};
