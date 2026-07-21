import {SvelteMap} from 'svelte/reactivity';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';
import {ReadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ReadFileResult';
import {useProjectContext} from '$project/project-context.svelte';

export type ImageLoadState =
  | {status: 'loading'}
  | {status: 'loaded'; url: string}
  | {status: 'not-downloaded'}
  | {status: 'error'; reason: 'not-found' | 'offline' | 'unknown'; detail?: string};

/**
 * Loads picture images and hands out a shared blob object URL, cached per mediaUri. Scoped to the
 * open project (see useImageService), so identical mediaUris across a sense's pictures — the same
 * picture shown in a dialog, or revisited after navigating to another entry and back — are fetched
 * and decoded a single time. Cached object URLs live until the project is closed, then dispose()
 * revokes them.
 *
 * ensureLocal() shows a picture that's already available locally without touching the network; a
 * picture that would have to be fetched from the remote media service stays 'not-downloaded' until
 * download() is called (wired to a "click/tap to load" placeholder).
 */
export class ImageService {
  readonly #getApi: () => IMiniLcmJsInvokable | undefined;
  readonly #cache = new SvelteMap<string, ImageLoadState>();
  #disposed = false;

  constructor(getApi: () => IMiniLcmJsInvokable | undefined) {
    this.#getApi = getApi;
  }

  /**
   * Loads `mediaUri` if it's already cached locally, without downloading a remote file. Runs once
   * per mediaUri (call from an $effect); a file that isn't local resolves to 'not-downloaded'.
   */
  ensureLocal(mediaUri: string): void {
    if (this.#cache.has(mediaUri)) return;
    this.#cache.set(mediaUri, {status: 'loading'});
    void this.#load(mediaUri, false);
  }

  /**
   * Downloads `mediaUri` from the remote media service on demand (e.g. a click on the placeholder).
   * No-op while it's already loading or loaded.
   */
  download(mediaUri: string): void {
    const current = this.#cache.get(mediaUri);
    if (current?.status === 'loading' || current?.status === 'loaded') return;
    this.#cache.set(mediaUri, {status: 'loading'});
    void this.#load(mediaUri, true);
  }

  /** Reactive load-state for `mediaUri`. Call from a $derived; 'loading' until ensureLocal resolves. */
  get(mediaUri: string): ImageLoadState {
    return this.#cache.get(mediaUri) ?? {status: 'loading'};
  }

  // getFileStream reports expected failures via the response (branched on below). An unexpected
  // thrown error (e.g. from blob()) is surfaced as an error state — else the picture would spin
  // forever on 'loading' — and then re-thrown so the global handler still reports it.
  async #load(mediaUri: string, downloadIfMissing: boolean): Promise<void> {
    const api = this.#getApi();
    if (!api) {
      this.#cache.set(mediaUri, {status: 'error', reason: 'unknown'});
      return;
    }
    try {
      const file = await api.getFileStream(mediaUri, downloadIfMissing);
      if (this.#disposed) return;
      if (!file.stream) {
        // A local-only probe (downloadIfMissing=false) reports NotFound when the file simply isn't
        // cached yet — that's the click-to-download case, not an error.
        if (!downloadIfMissing && file.result === ReadFileResult.NotFound) {
          this.#cache.set(mediaUri, {status: 'not-downloaded'});
          return;
        }
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
    } catch (error) {
      if (!this.#disposed) {
        this.#cache.set(mediaUri, {status: 'error', reason: 'unknown', detail: error instanceof Error ? error.message : undefined});
      }
      throw error;
    }
  }

  dispose(): void {
    this.#disposed = true;
    for (const state of this.#cache.values()) {
      if (state.status === 'loaded') URL.revokeObjectURL(state.url);
    }
    this.#cache.clear();
  }
}

const imageServiceKey = Symbol('ImageService');

/**
 * The project-scoped image cache (one per open project), or undefined when rendered outside a
 * project (e.g. stories). Created lazily on first use and cached on the project context, so an image
 * loaded in one entry stays cached when you navigate to another entry and back. Its object URLs are
 * revoked when the project context is destroyed (project closed).
 */
export function useImageService(): ImageService | undefined {
  const projectContext = useProjectContext();
  if (!projectContext) return undefined;
  return projectContext.getOrAdd(imageServiceKey, () => {
    const service = new ImageService(() => projectContext.maybeApi);
    // getOrAdd runs this inside the project context's $effect.root; its teardown revokes the URLs.
    $effect(() => () => service.dispose());
    return service;
  });
}
