import type {FFmpeg} from '@ffmpeg/ffmpeg';

let ffmpegInstance: FFmpeg | null = null;
let loadingPromise: Promise<FFmpeg> | null = null;

/**
 * Lazily loads and initializes FFmpeg WebAssembly.
 * Returns a promise that resolves to the initialized FFmpeg instance.
 * Subsequent calls will return the same instance.
 */
export async function loadFFmpeg(): Promise<FFmpeg> {
  // If already loaded, return the instance
  if (ffmpegInstance) {
    return ffmpegInstance;
  }

  // If currently loading, return the existing promise
  if (loadingPromise) {
    return loadingPromise;
  }

  // Start loading FFmpeg
  loadingPromise = (async () => {
    try {
      // eslint-disable-next-line @typescript-eslint/naming-convention
      const { FFmpeg } = await import('@ffmpeg/ffmpeg');

      // one way to get URLs
      // const { default: ffmpegURL } = await import('@ffmpeg/ffmpeg?url');
      const {default: coreURL} = await import('@ffmpeg/core?url');
      const {default: wasmURL} = await import('@ffmpeg/core/wasm?url');
      // const {default: workerURL} = await import('@ffmpeg/ffmpeg/worker?url'); // not available in 0.12.10

      // another way to get URLs
      // const coreURL = new URL('@ffmpeg/core', import.meta.url).href;
      // const wasmURL = new URL('@ffmpeg/core/wasm', import.meta.url).href;
      // const workerURL = new URL('@ffmpeg/ffmpeg/worker', import.meta.url).href;

      // const workerURL = ffmpegURL.replace('index.js', 'worker.js');

      const ffmpeg = new FFmpeg();


      // Or just use a CDN URL
      // const baseURL = 'https://cdn.jsdelivr.net/npm/@ffmpeg/core-mt@0.12.10/dist/esm'

      // this never returns...
      await ffmpeg.load({
        // coreURL: await toBlobURL(`${baseURL}/ffmpeg-core.js`, 'text/javascript'),
        // wasmURL: await toBlobURL(`${baseURL}/ffmpeg-core.wasm`, 'application/wasm'),
        // classWorkerURL: await toBlobURL(`${baseURL}/ffmpeg-core.worker.js`, 'text/javascript'),

        // toBlobURL avoids CORS issues. It's probably only required for the classWorkerURL in dev, because workers seem to be more strict about CORS.
        coreURL: coreURL,
        wasmURL: wasmURL,
        // Can't use blob URLs for module worker scripts
        // classWorkerURL: await toBlobURL(workerURL, 'text/javascript'),
        // Seems to be optional now that 0.10.15 exports /worker
        // classWorkerURL: workerURL,
      });
      console.log('FFmpeg loaded successfully');

      ffmpegInstance = ffmpeg;
      return ffmpeg;
    } catch (error) {
      // Reset loading state on error
      loadingPromise = null;
      throw new Error(`Failed to load FFmpeg: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  })();

  return loadingPromise;
}
