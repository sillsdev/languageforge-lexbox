/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

export interface QueryOptions {
  order: {
    field: 'headword',
    writingSystem: string | 'default',
    ascending?: boolean
  };
  count: number;
  offset: number;
  exemplar?: { value: string, writingSystem: string | 'default' };
}
