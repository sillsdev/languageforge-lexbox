export const DEFAULT_PAGE_SIZE = 100;

export function limit<T>(items: T[], take = DEFAULT_PAGE_SIZE): T[] {
  return page(items, 0, take);
}

export function page<T>(item: T[], skip = 0, take = DEFAULT_PAGE_SIZE): T[] {
  return item.slice(skip, skip + take);
}
