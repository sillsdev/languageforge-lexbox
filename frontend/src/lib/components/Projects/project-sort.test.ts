import {describe, expect, test} from 'vitest';
import {nextProjectSort, type SortableProject, sortProjects} from './project-sort';

function project(fields: Partial<SortableProject> & {code: string}): SortableProject {
  return {name: fields.code, isDraft: false, ...fields};
}

function codes(projects: SortableProject[]): string[] {
  return projects.map((p) => p.code);
}

describe('sortProjects', () => {
  test('sorts by code case-insensitively in both directions', () => {
    const projects = [project({code: 'b-flex'}), project({code: 'C-flex'}), project({code: 'a-flex'})];
    expect(codes(sortProjects(projects, {column: 'code', dir: 'asc'}))).toEqual(['a-flex', 'b-flex', 'C-flex']);
    expect(codes(sortProjects(projects, {column: 'code', dir: 'desc'}))).toEqual(['C-flex', 'b-flex', 'a-flex']);
  });

  test('puts projects that were never changed last, whichever the direction', () => {
    const projects = [
      project({code: 'never', lastCommit: null}),
      project({code: 'old', lastCommit: '2025-01-01T00:00:00Z'}),
      project({code: 'new', lastCommit: '2026-01-01T00:00:00Z'}),
    ];
    expect(codes(sortProjects(projects, {column: 'lastChange', dir: 'desc'}))).toEqual(['new', 'old', 'never']);
    expect(codes(sortProjects(projects, {column: 'lastChange', dir: 'asc'}))).toEqual(['old', 'new', 'never']);
  });

  test('sorts drafts in with real projects by created date, but last by users', () => {
    const projects = [
      project({code: 'older', createdDate: '2025-01-01T00:00:00Z', userCount: 1}),
      project({code: 'draft', createdDate: '2026-01-01T00:00:00Z', isDraft: true}),
      project({code: 'newer', createdDate: '2025-06-01T00:00:00Z', userCount: 5}),
    ];
    expect(codes(sortProjects(projects, {column: 'createdAt', dir: 'desc'}))).toEqual(['draft', 'newer', 'older']);
    expect(codes(sortProjects(projects, {column: 'users', dir: 'asc'}))).toEqual(['older', 'newer', 'draft']);
  });

  test('breaks ties by name and leaves the input untouched', () => {
    const projects = [project({code: 'x', name: 'Zed', userCount: 2}), project({code: 'y', name: 'alpha', userCount: 2})];
    expect(codes(sortProjects(projects, {column: 'users', dir: 'desc'}))).toEqual(['y', 'x']);
    expect(codes(projects)).toEqual(['x', 'y']);
  });
});

describe('nextProjectSort', () => {
  test('reverses the active column and starts a new column in its default direction', () => {
    expect(nextProjectSort({column: 'code', dir: 'asc'}, 'code')).toEqual({column: 'code', dir: 'desc'});
    expect(nextProjectSort({column: 'code', dir: 'asc'}, 'lastChange')).toEqual({column: 'lastChange', dir: 'desc'});
    expect(nextProjectSort({column: 'lastChange', dir: 'desc'}, 'name')).toEqual({column: 'name', dir: 'asc'});
  });
});
