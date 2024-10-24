export function projectUrl({code, id}: {code: string, id?: string}): string {
  const idParam = id ? `?id=${id}` : '';
  return `/project/${code}${idParam}`;
}
