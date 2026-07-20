import {Context} from 'runed';
import {onDestroy} from 'svelte';
import {SvelteMap} from 'svelte/reactivity';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';
import {ReadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ReadFileResult';

export type ImageLoadState =
  | {status: 'loading'}
  | {status: 'loaded'; url: string}
  | {status: 'error'; reason: 'not-found' | 'offline' | 'unknown'; detail?: string};

/**
 * Loads picture images once per mediaUri and hands out a shared blob object URL. Scoped (via context)
 * to the entry view, so identical mediaUris across a sense's pictures — or the same picture shown in
 * both the field and a dialog — are fetched and decoded a single time. The cached object URLs live
 * until the owning entry view is torn down (see initImageService), then dispose() revokes them.
 */
export class ImageService {
  readonly #getApi: () => IMiniLcmJsInvokable | undefined;
  readonly #cache = new SvelteMap<string, ImageLoadState>();
  #disposed = false;

  constructor(getApi: () => IMiniLcmJsInvokable | undefined) {
    this.#getApi = getApi;
  }

  /** Starts loading `mediaUri` if it hasn't been requested yet. Call from an $effect (it mutates). */
  preload(mediaUri: string): void {
    if (this.#cache.has(mediaUri)) return;
    this.#cache.set(mediaUri, {status: 'loading'});
    void this.#load(mediaUri);
  }

  /** Reactive load-state for `mediaUri`. Call from a $derived; 'loading' until preload has resolved. */
  get(mediaUri: string): ImageLoadState {
    return this.#cache.get(mediaUri) ?? {status: 'loading'};
  }

  // getFileStream reports failure via the response (not exceptions), so we branch on it; a thrown
  // error (e.g. from blob()) propagates to the global handler, matching the rest of the viewer.
  async #load(mediaUri: string): Promise<void> {
    const api = this.#getApi();
    if (!api) {
      this.#cache.set(mediaUri, {status: 'error', reason: 'unknown'});
      return;
    }
    const file = await api.getFileStream(mediaUri);
    if (this.#disposed) return;
    if (!file.stream) {
      const reason =
        file.result === ReadFileResult.NotFound ? 'not-found'
        : file.result === ReadFileResult.Offline ? 'offline'
        : 'unknown';
      this.#cache.set(mediaUri, {status: 'error', reason, detail: file.errorMessage ?? undefined});
      return;
    }
    const blob = await new Response(await file.stream.stream()).blob();
    // Bail before minting a URL if the service was torn down mid-load, else it would never be revoked.
    if (this.#disposed) return;
    this.#cache.set(mediaUri, {status: 'loaded', url: URL.createObjectURL(blob)});
  }

  dispose(): void {
    this.#disposed = true;
    for (const state of this.#cache.values()) {
      if (state.status === 'loaded') URL.revokeObjectURL(state.url);
    }
    this.#cache.clear();
  }
}

const imageServiceContext = new Context<ImageService>('image-service');

/**
 * Creates an entry-view-scoped image cache and publishes it to descendants (pictures, the edit
 * dialog, the fullscreen viewer). Call once from the entry view; its object URLs are revoked when
 * that view is destroyed.
 */
export function initImageService(getApi: () => IMiniLcmJsInvokable | undefined): ImageService {
  const service = new ImageService(getApi);
  imageServiceContext.set(service);
  onDestroy(() => service.dispose());
  return service;
}

/** The entry-view image service, or undefined when rendered outside an entry view. */
export function useImageService(): ImageService | undefined {
  return imageServiceContext.getOr(undefined);
}
