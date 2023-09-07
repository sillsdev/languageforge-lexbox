import {redirect} from '@sveltejs/kit';
import {
  type Client,
  type AnyVariables,
  type TypedDocumentNode,
  type OperationContext,
  type OperationResult,
  fetchExchange,
  CombinedError,
  queryStore,
  type OperationResultSource
} from '@urql/svelte';
import {createClient} from '@urql/svelte';
import {browser} from '$app/environment';
import {isObject} from '../util/types';
import {tracingExchange} from '$lib/otel';
import {LexGqlError, isErrorResult, type $OpResult, type GqlInputError, type ExtractErrorTypename, type GenericData} from './types';
import type {Readable, Unsubscriber} from 'svelte/store';
import {derived} from 'svelte/store';
import {cacheExchange} from '@urql/exchange-graphcache';
import {devtoolsExchange} from '@urql/devtools';
import type { LexAuthUser } from '$lib/user';

let globalClient: GqlClient | null = null;

function createGqlClient(_gqlEndpoint?: string): Client {
  const url = `/api/graphql`;
  return createClient({
    url,
    exchanges: [
      ...(import.meta.env.DEV ? [devtoolsExchange] : []),
      cacheExchange({
        keys: {
          //     eslint-disable-next-line @typescript-eslint/naming-convention
          'Changeset': () => null,
        }
      }),
      tracingExchange,
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

export function ensureClientMatchesUser(user: LexAuthUser): void {
  if (!globalClient) return;
  if (globalClient.ownedByUserId === '') globalClient.ownedByUserId = user.id;
  if (globalClient.ownedByUserId === user.id) return;

  console.warn(`Deleting the current client since it is owned by a different user, this will clear the cache, this happens after the load function, so it may reuse cached data from the old user, ${globalClient.ownedByUserId} !== ${user.id}`)
  globalClient = null;
  getClient().ownedByUserId = user.id;
}

type OperationOptions = Partial<OperationContext>;

type QueryOperationOptions = OperationOptions; // ensure the sveltekit fetch is always provided

type OperationResultState<Data, Variables extends AnyVariables> = ReturnType<typeof queryStore<Data, Variables>> extends Readable<infer T> ? T : never;
type QueryStoreReturnType<Data> = { [K in keyof Data]: Readable<Data[K]> };

class GqlClient {
  public ownedByUserId = '';
  constructor(public readonly client: Client) {
    this.subscription = (...args) => this.client.subscription(...args);
  }

  query<Data extends GenericData, Variables extends AnyVariables = AnyVariables>(query: TypedDocumentNode<Data, Variables>, variables: Variables, context: QueryOperationOptions = {}): $OpResult<Data> {
    return this.doOperation(
      context,
      (_context) => this.client.query<Data, Variables>(query, variables, _context)
    );
  }

  async queryStore<Data = unknown, Variables extends AnyVariables = AnyVariables>(
    fetch: Fetch,
    query: TypedDocumentNode<Data, Variables>,
    variables: Variables,
    context: QueryOperationOptions = {}): Promise<QueryStoreReturnType<Data>> {
    const resultStore = queryStore<Data, Variables>({
      client: this.client,
      query,
      variables,
      context: {fetch, ...context}
    });
    const results = await new Promise<OperationResultState<Data, Variables>>((resolve) => {
      let invalidate = undefined as Unsubscriber | undefined;
      invalidate = resultStore.subscribe(value => {
        if (value.fetching) return;
        if (invalidate) invalidate();
        resolve(value);
      });
    });

    this.throwAnyUnexpectedErrors(results);
    const keys = Object.keys(results.data ?? {}) as Array<keyof typeof results.data>;
    const resultData = {} as Record<string, Readable<unknown>>;
    for (const key of keys) {
      resultData[key] = derived(resultStore, value => {
        const dataValue = value.data ? value.data[key] : undefined;
        return dataValue;
      });
    }

    return resultData as QueryStoreReturnType<Data>;
  }

  mutation<Data extends GenericData, Variables extends AnyVariables = AnyVariables>(query: TypedDocumentNode<Data, Variables>, variables: Variables, context: OperationOptions = {}): $OpResult<Data> {
    return this.doOperation(
      context,
      (_context) => this.client.mutation<Data, Variables>(query, variables, _context)
    );
  }

  // We can't wrap a subscription, because it's not just a web request,
  // but tracingExchange should trace subscription setup?
  // We can't throw errors, because errors thrown in wonka/an exchange kill node.
  subscription: typeof this.client.subscription;

  private async doOperation<Data extends GenericData, Variables extends AnyVariables>(context: OperationOptions, operation: (context: OperationOptions) => OperationResultSource<OperationResult<Data, Variables>>): $OpResult<Data> {
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

  private findInputErrors<T extends GenericData>({data}: OperationResult<T, AnyVariables>): LexGqlError<ExtractErrorTypename<T>> | undefined {
    const errors: GqlInputError<ExtractErrorTypename<T>>[] = [];
    if (isObject(data)) {
      for (const resultValue of Object.values(data)) {
        if (isErrorResult(resultValue)) {
          errors.push(...resultValue.errors as GqlInputError<ExtractErrorTypename<T>>[]);
        }
      }
    }

    return errors.length > 0 ? new LexGqlError(errors) : undefined;
  }
}
