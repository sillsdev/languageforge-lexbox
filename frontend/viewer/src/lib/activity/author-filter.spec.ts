import {describe, expect, it} from 'vitest';
import {
  type AuthorFilterValue,
  serializeAuthorFilter,
  deserializeAuthorFilter,
  authorFilterToActivityFilter,
} from './author-filter';

describe('AuthorFilter serialization', () => {
  const roundTrips: AuthorFilterValue[] = [
    {kind: 'all'},
    {kind: 'fieldWorks'},
    {kind: 'fwLiteUsers'},
    {kind: 'missing'},
    {kind: 'author', authorName: 'Tim Haasdyk'},
    {kind: 'author', authorName: 'Author With: Colon'},
    {kind: 'author', authorName: 'FieldWorks'}, // user literally named FieldWorks
    {kind: 'author', authorName: 'fwLiteUsers'}, // user with reserved-looking name
  ];

  it.each(roundTrips)('round-trips %j', (value) => {
    expect(deserializeAuthorFilter(serializeAuthorFilter(value))).toEqual(value);
  });

  it('serializes "all" as empty so URL stays clean', () => {
    expect(serializeAuthorFilter({kind: 'all'})).toBe('');
  });

  it('deserializes unknown input as "all" rather than throwing', () => {
    expect(deserializeAuthorFilter('garbage')).toEqual({kind: 'all'});
    expect(deserializeAuthorFilter('me:legacy')).toEqual({kind: 'all'}); // dropped 'me' kind from earlier design
  });
});

describe('authorFilterToActivityFilter', () => {
  it('returns undefined for "all" so callers can omit the filter', () => {
    expect(authorFilterToActivityFilter({kind: 'all'})).toBeUndefined();
  });

  it('maps fieldWorks to an exact author match on the FieldWorks name', () => {
    expect(authorFilterToActivityFilter({kind: 'fieldWorks'})).toEqual({
      authorName: 'FieldWorks',
      authorMissing: false,
      excludeFieldWorks: false,
    });
  });

  it('maps fwLiteUsers to excludeFieldWorks (no authorName match)', () => {
    expect(authorFilterToActivityFilter({kind: 'fwLiteUsers'})).toEqual({
      authorMissing: false,
      excludeFieldWorks: true,
    });
  });

  it('maps missing to authorMissing', () => {
    expect(authorFilterToActivityFilter({kind: 'missing'})).toEqual({
      authorMissing: true,
      excludeFieldWorks: false,
    });
  });

  it('maps author to an exact-name match', () => {
    expect(authorFilterToActivityFilter({kind: 'author', authorName: 'Alice'})).toEqual({
      authorName: 'Alice',
      authorMissing: false,
      excludeFieldWorks: false,
    });
  });
});
