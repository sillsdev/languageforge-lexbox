import {describe, it, expect} from 'vitest';
import {knownChangeTypes} from '$lib/dotnet-types/generated-types/LcmCrdt/ChangeTypes';
import {changeTypeSection, CHANGE_TYPE_SECTIONS, explicitlyGroupedChangeTypes} from './change-type-groups';

describe('change-type filter grouping', () => {
  it('maps every generated change type to a filter section', () => {
    const ungrouped = knownChangeTypes.filter((key) => changeTypeSection(key) === undefined);
    expect(ungrouped, 'add these to change-type-groups.ts so they get a filter section').toEqual([]);
  });

  it('only maps to declared sections', () => {
    const sections = new Set<string>(CHANGE_TYPE_SECTIONS);
    const bad = knownChangeTypes.filter((key) => !sections.has(changeTypeSection(key)!));
    expect(bad).toEqual([]);
  });

  it('has no mapping for a change type the backend never emits (reverse coverage — no dead entries)', () => {
    const known = new Set<string>(knownChangeTypes);
    const stale = explicitlyGroupedChangeTypes.filter((key) => !known.has(key));
    expect(stale).toEqual([]);
    expect(changeTypeSection('jsonPatch:NoSuchEntity')).toBeUndefined();
  });

  it('routes the subtle cases where a change edits a different entity than it names', () => {
    // A semantic-domain tag added to a sense is a sense change; creating the domain itself is vocabulary.
    expect(changeTypeSection('AddSemanticDomainChange')).toBe('sense');
    expect(changeTypeSection('CreateSemanticDomainChange')).toBe('vocabulary');
    // Publications: entry membership vs the publication object.
    expect(changeTypeSection('AddPublicationChange')).toBe('entry');
    expect(changeTypeSection('CreatePublicationChange')).toBe('vocabulary');
    // Component links group under entries from either end.
    expect(changeTypeSection('delete:ComplexFormComponent')).toBe('entry');
  });
});
