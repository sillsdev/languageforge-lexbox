import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {flushSync} from 'svelte';
import {MorphTypeKind as K, type IEntry, type IMorphType} from '$lib/dotnet-types';
import {NewEntryMorphType} from './new-entry-morph-type.svelte';

const morphTypes = [
  {kind: K.BoundStem, prefix: '*'},
  {kind: K.Circumfix},
  {kind: K.Infix, prefix: '-', postfix: '-'},
  {kind: K.Prefix, postfix: '-'},
  {kind: K.Root},
  {kind: K.Stem},
  {kind: K.Suffix, prefix: '-'},
  {kind: K.Suprafix, prefix: '~', postfix: '~'},
  {kind: K.Phrase},
] as IMorphType[];

describe('NewEntryMorphType', () => {
  let entry = $state({} as IEntry);
  let classic: boolean;
  let morphType: NewEntryMorphType;
  let cleanup: () => void;

  function setup(view: 'classic' | 'lite') {
    classic = view === 'classic';
    cleanup = $effect.root(() => {
      morphType = new NewEntryMorphType({
        entry: () => entry,
        vernacularWsIds: () => ['a', 'b'],
        alwaysShowPicker: () => classic,
        morphTypes: {
          current: morphTypes,
          decorate: (form, kind) => {
            const type = morphTypes.find(mt => mt.kind === kind);
            return form && `${type?.prefix ?? ''}${form}${type?.postfix ?? ''}`;
          },
        },
      });
    });
    flushSync();
  }

  function type(form: string, wsId = 'a') {
    entry.lexemeForm[wsId] = form;
    flushSync();
  }

  function pick(kind: K) {
    entry.morphType = kind;
    morphType.onPicked();
    flushSync();
  }

  function pause() {
    vi.advanceTimersByTime(600);
    flushSync();
  }

  beforeEach(() => {
    vi.useFakeTimers();
    entry = {lexemeForm: {}, morphType: K.Stem} as unknown as IEntry;
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
  });

  it('follows the form until the user picks a type', () => {
    setup('classic');
    type('-zed');
    expect(entry.morphType).toBe(K.Suffix);
    type('zed zed');
    expect(entry.morphType).toBe(K.Phrase);
    type('a- -b');
    expect(entry.morphType).toBe(K.Circumfix);
  });

  it('holds a pick unless the markers contradict it', () => {
    setup('classic');
    pick(K.Root);
    type('zed');
    expect(entry.morphType).toBe(K.Root);
    type('-zed');
    expect(entry.morphType).toBe(K.Suffix);
    type('zed');
    expect(entry.morphType).toBe(K.Root);
  });

  it('holds a picked circumfix while its left part reads as a prefix', () => {
    setup('classic');
    pick(K.Circumfix);
    type('a-');
    expect(entry.morphType).toBe(K.Circumfix);
    type('a- -b');
    expect(entry.morphType).toBe(K.Circumfix);
  });

  it('rewrites the markers of every form when the user picks a type', () => {
    setup('classic');
    type('-zed');
    type('-zod', 'b');
    pick(K.Prefix);
    expect(entry.lexemeForm).toEqual({a: 'zed-', b: 'zod-'});
  });

  it('stores the forms without markers', () => {
    setup('classic');
    type('-zed');
    expect(morphType.storedLexemeForm).toEqual({a: 'zed'});
  });

  it('shows the picker in lite only after a pause on a form that is not a stem or phrase', () => {
    setup('lite');
    type('well-');
    type('well-known');
    pause();
    expect(morphType.showPicker).toBe(false);
    type('zed zed');
    pause();
    expect(morphType.showPicker).toBe(false);

    type('-zed');
    expect(morphType.showPicker).toBe(false);
    pause();
    expect(morphType.showPicker).toBe(true);
    expect(morphType.announced).toBe(K.Suffix);

    type('zed');
    pause();
    expect(morphType.showPicker).toBe(true);
  });

  it('shows the picker in lite when another writing system mismatches the type', () => {
    setup('lite');
    type('zed');
    type('-zed', 'b');
    pause();
    expect(morphType.shownProblems.b?.kind).toBe('mismatch');
    expect(morphType.showPicker).toBe(true);
  });

  it('shows a problem after a pause and keeps it up while the form changes to another problem', () => {
    setup('classic');
    type('*zed-');
    expect(morphType.blocked).toBe(true);
    expect(morphType.shownProblems).toEqual({});
    pause();
    expect(morphType.shownProblems.a?.kind).toBe('unmatchedMarkers');

    type('-zed~');
    expect(morphType.shownProblems.a?.kind).toBe('unmatchedMarkers');
    type('-zed');
    expect(morphType.shownProblems).toEqual({});
    expect(morphType.blocked).toBe(false);
  });

  it('only warns about a leftover marker', () => {
    setup('classic');
    type('--zed');
    pause();
    expect(morphType.shownProblems.a).toEqual({kind: 'leftoverMarker', marker: '-'});
    expect(morphType.blocked).toBe(false);
  });

  it('settle skips the pause and reports what it newly showed', async () => {
    vi.useRealTimers(); // runScheduledNow waits on the debounce promise
    setup('lite');
    type('-zed');
    expect(await morphType.settle()).toBe(true);
    expect(morphType.showPicker).toBe(true);
    expect(await morphType.settle()).toBe(false);
  });

  it('reset hides the lite picker and forgets the pick', () => {
    setup('lite');
    pick(K.Root);
    type('-zed');
    pause();
    morphType.reset();
    flushSync();
    expect(morphType.showPicker).toBe(false);
    expect(morphType.announced).toBeUndefined();
    type('zed');
    expect(entry.morphType).toBe(K.Stem);
  });
});
