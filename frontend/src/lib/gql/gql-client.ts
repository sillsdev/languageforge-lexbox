import {
  type Client,
  type AnyVariables,
  type TypedDocumentNode,
  type OperationContext,
  type OperationResult,
  fetchExchange,
  queryStore,
  type OperationResultSource,
  type OperationResultStore
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
          /* eslint-disable @typescript-eslint/naming-convention */
          'Changeset': () => null,
          'UsersCollectionSegment': () => null,
          /* eslint-enable @typescript-eslint/naming-convention */
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

  queryStore<Data = unknown, Variables extends AnyVariables = AnyVariables>(
    fetch: Fetch,
    query: TypedDocumentNode<Data, Variables>,
    variables: Variables,
    context: QueryOperationOptions = {}): OperationResultStore<Data, Variables> {
    const resultStore = queryStore<Data, Variables>({
      client: this.client,
      query,
      variables,
      context: {fetch, ...context}
    });

    if (browser) {
      return derived(resultStore, (result) => {
        this.throwAnyUnexpectedErrors(result);
        return result;
      });
    } else {
      /**
       * We kill node if we validate each query result and throw in the urql pipeline, but we shouldn't ever need to, because:
       * 1) Only the initial result of the query store will ever be fetched server-side
       * 2) If we want to await the initial result server-side, then we should be using `awaitedQueryStore`, where we CAN safely validate the result and throw
       * 3) If we don't await the initial result server-side then there should never be a result OR an error server-side
       */
      return resultStore;
    }
  }

  async awaitedQueryStore<Data = unknown, Variables extends AnyVariables = AnyVariables>(
    fetch: Fetch,
    query: TypedDocumentNode<Data, Variables>,
    variables: Variables,
    context: QueryOperationOptions = {}): Promise<QueryStoreReturnType<Data>> {
    const resultStore = this.queryStore<Data, Variables>(fetch, query, variables, context);

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
    // unexpected status codes are handled in the fetch hooks
    // throws there (e.g. SvelteKit redirects) turn into networkErrors that we rethrow here
    if (error.networkError) throw error.networkError;
    throw error;
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
