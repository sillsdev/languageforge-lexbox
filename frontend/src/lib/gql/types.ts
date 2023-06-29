import { isObjectWhere } from '$lib/util/types';

export * from './generated/graphql';

export interface GqlResult<T> {
  data?: T;
  error?: LexGqlError;
}

export type $OpResult<T> = Promise<GqlResult<T>>;

export interface GqlInputError {
  message: string;
  code?: string;
}

interface ErrorResult {
  errors: GqlInputError[];
}

export function isErrorResult(value: unknown): value is ErrorResult {
  return isObjectWhere<ErrorResult>(value, obj => Array.isArray(obj.errors));
}

export class LexGqlError {

  readonly message: string;

  constructor(public readonly errors: GqlInputError[]) {
    this.message = this.errors.map(e => e.message).join(', ');
  }

  forCode(code: string): GqlInputError[] | undefined {
    const codeErrors = this.errors.filter(error => error.code == code);
    return codeErrors.length > 0 ? codeErrors : undefined;
  }
}
