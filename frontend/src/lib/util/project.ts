export function projectUrl({code, id}: {code: string, id?: string}): `/project/${string}` {
  const idParam = id ? `?id=${id}` : '';
  return `/project/${code}${idParam}`;
}
