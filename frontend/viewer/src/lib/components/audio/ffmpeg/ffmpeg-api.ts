import {FFmpeg} from '@ffmpeg/ffmpeg';
import coreURL from '@ffmpeg/core?url';
import {fetchFile} from '@ffmpeg/util';
import inlineDataUrlWorker from './bundled-ffmpeg-worker.js?url&inline';
import {randomId} from '$lib/utils';
import wasmUrl from '@ffmpeg/core/wasm?url';

async function ensureLoaded(ffmpeg: FFmpeg) {
  if (ffmpeg.loaded) return;
  await ffmpeg.load({
    coreURL: coreURL,
    wasmURL: wasmUrl,
    classWorkerURL: import.meta.env.DEV ? inlineDataUrlWorker : undefined,
  });
}

async function loadFFmpegInternal(): Promise<FFmpeg> {
  const ffmpeg = new FFmpeg();
  ffmpeg.on('log', (msg) => {
    console.log('FFmpeg log:', msg);
  });
  await ensureLoaded(ffmpeg);

  return ffmpeg;
}

export class FFmpegFile {
  readonly filename: string;
  private id: string = randomId();
  private prefix: string;

  constructor(mimeType: string, filename: string, prefix: string) {
    this.mimeType = mimeType;
    this.filename = filename;
    this.prefix = prefix;
  }

  get internalFilePath() {
    return `${this.internalFileDir}/${this.filename}`;
  }
  get internalFileDir() {
    return `/${this.prefix}/${this.id}`;
  }
  readonly mimeType: string;

  changeExtension(newExtension: string, mimeType?: string, prefix?: string): FFmpegFile {
    return new FFmpegFile(mimeType ?? this.mimeType, this.filename.replace(/\.[^.]+$/, `.${newExtension}`), prefix ?? this.prefix);
  }
}

export class FFmpegApi {
  private static loadingPromise: Promise<FFmpeg> | null = null;

  public static async create(): Promise<FFmpegApi> {
    console.log('Loading FFmpeg...');
    FFmpegApi.loadingPromise ??= loadFFmpegInternal();
    const ffmpeg = await FFmpegApi.loadingPromise;
    await ensureLoaded(ffmpeg);
    console.log('FFmpeg loaded:', ffmpeg);
    return new FFmpegApi(ffmpeg);
  }

  private constructor(private ffmpeg: FFmpeg) {
  }

  public terminate() {
    this.ffmpeg.terminate();
  }

  private async createDir(path: string, signal: AbortSignal) {
    console.log('Creating dir:', path);
    const paths = path.split('/').filter(v => !!v);
    let newDir = '';
    for (const dir of paths) {
      console.log('Checking dir:', newDir, dir);
      const dirExists = (await this.ffmpeg.listDir(newDir || '/', {signal})).some(f => f.name === dir);
      newDir += `/${dir}`;
      if (dirExists) {
        continue;
      }
      console.log('Creating dir:', newDir);
      //create dir never returns if the dir already exists or if you're trying to create a sub directory of a directory which doesn't exist
      await this.ffmpeg.createDir(newDir, {signal});
    }
  }
  async toFFmpegFile(file: File, signal: AbortSignal): Promise<FFmpegFile> {
    console.log('Writing file to ffmpeg FS:', file.name);
    const ffmpegFile = new FFmpegFile(file.type, file.name, 'input');
    await this.createDir(ffmpegFile.internalFileDir, signal);
    await this.ffmpeg.writeFile(ffmpegFile.internalFilePath, await fetchFile(file), {signal});
    return ffmpegFile;
  }

  async readFile(file: FFmpegFile, signal: AbortSignal): Promise<File> {
    console.log('Reading file from ffmpeg FS:', file.filename);
    const data = await this.ffmpeg.readFile(file.internalFilePath, undefined, {signal}) as Uint8Array;
    const buffer = data instanceof Uint8Array ? data.slice().buffer : new Uint8Array(data).buffer;
    return new File([buffer], file.filename, {type: file.mimeType});
  }

  async convertToWav(file: FFmpegFile, signal: AbortSignal): Promise<FFmpegFile> {
    const convertedFile = file.changeExtension('wav', 'audio/wav', 'convert');
    await this.createDir(convertedFile.internalFileDir, signal);
    await this.ffmpeg.exec(
      [
        '-i', file.internalFilePath,
        '-af', 'loudnorm',
        '-ar', '44100',
        '-ac', '2',
        '-codec:a', 'pcm_s16le',
        convertedFile.internalFilePath
      ],
      undefined,
      {signal}
    );
    return convertedFile;
  }

  async convertToFlac(file: FFmpegFile,  signal: AbortSignal): Promise<FFmpegFile> {
    console.log('Converting to FLAC:', file.filename);
    const convertedFile = file.changeExtension('flac', 'audio/flac', 'convert');
    await this.createDir(convertedFile.internalFileDir, signal);
    await this.ffmpeg.exec(
      [
        '-i', file.internalFilePath,
        '-af', 'loudnorm',
        '-ar', '44100',
        '-codec:a', 'flac',
        convertedFile.internalFilePath
      ],
      undefined,
      {signal}
    );
    return convertedFile;
  }
}
