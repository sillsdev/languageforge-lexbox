import { redirect } from '@sveltejs/kit';
import type { DocumentNode } from 'graphql';
import {
  type Client,
  type PromisifiedSource,
  type AnyVariables,
  type TypedDocumentNode,
  type OperationContext,
  type OperationResult,
  fetchExchange,
  CombinedError,
  cacheExchange
} from '@urql/svelte';
import { createClient } from '@urql/svelte';
import { browser } from '$app/environment';
import { isObject } from '../util/types';
import { tracingExchange } from '$lib/otel';
import { LexGqlError, isErrorResult, type $OpResult, type GqlInputError } from './types';

let globalClient: GqlClient | null = null;

function createGqlClient(_gqlEndpoint?: string): Client {
  const url = `/api/graphql`;
  return createClient({
    url,
    exchanges: [
      tracingExchange,
      cacheExchange,
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

type OperationOptions = Partial<OperationContext>;

type QueryOperationOptions = OperationOptions
  & { fetch: Fetch }; // ensure the sveltekit fetch is always provided

class GqlClient {

  constructor(private readonly client: Client) {
    this.subscription = (...args) => this.client.subscription(...args);
  }

  query<Data = unknown, Variables extends AnyVariables = AnyVariables>(query: DocumentNode | TypedDocumentNode<Data, Variables> | string, variables: Variables, context: QueryOperationOptions): $OpResult<Data> {
    return this.doOperation(context, (_context) => this.client.query<Data, Variables>(query, variables, _context));
  }

  mutation<Data = unknown, Variables extends AnyVariables = AnyVariables>(query: DocumentNode | TypedDocumentNode<Data, Variables> | string, variables: Variables, context: OperationOptions = {}): $OpResult<Data> {
    return this.doOperation(context, (_context) => this.client.mutation<Data, Variables>(query, variables, _context));
  }

  // We can't wrap a subscription, because it's not just a web request,
  // but tracingExchange should trace subscription setup?
  // We can't throw errors, because errors thrown in wonka/an exchange kill node.
  subscription: typeof this.client.subscription;

  private async doOperation<Data, Variables extends AnyVariables>(context: OperationOptions, operation: (context: OperationOptions) => PromisifiedSource<OperationResult<Data, Variables>>): $OpResult<Data> {
    const result = await operation(context).toPromise();
    this.throwAnyUnexpectedErrors(result);
    return {
      data: result.data,
      error: this.findInputErrors(result),
    };
  }

  private throwAnyUnexpectedErrors<T extends OperationResult<unknown, AnyVariables>>(result: T): void {
    const error = result.error;
    if (!error) return;
    if (this.is401(error)) throw redirect(307, '/logout');
    if (error.networkError) throw error.networkError; // e.g. SvelteKit redirects
    throw error;
  }

  private is401(error: CombinedError): boolean {
    return (error.response as Response | undefined)?.status === 401;
  }

  private findInputErrors<T>({ data }: OperationResult<T, AnyVariables>): LexGqlError | undefined {
    const errors: GqlInputError[] = [];
    if (isObject(data)) {
      for (const resultValue of Object.values(data)) {
        if (isErrorResult(resultValue)) {
          errors.push(...resultValue.errors);
        }
      }
    }
    return errors.length > 0 ? undefined : new LexGqlError(errors);
  }
}
