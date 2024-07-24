import {
  type AnyVariables,
  type Client,
  createClient,
  fetchExchange,
  type OperationContext,
  type OperationResult,
  type OperationResultSource,
  type OperationResultStore,
  type Pausable,
  queryStore,
  type TypedDocumentNode
} from '@urql/svelte';
import {browser} from '$app/environment';
import {isObject} from '../util/types';
import {tracingExchange} from '$lib/otel';
import {
  type $OpResult,
  type ChangeUserAccountBySelfMutationVariables,
  type DeleteUserByAdminOrSelfMutationVariables,
  type ExtractErrorTypename,
  type GenericData,
  type GqlInputError,
  isErrorResult,
  type LeaveProjectMutationVariables,
  LexGqlError,
  type SoftDeleteProjectMutationVariables,
  type BulkAddProjectMembersMutationVariables,
  type DeleteDraftProjectMutationVariables,
  type MutationAddProjectToOrgArgs,
  type MutationRemoveProjectFromOrgArgs,
  type BulkAddOrgMembersMutationVariables,
  type ChangeOrgMemberRoleMutationVariables,
  type AddOrgMemberMutationVariables,
  type CreateProjectMutationVariables,
} from './types';
import type {Readable, Unsubscriber} from 'svelte/store';
import {derived} from 'svelte/store';
import {cacheExchange} from '@urql/exchange-graphcache';
import {devtoolsExchange} from '@urql/devtools';
import type { LexAuthUser } from '$lib/user';
import { isRedirect } from '@sveltejs/kit';

let globalClient: GqlClient | null = null;

function createGqlClient(_gqlEndpoint?: string): Client {
  const url = `/api/graphql`;
  return createClient({
    url,
    exchanges: [
      ...(import.meta.env.DEV ? [devtoolsExchange] : []),
      cacheExchange({
          /* eslint-disable @typescript-eslint/naming-convention */
        keys: {
          'Changeset': () => null,
          'UsersCollectionSegment': () => null,
          'FlexProjectMetadata': (metaData) => metaData.projectId as string,
        },
        updates: {
          Mutation: {
            createProject: (result, args: CreateProjectMutationVariables, cache, _info) => {
              if (args.input.orgId) {
                cache.invalidate({__typename: 'OrgById', id: args.input.orgId}, 'projects');
              }
              if (args.input.id) {
                cache.invalidate({__typename: 'DraftProject', id: args.input.id});
                // Urql cache also stores values for myProjects and myDraftProjects query so we need to invalidate them too
                // Note singular MyProject name for the myProjects query cache, ditto for draft
                cache.invalidate({__typename: 'MyProject', id: args.input.id});
                cache.invalidate({__typename: 'MyDraftProject', id: args.input.id});
              }
              if (result?.createProject?.createProjectResponse?.id) {
                cache.invalidate({__typename: 'DraftProject', id: result?.createProject?.createProjectResponse?.id});
                cache.invalidate({__typename: 'MyProject', id: result?.createProject?.createProjectResponse?.id});
                cache.invalidate({__typename: 'MyDraftProject', id: result?.createProject?.createProjectResponse?.id});
              }
            },
            softDeleteProject: (result, args: SoftDeleteProjectMutationVariables, cache, _info) => {
              cache.invalidate({__typename: 'Project', id: args.input.projectId});
            },
            deleteDraftProject: (result, args: DeleteDraftProjectMutationVariables, cache, _info) => {
              cache.invalidate({__typename: 'DraftProject', id: args.input.draftProjectId});
            },
            deleteUserByAdminOrSelf: (result, args: DeleteUserByAdminOrSelfMutationVariables, cache, _info) => {
              cache.invalidate({__typename: 'User', id: args.input.userId});
            },
            changeUserAccountBySelf: (result, args: ChangeUserAccountBySelfMutationVariables, cache, _info) => {
              cache.invalidate({__typename: 'User', id: args.input.userId});
            },
            bulkAddProjectMembers: (result, args: BulkAddProjectMembersMutationVariables, cache, _info) => {
              if (args.input.projectId) {
                cache.invalidate({__typename: 'Project', id: args.input.projectId});
              }
            },
            bulkAddOrgMembers: (result, args: BulkAddOrgMembersMutationVariables, cache, _info) => {
              cache.invalidate({__typename: 'OrgById', id: args.input.orgId});
            },
            changeOrgMemberRole: (result, args: ChangeOrgMemberRoleMutationVariables, cache, _info) => {
              cache.invalidate({__typename: 'OrgById', id: args.input.orgId});
            },
            setOrgMemberRole: (result, args: AddOrgMemberMutationVariables, cache, _info) => {
              cache.invalidate({__typename: 'OrgById', id: args.input.orgId});
            },
            leaveProject: (result, args: LeaveProjectMutationVariables, cache, _info) => {
              cache.invalidate({__typename: 'Project', id: args.input.projectId});
            },
            addProjectToOrg: (result, args: MutationAddProjectToOrgArgs, cache, _info) => {
              cache.invalidate({__typename: 'Project', id: args.input.projectId});
              cache.invalidate({__typename: 'OrgById', id: args.input.orgId}, 'projects');
            },
            removeProjectFromOrg: (result, args: MutationRemoveProjectFromOrgArgs, cache, _info) => {
              cache.invalidate({__typename: 'Project', id: args.input.projectId});
              cache.invalidate({__typename: 'OrgById', id: args.input.orgId}, 'projects');
            }
          }
        }
        /* eslint-enable @typescript-eslint/naming-convention */
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
type QueryStoreReturnType<Data> = { [K in keyof Data]: Readable<Data[K]> & Pausable };

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
    context: QueryOperationOptions = {}): OperationResultStore<Data, Variables> & Pausable {
    const resultStore = queryStore<Data, Variables>({
      client: this.client,
      query,
      variables,
      context: {fetch, ...context}
    });

    if (browser) {
      return {
        //this is to ensure that the store is pausable
        ...resultStore,
        ...derived(resultStore, (result) => {
          this.throwAnyUnexpectedErrors(result, true);
          // Should we return a result if there's an error? Or should we call set() only if there's no error?
          // I think, YES, we should return the result even if there's an error.
          // Argument for "no": code that is expecting errors to be thrown is likely only capable of handling
          // good results. So, if we give it an error result, we might just trigger more confusing errors.
          // Argument for "yes": returning NO result is just as bad, beacuse we're essentially returning null,
          // which calling code is arguably less prepared to handle than an error result.
          return result;
        })
      };
    } else {
      /**
       * We can't validate and throw errors here, beacuse we'd kill node, but we shouldn't ever need to, because:
       * 1) Only the initial result of the query store will ever be fetched server-side
       * 2) If we want to await the initial result server-side, then we should be using `awaitedQueryStore`, where we CAN safely validate and throw
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

    const keys = Object.keys(results.data ?? {}) as Array<keyof Data>;
    const resultData = {} as QueryStoreReturnType<Data>;
    for (const key of keys) {
      resultData[key] = {
        //this is to ensure that the store is pausable
        ...resultStore,
        ...derived(resultStore, value => {
          const dataValue = value.data ? value.data[key] : undefined;
          return dataValue;
        })
        // we're claiming that the store values are always defined, which contradicts our data.value null check, so it's a lie,
        // but the type of almost every Svelte store is essentially a lie, because they often start as undefined even though they claim to be non-nullable e.g. writable()
        // we could choose to patch that with our: tryMakeNonNullable()
      } as QueryStoreReturnType<Data>[typeof key];
    }

    return resultData ;
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

  private throwAnyUnexpectedErrors<T extends OperationResult<unknown, AnyVariables>>(result: T, delayThrow: boolean = false): void {
    if (!result.error) return;
    const error =
      // Various status codes are handled in the fetch hooks (see hooks.shared.ts).
      // throws there (e.g. SvelteKit redirects and 500's) turn into networkErrors that land here
      result.error?.networkError ??
      // These are errors from urql. urql doesn't throw errors, it just sticks them on the result.
      // An error's stacktrace points to where it was instantiated (i.e. in urql),
      // but it's far more interesting (particularly when debugging how errors affect our app) to know when and where errors are getting thrown, namely HERE.
      // So, we new up our own error to get the more useful stacktrace.
      new AggregateError(result.error.graphQLErrors, result.error.message ?? result.error.cause);

    if (delayThrow && !isRedirect(error)) { // SvelteKit handles Redirects, so we don't want to delay them
      // We can't throw errors here, because errors thrown in wonka/an exchange kill the frontend.
      setTimeout(() => { throw error; });
    } else {
      throw error;
    }
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
