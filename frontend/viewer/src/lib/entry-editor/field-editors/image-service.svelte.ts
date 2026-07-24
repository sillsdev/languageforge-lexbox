import {Context} from 'runed';
import {onDestroy} from 'svelte';
import {SvelteMap} from 'svelte/reactivity';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';
import {ReadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ReadFileResult';

export type ImageState =
  | {status: 'loaded'; url: string}
  | {status: 'not-downloaded'}
  | {status: 'error'; reason: 'not-found' | 'offline' | 'unknown'};

export type LoadImageOptions = {
  downloadIfMissing?: boolean;
  bypassCache?: boolean;
};

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
    const blob = await new Response(await file.stream.stream()).blob();
    if (this.#disposed) return {status: 'error', reason: 'unknown'};
    const state = {status: 'loaded', url: URL.createObjectURL(blob)} as const;
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

export function initImageService(getApi: () => IMiniLcmJsInvokable): ImageService {
  const service = new ImageService(getApi);
  imageServiceContext.set(service);
  onDestroy(() => service.dispose());
  return service;
}

export function useImageService(): ImageService | undefined {
  return imageServiceContext.getOr(undefined);
}
