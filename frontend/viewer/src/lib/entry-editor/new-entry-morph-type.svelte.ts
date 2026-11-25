import {MorphTypeKind, type IEntry} from '$lib/dotnet-types';
import {tick, untrack} from 'svelte';
import {Debounced} from 'runed';
import type {MorphTypesService} from '$project/data/morph-types.svelte';
import {
  checkLexemeForm,
  findMorphType,
  fitsMorphType,
  stripMarkers,
  type LexemeFormCheck,
  type LexemeFormProblem,
} from '$project/data/morph-type-markers';

const PAUSE_MS = 600;

export type NewEntryMorphTypeDeps = {
  entry: () => IEntry;
  vernacularWsIds: () => string[];
  /** Classic always shows the picker */
  alwaysShowPicker: () => boolean;
  morphTypes: Pick<MorphTypesService, 'current' | 'decorate'>;
};

/**
 * Like FLEx, affix markers typed in the lexeme form pick the morph type and are stripped on create.
 *
 * The type follows the form until the user picks one. A pick holds unless the markers contradict it, so a
 * picked root survives "zed", "-zed" makes a suffix, and deleting the "-" brings the root back.
 * A picked circumfix always holds: typing its left part ("a-") reads as a prefix.
 *
 * Lite shows the picker only once the form reads as something other than a stem or phrase, which is rare.
 * Waiting for a pause keeps "well-known" from showing it on the way through "well-".
 * Problems wait for the same pause, so "~a" doesn't flash an error before the closing "~" is typed;
 * a shown problem stays up while typing, kept current, until it's fixed.
 */
export class NewEntryMorphType {
  #deps: NewEntryMorphTypeDeps;
  #picked = $state<MorphTypeKind>();
  #unusual = $state(false);
  /** The type to read out to screen readers, set whenever the visible picker changes */
  announced = $state<MorphTypeKind>();

  #entry = $derived.by(() => this.#deps.entry());
  #firstForm = $derived.by(() => this.#deps.vernacularWsIds().map(wsId => this.#entry.lexemeForm[wsId]).find(form => form?.trim()) ?? '');
  #detected = $derived.by(() => findMorphType(this.#firstForm, this.#deps.morphTypes.current));
  #settledKind: Debounced<MorphTypeKind | undefined>;

  readonly showPicker = $derived.by(() => this.#deps.alwaysShowPicker() || this.#unusual);

  readonly checks: Record<string, LexemeFormCheck> = $derived.by(() => Object.fromEntries(this.#deps.vernacularWsIds()
    .filter(wsId => this.#entry.lexemeForm[wsId]?.trim())
    .map(wsId => [wsId, checkLexemeForm(this.#entry.lexemeForm[wsId], this.#entry.morphType, this.#deps.morphTypes.current)])));

  readonly blocked = $derived.by(() => Object.values(this.checks).some(check => check.problem && check.problem.kind !== 'leftoverMarker'));

  #problems = $derived.by(() => Object.fromEntries(Object.entries(this.checks)
    .flatMap(([wsId, {problem}]) => problem ? [[wsId, problem] as const] : [])));
  #settledProblems: Debounced<Record<string, LexemeFormProblem>>;

  /** Problems to show by writing system */
  readonly shownProblems: Record<string, LexemeFormProblem> = $derived.by(() => Object.fromEntries(Object.entries(this.#problems)
    .filter(([wsId]) => this.#settledProblems.current[wsId])));

  /** The lexeme form as it will be stored: markers stripped */
  readonly storedLexemeForm = $derived.by(() => {
    const lexemeForm = {...this.#entry.lexemeForm};
    for (const [wsId, check] of Object.entries(this.checks)) lexemeForm[wsId] = check.stored;
    return lexemeForm;
  });

  constructor(deps: NewEntryMorphTypeDeps) {
    this.#deps = deps;
    this.#settledKind = new Debounced(() => this.#detected.valid ? this.#detected.kind : undefined, PAUSE_MS);
    this.#settledProblems = new Debounced(() => this.#problems, PAUSE_MS);

    $effect(() => {
      const detected = this.#detected;
      if (!detected.valid) return;
      const picked = this.#picked;
      const unmarked = !detected.leading && !detected.trailing;
      const kind = picked && (picked === MorphTypeKind.Circumfix || unmarked || fitsMorphType(picked, detected.kind))
        ? picked : detected.kind;
      const entry = untrack(() => this.#entry);
      if (entry.morphType === kind) return;
      entry.morphType = kind;
      if (untrack(() => this.showPicker)) this.announced = kind;
    });

    $effect(() => {
      const kind = this.#settledKind.current;
      // Lite users can't fix a type mismatch in another writing system without the picker
      const mismatch = Object.values(this.shownProblems).some(problem => problem.kind === 'mismatch');
      if (untrack(() => this.#unusual) || !(mismatch || (kind && kind !== MorphTypeKind.Stem && kind !== MorphTypeKind.Phrase))) return;
      this.#unusual = true;
      this.announced = untrack(() => this.#entry.morphType);
    });
  }

  /** The user picked a type: hold it and rewrite the markers to match */
  onPicked(): void {
    const entry = this.#entry;
    this.#picked = entry.morphType;
    // FLEx leaves circumfix forms alone: their markers vary
    if (this.#picked === MorphTypeKind.Circumfix) return;
    for (const wsId of this.#deps.vernacularWsIds()) {
      const form = entry.lexemeForm[wsId];
      const match = findMorphType(form ?? '', this.#deps.morphTypes.current);
      if (!form?.trim() || (match.valid && match.kind === MorphTypeKind.Circumfix)) continue;
      const bare = stripMarkers(form, match);
      entry.lexemeForm[wsId] = this.#deps.morphTypes.decorate(bare, entry.morphType) ?? bare;
    }
  }

  /** Skips the pause. Returns true if that showed something new: the picker or a problem. */
  async settle(): Promise<boolean> {
    const before = {showPicker: this.showPicker, problems: Object.keys(this.shownProblems)};
    await Promise.all([this.#settledKind.updateImmediately(), this.#settledProblems.updateImmediately()]);
    await tick();
    return this.showPicker !== before.showPicker || Object.keys(this.shownProblems).some(wsId => !before.problems.includes(wsId));
  }

  reset(): void {
    this.#unusual = false;
    this.#picked = undefined;
    this.announced = undefined;
    this.#settledKind.setImmediately(undefined);
    this.#settledProblems.setImmediately({});
  }
}
