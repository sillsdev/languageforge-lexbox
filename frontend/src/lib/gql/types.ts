import { TraceableMixin as AsTraceable } from '$lib/otel/types';
import { CombinedError, type OperationResult } from '@urql/svelte';

export * from './generated/graphql';

export type $OpResult<T> = Promise<OperationResult<T>>;

export type ServerError = { message: string, code?: string };

export function hasError(value: unknown): value is { errors: ServerError[] } {
  if (typeof value !== 'object' || value === null) return false;
  return 'errors' in value && Array.isArray(value.errors);
}

export class LexGqlError extends AsTraceable(CombinedError) {
  constructor(public readonly errors: ServerError[]) {
    super({});
    this.message = errors.map(e => e.message).join(', ');
  }
}
