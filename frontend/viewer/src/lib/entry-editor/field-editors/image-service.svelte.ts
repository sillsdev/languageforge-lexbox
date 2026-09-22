import {Context} from 'runed';
import {onDestroy} from 'svelte';
import {SvelteMap} from 'svelte/reactivity';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';
import {ReadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ReadFileResult';
import {guessMimeType} from '$lib/media-manager/media-file-utils';
import {useProjectContext} from '$project/project-context.svelte';

export type ImageState =
  | {status: 'loaded'; url: string}
  | {status: 'not-downloaded'}
  | {status: 'error'; reason: 'not-found' | 'offline' | 'unknown'};

export type LoadImageOptions = {
  downloadIfMissing?: boolean;
  bypassCache?: boolean;
};

const LOCAL_PREVIEW_PREFIX = 'local-preview:';

/**
 * Loads picture images and hands back a shared blob object URL per mediaUri. Cached until the
 * entry view is torn down.
 */
export class ImageService {
  readonly #getApi: () => IMiniLcmJsInvokable;
  readonly #cache = new SvelteMap<string, Extract<ImageState, {status: 'loaded'}>>();
  readonly #inFlight = new SvelteMap<string, Promise<ImageState>>();
  #disposed = false;

  constructor(getApi: () => IMiniLcmJsInvokable) {
    this.#getApi = getApi;
  }

  loadImage(mediaUri: string, options: LoadImageOptions = {}): Promise<ImageState> {
    const {downloadIfMissing = false, bypassCache = false} = options;
    if (!bypassCache) {
      const cached = this.#cache.get(mediaUri);
      if (cached) return Promise.resolve(cached);
      const inFlight = this.#inFlight.get(mediaUri);
      if (inFlight) return inFlight;
    }
    const promise = this.#load(mediaUri, downloadIfMissing).finally(() => {
      if (this.#inFlight.get(mediaUri) === promise) this.#inFlight.delete(mediaUri);
    });
    if (!bypassCache) this.#inFlight.set(mediaUri, promise);
    return promise;
  }

  /** Read inside $derived in order to react to other components loading a mediaUri */
  cached(mediaUri: string): Extract<ImageState, {status: 'loaded'}> | undefined {
    return this.#cache.get(mediaUri);
  }

  /** Registers an in-memory file for preview under a synthetic mediaUri and returns that uri. Used
      for pictures that have not been uploaded yet. Release it with releaseLocalPreview once the draft
      is superseded/discarded; any that remain are revoked at dispose(). */
  registerLocalPreview(file: File): string {
    const uri = `${LOCAL_PREVIEW_PREFIX}${crypto.randomUUID()}`;
    this.#cache.set(uri, {status: 'loaded', url: URL.createObjectURL(file)});
    return uri;
  }

  /** Revokes and forgets a preview registered via registerLocalPreview. No-op for a real (uploaded)
      mediaUri, so callers can pass a draft's uri without risking a shared, still-in-use image. */
  releaseLocalPreview(uri: string): void {
    if (!uri.startsWith(LOCAL_PREVIEW_PREFIX)) return;
    const state = this.#cache.get(uri);
    if (!state) return;
    URL.revokeObjectURL(state.url);
    this.#cache.delete(uri);
  }

  async #load(mediaUri: string, downloadIfMissing: boolean): Promise<ImageState> {
    const api = this.#getApi();
    const file = await api.getFileStream(mediaUri, downloadIfMissing);
    if (!file.stream) {
      if (!downloadIfMissing && file.result === ReadFileResult.NotFound) {
        return {status: 'not-downloaded'};
      }
      const reason =
        file.result === ReadFileResult.NotFound ? 'not-found'
        : file.result === ReadFileResult.Offline ? 'offline'
        : 'unknown';
      return {status: 'error', reason};
    }
    // Response(stream).blob() drops Content-Type; SVG (and some others) need the right MIME
    // type to render in <img>, so re-apply from the filename when we have one.
    const headers = file.fileName ? {'Content-Type': guessMimeType(file.fileName)} : undefined;
    const blob = await new Response(await file.stream.stream(), {headers}).blob();
    if (this.#disposed) return {status: 'error', reason: 'unknown'};
    const state = {status: 'loaded', url: URL.createObjectURL(blob)} as const;
    const previous = this.#cache.get(mediaUri);
    if (previous) URL.revokeObjectURL(previous.url);
    this.#cache.set(mediaUri, state);
    return state;
  }

  dispose(): void {
    this.#disposed = true;
    for (const state of this.#cache.values()) URL.revokeObjectURL(state.url);
    this.#cache.clear();
  }
}

const imageServiceContext = new Context<ImageService>('image-service');

export function initImageService(getApi?: () => IMiniLcmJsInvokable): ImageService {
  if (!getApi) {
    const projectContext = useProjectContext();
    getApi = () => projectContext.api;
  }
  const service = new ImageService(getApi);
  imageServiceContext.set(service);
  onDestroy(() => service.dispose());
  return service;
}

export function useImageService(): ImageService {
  let imageService = imageServiceContext.getOr(undefined);
  if (!imageService) {
    const projectContext = useProjectContext();
    imageService = new ImageService(() => projectContext.api);
    onDestroy(() => imageService?.dispose());
  }
  return imageService;
}
