import type { OperationResult } from '@urql/svelte';

export * from './generated/graphql';

export type $OpResult<T> = Promise<OperationResult<T>>;
