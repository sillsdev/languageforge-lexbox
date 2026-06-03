import type {IProjectActivityFilter} from '$lib/dotnet-types';

export type AuthorFilterValue =
  | {kind: 'all'}
  | {kind: 'fieldWorks'}
  | {kind: 'fwLiteUsers'}
  | {kind: 'author'; authorName: string}
  | {kind: 'missing'};

export const FIELD_WORKS_AUTHOR_NAME = 'FieldWorks';

export function serializeAuthorFilter(value: AuthorFilterValue): string {
  if (value.kind === 'all') return '';
  if (value.kind === 'author') return `author:${value.authorName}`;
  return value.kind;
}

export function deserializeAuthorFilter(raw: string): AuthorFilterValue {
  if (!raw) return {kind: 'all'};
  if (raw === 'fieldWorks' || raw === 'fwLiteUsers' || raw === 'missing') return {kind: raw};
  if (raw.startsWith('author:')) return {kind: 'author', authorName: raw.slice('author:'.length)};
  return {kind: 'all'};
}

export function authorFilterToActivityFilter(value: AuthorFilterValue): IProjectActivityFilter | undefined {
  switch (value.kind) {
    case 'all':         return undefined;
    case 'fieldWorks':  return {authorName: FIELD_WORKS_AUTHOR_NAME, authorMissing: false, excludeFieldWorks: false};
    case 'fwLiteUsers': return {authorMissing: false, excludeFieldWorks: true};
    case 'missing':     return {authorMissing: true, excludeFieldWorks: false};
    case 'author':      return {authorName: value.authorName, authorMissing: false, excludeFieldWorks: false};
  }
}
