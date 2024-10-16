/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */
/* eslint-disable @typescript-eslint/no-redundant-type-constituents */
export interface QueryOptions {
  order: {
    field: 'headword',
    writingSystem: Exclude<string, 'default'> | 'default',
    ascending?: boolean
  };
  count: number;
  offset: number;
  exemplar?: { value: string, writingSystem: Exclude<string, 'default'> | 'default' };
}
