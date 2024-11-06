/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

type QueryWsId = Omit<string, 'default'> | 'default';

export interface QueryOptions {
  order: {
    field: 'headword',
    writingSystem: QueryWsId,
    ascending?: boolean
  };
  count: number;
  offset: number;
  exemplar?: { value: string, writingSystem: QueryWsId };
}

export function pickWs(ws: QueryWsId, defaultWs: string): string {
  return ws === 'default' ? defaultWs : ws as string;
}
