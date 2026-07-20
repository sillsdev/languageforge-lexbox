import {SvelteMap} from 'svelte/reactivity';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';
import {ReadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ReadFileResult';
import {useProjectContext} from '$project/project-context.svelte';

export type ImageLoadState =
  | {status: 'not-loaded'}
  | {status: 'loading'}
  | {status: 'loaded'; url: string}
  | {status: 'error'; reason: 'not-found' | 'offline' | 'unknown'; detail?: string};

/**
 * Loads picture images once per mediaUri and hands out a shared blob object URL. Scoped to the open
 * project (see useImageService), so identical mediaUris across a sense's pictures — or the same
 * picture shown in a dialog, or revisited after navigating to another entry and back — are fetched
 * and decoded a single time. The cached object URLs live until the project is closed, then dispose()
 * revokes them.
 */
export class ImageService {
  readonly #getApi: () => IMiniLcmJsInvokable | undefined;
  readonly #cache = new SvelteMap<string, ImageLoadState>();
  #disposed = false;

  constructor(getApi: () => IMiniLcmJsInvokable | undefined) {
    this.#getApi = getApi;
  }

  /**
   * Begins loading `mediaUri` on demand (e.g. from a click). No-op if it's already been requested,
   * so repeated clicks — or several pictures sharing one mediaUri — trigger a single fetch.
   * Nothing loads until this is called: an untouched mediaUri stays {@link get}-reported as 'not-loaded'.
   */
  load(mediaUri: string): void {
    if (this.#cache.has(mediaUri)) return;
    this.#cache.set(mediaUri, {status: 'loading'});
    void this.#load(mediaUri);
  }

  /** Reactive load-state for `mediaUri`. Call from a $derived; 'not-loaded' until load() is called. */
  get(mediaUri: string): ImageLoadState {
    return this.#cache.get(mediaUri) ?? {status: 'not-loaded'};
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
