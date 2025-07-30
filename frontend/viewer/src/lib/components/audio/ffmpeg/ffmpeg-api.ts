import {FFmpeg} from '@ffmpeg/ffmpeg';
import coreURL from '@ffmpeg/core?url';
import {fetchFile} from '@ffmpeg/util';
import inlineDataUrlWorker from './bundled-ffmpeg-worker.js?url&inline';
import wasmUrl from '@ffmpeg/core/wasm?url';
import {randomId} from '$lib/utils';

let loadingPromise: Promise<FFmpeg> | null = null;

export async function loadFFmpeg(): Promise<FFmpeg> {
  if (loadingPromise) {
    return await loadingPromise;
  }

  try {
    loadingPromise = loadFFmpegInternal();
    return await loadingPromise;
  } catch (error) {
    loadingPromise = null;
    throw error;
  }
}

async function loadFFmpegInternal(): Promise<FFmpeg> {
  const ffmpeg = new FFmpeg();
  ffmpeg.on('log', (msg) => {
    console.log('FFmpeg log:', msg);
  });

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
  public static async create(): Promise<FFmpegApi> {
    console.log('Loading FFmpeg...');
    const ffmpeg = await loadFFmpeg();
    console.log('FFmpeg loaded:', ffmpeg);
    return new FFmpegApi(ffmpeg);
  }
  private constructor(private ffmpeg: FFmpeg) {
  }

  private async createDir(path: string) {
    console.log('Creating dir:', path);
    const paths = path.split('/').filter(v => !!v);
    let newDir = '';
    for (const dir of paths) {
      console.log('Checking dir:', newDir, dir);
      const dirExists = (await this.ffmpeg.listDir(newDir || '/')).some(f => f.name === dir);
      newDir += `/${dir}`;
      if (dirExists) {
        continue;
      }
      console.log('Creating dir:', newDir);
      //create dir never returns if the dir already exists or if you're trying to create a sub directory of a directory which doesn't exist
      await this.ffmpeg.createDir(newDir);
    }
  }
  async toFFmpegFile(file: File): Promise<FFmpegFile> {
    console.log('Writing file to ffmpeg FS:', file.name);
    const ffmpegFile = new FFmpegFile(file.type, file.name, 'input');
    await this.createDir(ffmpegFile.internalFileDir);
    await this.ffmpeg.writeFile(ffmpegFile.internalFilePath, await fetchFile(file));
    return ffmpegFile;
  }

  async readFile(file: FFmpegFile): Promise<File> {
    console.log('Reading file from ffmpeg FS:', file.filename);
    const data = await this.ffmpeg.readFile(file.internalFilePath) as Uint8Array;
    return new File([data], file.filename, {type: file.mimeType});
  }

  async convertToWav(file: FFmpegFile): Promise<FFmpegFile> {
    const convertedFile = file.changeExtension('wav', 'audio/wav', 'convert');
    await this.createDir(convertedFile.internalFileDir);
    await this.ffmpeg.exec(
      [
        '-i', file.internalFilePath,
        '-af', 'loudnorm',
        '-ar', '44100',
        '-ac', '2',
        '-codec:a', 'pcm_s16le',
        convertedFile.internalFilePath
      ]
    );
    return convertedFile;
  }

  async convertToFlac(file: FFmpegFile): Promise<FFmpegFile> {
    console.log('Converting to FLAC:', file.filename);
    const convertedFile = file.changeExtension('flac', 'audio/flac', 'convert');
    await this.createDir(convertedFile.internalFileDir);
    await this.ffmpeg.exec(
      [
        '-i', file.internalFilePath,
        '-af', 'loudnorm',
        '-ar', '44100',
        '-codec:a', 'flac',
        convertedFile.internalFilePath
      ]
    );
    return convertedFile;
  }
}
