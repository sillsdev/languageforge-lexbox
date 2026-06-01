import type {ResourceReturn} from 'runed';

/**
 * Reproduces the "wrapper service" pattern that PartOfSpeechService uses:
 * a class instance owning a DetachedResource, exposing a `$derived.by` view
 * of it. Consumers read that derived view from their own `$derived`
 * callbacks.
 *
 * When this pattern silently fails (resource resolves but the consumer's
 * derived stays stale), the visible symptom is e.g. PoS labels missing
 * from the dictionary preview.
 *
 * Lives in a plain `.svelte.ts` (not the harness's `<script module>`) so
 * the type flows through TypeScript without ESLint complaining about
 * `Unsafe member access` on a class re-exported from a Svelte file.
 */
export class WrapperService {
  readonly resource: ResourceReturn<string[], unknown, true>;

  constructor(resource: ResourceReturn<string[], unknown, true>) {
    this.resource = resource;
  }

  // Mirrors PartOfSpeechService.current — a $derived.by class field that
  // transforms the underlying resource value. This is the pattern that
  // breaks under Svelte 5.55.3+ "freeze deriveds once their containing
  // effects are destroyed" when the service is created inside a transient
  // component context.
  transformed: Array<{value: string; upper: string}> = $derived.by(() => {
    return this.resource.current.map(value => ({value, upper: value.toUpperCase()}));
  });
}
