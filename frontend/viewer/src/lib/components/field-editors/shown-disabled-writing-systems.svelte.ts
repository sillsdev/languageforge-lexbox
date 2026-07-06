import type {IMultiString, IRichMultiString, IWritingSystem} from '$lib/dotnet-types';
import type {ReadonlyDeep} from 'type-fest';
import {SvelteSet} from 'svelte/reactivity';
import {asString, useWritingSystemService} from '$project/data';

/**
 * The disabled writing systems a field should currently show: those with data in the field's value.
 * Sticky per bound value: once a writing system is shown it stays visible while the field is being
 * edited, so clearing the text doesn't remove the input out from under the user.
 * Must be called during component initialization.
 */
export function useShownDisabledWritingSystems(
  writingSystems: () => ReadonlyArray<ReadonlyDeep<IWritingSystem>>,
  value: () => IMultiString | IRichMultiString,
): {readonly current: IWritingSystem[]} {
  const writingSystemService = useWritingSystemService();
  const shownWsIds = new SvelteSet<string>();
  let lastValue: unknown = undefined;

  const candidates = $derived(writingSystemService.disabledCandidates(writingSystems()));

  $effect(() => {
    const currentValue = value();
    if (lastValue !== currentValue) {
      lastValue = currentValue;
      shownWsIds.clear();
    }
    for (const ws of candidates) {
      if (asString(currentValue[ws.wsId])) shownWsIds.add(ws.wsId);
    }
  });

  const current = $derived(candidates.filter((ws) => shownWsIds.has(ws.wsId) || !!asString(value()[ws.wsId])));

  return {
    get current() {
      return current;
    },
  };
}
