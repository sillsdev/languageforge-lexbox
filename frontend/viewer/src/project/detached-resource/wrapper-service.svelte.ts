import type {ResourceReturn} from 'runed';

/**
 * Mirrors the PartOfSpeechService pattern: a cached class instance owning a
 * DetachedResource and exposing a `$derived.by` view that consumers read from
 * their own `$derived`. This is the shape that went stale under the bug fixed in
 * ProjectContext#ownAndCache.
 *
 * Kept in a plain `.svelte.ts` (not the harness's `<script module>`) to dodge an
 * ESLint "unsafe member access" on a class re-exported from a `.svelte` file.
 */
export class WrapperService {
  readonly resource: ResourceReturn<string[], unknown, true>;

  constructor(resource: ResourceReturn<string[], unknown, true>) {
    this.resource = resource;
  }

  transformed: Array<{value: string; upper: string}> = $derived.by(() => {
    return this.resource.current.map(value => ({value, upper: value.toUpperCase()}));
  });
}
