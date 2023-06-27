import {redirect} from '@sveltejs/kit';
import type { DocumentNode } from 'graphql';
import {
  mapExchange,
  type Client,
  defaultExchanges,
  type PromisifiedSource,
  type AnyVariables,
  type TypedDocumentNode,
  type OperationContext,
  type OperationResult,
  fetchExchange,
  CombinedError
} from '@urql/svelte';
import { createClient } from '@urql/svelte';
import { browser } from '$app/environment';
import { isObject } from '../util/types';
import { traceOperation, tracingExchange } from '$lib/otel';
import { LexGqlError, hasError, type ServerError } from './types';

let globalClient: GqlClient | null = null;

export function createGqlClient(_gqlEndpoint?: string): Client {
  const url = `/api/graphql`;
  return createClient({
    url,
    exchanges: [
      tracingExchange,
      mapExchange({
        onResult: (result) => {
          if (result.error) {
            result.error = new LexGqlError([result.error]);
            return result;
          }
          if (isObject(result.data)) {
            let errors: ServerError[] = [];
            for (const resultValue of Object.values(result.data)) {
              if (hasError(resultValue)) {
                errors = errors.concat(resultValue.errors.map(error => error));
              }
            }
            if (errors.length > 0) {
              result.error = new LexGqlError(errors);
            }
          }
          return result;
        }
      }),
      ...defaultExchanges,
      fetchExchange
    ]
  });
}

export function getClient(): GqlClient {
  if (browser) {
    if (globalClient) return globalClient;
    globalClient = new GqlClient(createGqlClient(''));
    return globalClient;
  } else {
    //We do not cache the client on the server side.
    return new GqlClient(createGqlClient());
  }
}

type OperationOptions = Partial<OperationContext & { throwErrors: boolean }>;

type QueryOperationOptions = OperationOptions
  & { fetch: typeof fetch }; // ensure the sveltekit fetch is always provided

class GqlClient {

  constructor(private readonly client: Client) {
    this.subscription = (...args) => this.client.subscription(...args);
  }

  query<Data = unknown, Variables extends AnyVariables = AnyVariables>(query: DocumentNode | TypedDocumentNode<Data, Variables> | string, variables: Variables, context: QueryOperationOptions): Promise<OperationResult<Data, Variables>> {
    context.throwErrors ??= true; // queries should generally just work
    return this.doOperation(context, (_context) => this.client.query<Data, Variables>(query, variables, _context));
  }

  mutation<Data = unknown, Variables extends AnyVariables = AnyVariables>(query: DocumentNode | TypedDocumentNode<Data, Variables> | string, variables: Variables, context: OperationOptions = {}): Promise<OperationResult<Data, Variables>> {
    return this.doOperation(context, (_context) => this.client.mutation<Data, Variables>(query, variables, _context));
  }

  // We can't wrap a subscription, because it's not just a web request,
  // but tracingExchange should trace subscription setup?
  // We can't throw errors, because errors thrown in wonka/an exchange kill node.
  subscription: typeof this.client.subscription;

  private async doOperation<Data, Variables extends AnyVariables>(context: OperationOptions, operation: (context: OperationOptions) => PromisifiedSource<OperationResult<Data, Variables>>): Promise<OperationResult<Data, Variables>> {
    return traceOperation(async () => {
      const result = await operation(context).toPromise();
      context?.throwErrors && this.throwAnyErrors(result);
      return result;
    });
  }

  private throwAnyErrors<T extends OperationResult<unknown, AnyVariables>>(result: T): void {
    if (!result.error) return;
    const error = result.error as LexGqlError;
    if (error.errors.length === 1 && is401(error.errors[0] as CombinedError)) {
      throw redirect(307, '/logout');
    }
    throw result.error;
  }
}

function is401(error: CombinedError): boolean {
  // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
  return error.response?.status === 401;
}
