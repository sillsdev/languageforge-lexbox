/**
 * The fields the sort reads. Structural so this plain .ts module needn't import types from a .svelte module,
 * which eslint can't resolve. Both ProjectItemWithDraftStatus variants satisfy it.
 */
export type SortableProject = {
  name: string;
  code: string;
  isDraft?: boolean;
  userCount?: number | null;
  createdDate?: string | Date | null;
  deletedDate?: string | Date | null;
  lastCommit?: string | Date | null;
};

export const projectSortColumns = ['name', 'code', 'users', 'createdAt', 'lastChange'] as const;
export type ProjectSortColumn = (typeof projectSortColumns)[number];
export type SortDir = 'asc' | 'desc';
export type ProjectSort = {column: ProjectSortColumn; dir: SortDir};

export function isProjectSortColumn(value: string | undefined): value is ProjectSortColumn {
  return projectSortColumns.includes(value as ProjectSortColumn);
}

/** Text columns start A→Z, numbers and dates start with the biggest/newest. */
export function defaultSortDir(column: ProjectSortColumn): SortDir {
  return column === 'name' || column === 'code' ? 'asc' : 'desc';
}

/** Clicking the active column reverses it, clicking another column switches to it in its default direction. */
export function nextProjectSort(current: ProjectSort, clicked: ProjectSortColumn): ProjectSort {
  if (current.column === clicked) return {column: clicked, dir: current.dir === 'asc' ? 'desc' : 'asc'};
  return {column: clicked, dir: defaultSortDir(clicked)};
}

function toTime(value: string | Date | null | undefined): number | undefined {
  return value ? new Date(value).getTime() : undefined;
}

// Mirrors what ProjectTable shows in each column, so the order matches what the user sees.
function sortValue(project: SortableProject, column: ProjectSortColumn): string | number | undefined {
  switch (column) {
    case 'name':
      return project.name.toLocaleLowerCase();
    case 'code':
      return project.code.toLocaleLowerCase();
    case 'users':
      return project.isDraft ? undefined : (project.userCount ?? undefined);
    case 'createdAt':
      return toTime(project.createdDate);
    case 'lastChange':
      if (project.isDraft) return undefined;
      return toTime(project.deletedDate ?? project.lastCommit);
  }
}

function compareValues(a: string | number, b: string | number): number {
  if (typeof a === 'string' && typeof b === 'string') return a.localeCompare(b);
  return (a as number) - (b as number);
}

/** Sorts a copy of `projects`. Projects with no value for the column (e.g. never changed) always go last. */
export function sortProjects<T extends SortableProject>(projects: T[], sort: ProjectSort): T[] {
  const direction = sort.dir === 'asc' ? 1 : -1;
  return projects.toSorted((a, b) => {
    const aValue = sortValue(a, sort.column);
    const bValue = sortValue(b, sort.column);
    let result = 0;
    if (aValue === undefined || bValue === undefined) {
      if (aValue !== bValue) return aValue === undefined ? 1 : -1;
    } else {
      result = compareValues(aValue, bValue) * direction;
    }
    return (
      result ||
      a.name.toLocaleLowerCase().localeCompare(b.name.toLocaleLowerCase()) ||
      a.code.localeCompare(b.code)
    );
  });
}
