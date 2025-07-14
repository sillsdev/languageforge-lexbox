import {FFmpeg} from '@ffmpeg/ffmpeg';
import coreURL from '@ffmpeg/core?url';
import {fetchFile} from '@ffmpeg/util';
import inlineDataUrlWorker from './bundled-ffmpeg-worker.js?url&inline';
import wasmUrl from '@ffmpeg/core/wasm?url';

let loadingPromise: Promise<FFmpeg> | null = null;

export async function loadFFmpeg(): Promise<FFmpeg> {
  if (loadingPromise) {
    return loadingPromise;
  }

  try {
    loadingPromise = loadFFmpegInternal();
  } catch (error) {
    loadingPromise = null;
    throw error;
  }

  return loadingPromise;
}

async function loadFFmpegInternal(): Promise<FFmpeg> {
  const ffmpeg = new FFmpeg();

  await ffmpeg.load({
    coreURL: coreURL,
    wasmURL: wasmUrl,
    classWorkerURL: import.meta.env.DEV ? inlineDataUrlWorker : undefined,
  });

  return ffmpeg;
}

export async function convertToWav(ffmpeg: FFmpeg, blob: Blob): Promise<Blob> {
  // Load input into ffmpeg FS
  const inputName = 'input.mp3';
  const outputName = 'output.wav';

  await ffmpeg.writeFile(inputName, await fetchFile(blob));

  // Normalize volume and convert to WAV
  await ffmpeg.exec([
    '-i', inputName,
    '-af', 'loudnorm',
    '-ar', '44100',
    '-ac', '2',
    '-c:a', 'pcm_s16le',
    outputName
  ]);

  // Read output file
  const data = await ffmpeg.readFile(outputName) as Uint8Array;;
  return new Blob([data], {type: 'audio/wav'});
}

