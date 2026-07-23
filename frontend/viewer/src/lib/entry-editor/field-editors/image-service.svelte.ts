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
  /** Fetch from the remote media service if the file isn't cached locally. Off by default, so an
      initial load only probes what's already local and a remote-only picture resolves to
      'not-downloaded' (the "click to load" placeholder); the click passes this to force the fetch. */
  downloadIfMissing?: boolean;
  /** Ignore any cached object URL and re-fetch (e.g. after the underlying file was replaced). */
  bypassCache?: boolean;
};

/**
 * Loads picture images and hands back a shared blob object URL per mediaUri. Scoped (via context) to
 * the entry view, so identical mediaUris across a sense's pictures — or the same picture shown in
 * both the field and a dialog — are fetched and decoded once. Cached object URLs live until the
 * entry view is torn down (see initImageService), then dispose() revokes them.
 *
 * loadImage() resolves to the final state (no 'loading' — that's the unresolved promise); callers
 * own their own pending UI. Only 'loaded' results are cached: a 'not-downloaded' or 'error' outcome
 * is cheap to re-derive and must stay re-attemptable, so a later loadImage on the same uri (e.g. a
 * retry click with downloadIfMissing) probes again rather than replaying the stale outcome.
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

  // getFileStream reports expected failures via the response (branched on below); an unexpected
  // thrown error (e.g. from blob()) bubbles to the global handler, and #getApi() throws if the
  // project api isn't ready yet — both are surfaced there, not swallowed into an error state.
  async #load(mediaUri: string, downloadIfMissing: boolean): Promise<ImageState> {
    const api = this.#getApi();
    const file = await api.getFileStream(mediaUri, downloadIfMissing);
    if (!file.stream) {
      // A local-only probe (downloadIfMissing=false) reports NotFound when the file simply isn't
      // cached yet — that's the click-to-download case, not an error.
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
    // Don't mint a URL if the service was torn down mid-load: dispose() has already run, so this one
    // would never be revoked. The awaiting caller is gone too, so the returned state is discarded.
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

/**
 * Creates an entry-view-scoped image cache and publishes it to descendants (pictures, the edit
 * dialog, the fullscreen viewer). Call once from the entry view; its object URLs are revoked when
 * that view is destroyed.
 */
export function initImageService(getApi: () => IMiniLcmJsInvokable): ImageService {
  const service = new ImageService(getApi);
  imageServiceContext.set(service);
  onDestroy(() => service.dispose());
  return service;
}

/** The entry-view image service, or undefined when rendered outside an entry view. */
export function useImageService(): ImageService | undefined {
  return imageServiceContext.getOr(undefined);
}
