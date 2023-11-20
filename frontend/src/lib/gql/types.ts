import { isObjectWhere } from '$lib/util/types';
import type {Operation} from '@urql/core';

export * from './generated/graphql';

export interface GqlResult<T extends GenericData> {
  data?: T;
  error?: LexGqlError<ExtractErrorTypename<T>>;
}

export type $OpResult<T extends GenericData> = Promise<GqlResult<T>>;

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type GenericData = Record<string, { errors: Errors } | Record<any, any> | null>;
// eslint-disable-next-line @typescript-eslint/naming-convention
type Errors = ({ __typename?: string, message?: string })[];
export type ExtractErrors<T extends GenericData> = Extract<NonNullable<T[keyof T]>['errors'], Errors>;
export type ExtractErrorTypename<T extends GenericData> = Extract<NonNullable<T[keyof T]>['errors'], Errors>[number]['__typename'];

export interface GqlInputError<Typename extends string> {
  // eslint-disable-next-line @typescript-eslint/naming-convention
  __typename: Typename;
  message: string;
  code?: string;
}

interface ErrorResult {
  errors: GqlInputError<string>[];
}

export function isErrorResult(value: unknown): value is ErrorResult {
  return isObjectWhere<ErrorResult>(value, obj => Array.isArray(obj.errors));
}

export class LexGqlError<Typename extends string> {

  readonly message: string;

  constructor(public readonly errors: GqlInputError<Typename>[]) {
    this.message = this.errors.map(e => e.message).join(', ');
    //sometimes there are errors but the message wasn't selected for some reason, if we have an empty string here then code lower down will think there's no error at all.
    if (this.message === '') this.message = 'Unknown error';
  }

  byCode(code: string): GqlInputError<Typename>[] | undefined {
    const codeErrors = this.errors.filter(error => error.code == code);
    return codeErrors.length > 0 ? codeErrors : undefined;
  }

  byType<T extends Typename>(typename: T): GqlInputError<T>[] | undefined {
    const codeErrors = this.errors.filter(error => error.__typename == typename) as GqlInputError<T>[];
    return codeErrors.length > 0 ? codeErrors : undefined;
  }
}

export function getOperationName(operation: Operation): string| undefined {
  const def = operation.query.definitions[0];
  if ('name' in def) return  def.name?.value;
  return undefined;
}
