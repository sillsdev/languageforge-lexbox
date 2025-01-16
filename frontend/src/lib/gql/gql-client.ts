import {browser} from '$app/environment';
import {tracingExchange, tryCopyTraceContext} from '$lib/otel';
import type {LexAuthUser} from '$lib/user';
import {isRedirect} from '@sveltejs/kit';
import {devtoolsExchange} from '@urql/devtools';
import {cacheExchange} from '@urql/exchange-graphcache';
import {
  type AnyVariables,
  type Client,
  type CombinedError,
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
import type {Readable, Unsubscriber} from 'svelte/store';
import {derived} from 'svelte/store';
import {isObject} from '../util/types';
import {
  type $OpResult,
  type CreateOrgMutation,
  type CreateProjectMutation,
  CreateProjectResult,
  type ExtractErrorTypename,
  type GenericData,
  type GqlInputError,
  isErrorResult,
  LexGqlError,
  type MutationAddProjectsToOrgArgs,
  type MutationAddProjectToOrgArgs,
  type MutationBulkAddOrgMembersArgs,
  type MutationBulkAddProjectMembersArgs,
  type MutationChangeOrgMemberRoleArgs,
  type MutationChangeUserAccountBySelfArgs,
  type MutationCreateGuestUserByAdminArgs,
  type MutationCreateOrganizationArgs,
  type MutationCreateProjectArgs,
  type MutationDeleteDraftProjectArgs,
  type MutationDeleteUserByAdminOrSelfArgs,
  type MutationLeaveOrgArgs,
  type MutationLeaveProjectArgs,
  type MutationRemoveProjectFromOrgArgs,
  type MutationSetOrgMemberRoleArgs,
  type MutationSoftDeleteProjectArgs,
} from './types';

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
          'ProjectWritingSystems': () => null,
          'FLExWsId': (metaData) => metaData.tag as string,
        },
        updates: {
          Mutation: {
            createProject: (result: CreateProjectMutation, args: MutationCreateProjectArgs, cache, _info) => {
              if (args.input.orgId) {
                cache.invalidate({__typename: 'OrgById', id: args.input.orgId}, 'projects');
              }
              const draftCreated = result.createProject.createProjectResponse?.result === CreateProjectResult.Requested;
              const dashboardQuery = draftCreated ? 'myProjects' : 'myDraftProjects';
              const adminDashboardQuery = draftCreated ? 'projects' : 'draftProjects';
              cache.inspectFields('Query')
                .filter(field => field.fieldName === dashboardQuery || field.fieldName === adminDashboardQuery)
                .forEach(field => cache.invalidate('Query', field.fieldKey));
              // Invalidate the project code in case there's a deleted project with the same code in the cache
              // in a perfect world, we might update the cache with a result from the create mutation response,
              // but then we'd probably still need to refetch it, because fields would be missing
              cache.invalidate('Query', 'projectByCode', {code: args.input.code});
            },
            softDeleteProject: (result, args: MutationSoftDeleteProjectArgs, cache, _info) => {
              cache.invalidate({__typename: 'Project', id: args.input.projectId});
            },
            deleteDraftProject: (result, args: MutationDeleteDraftProjectArgs, cache, _info) => {
              cache.invalidate({__typename: 'DraftProject', id: args.input.draftProjectId});
            },
            deleteUserByAdminOrSelf: (result, args: MutationDeleteUserByAdminOrSelfArgs, cache, _info) => {
              cache.invalidate({__typename: 'User', id: args.input.userId});
            },
            changeUserAccountBySelf: (result, args: MutationChangeUserAccountBySelfArgs, cache, _info) => {
              cache.invalidate({__typename: 'User', id: args.input.userId});
            },
            bulkAddProjectMembers: (result, args: MutationBulkAddProjectMembersArgs, cache, _info) => {
              cache.invalidate({__typename: 'Project', id: args.input.projectId});
            },
            createGuestUserByAdmin: (result, args: MutationCreateGuestUserByAdminArgs, cache, _info) => {
              if (args.input.orgId) {
                cache.invalidate({__typename: 'OrgById', id: args.input.orgId});
              }
            },
            createOrganization: (result: CreateOrgMutation, args: MutationCreateOrganizationArgs, cache, _info) => {
              cache.invalidate('Query', 'myOrgs');
            },
            addProjectsToOrg: (result, args: MutationAddProjectsToOrgArgs, cache, _info) => {
              cache.invalidate({__typename: 'OrgById', id: args.input.orgId});
            },
            bulkAddOrgMembers: (result, args: MutationBulkAddOrgMembersArgs, cache, _info) => {
              cache.invalidate({__typename: 'OrgById', id: args.input.orgId});
            },
            changeOrgMemberRole: (result, args: MutationChangeOrgMemberRoleArgs, cache, _info) => {
              cache.invalidate({__typename: 'OrgById', id: args.input.orgId});
            },
            leaveOrg: (result, args: MutationLeaveOrgArgs, cache, _info) => {
              cache.invalidate({__typename: 'OrgById', id: args.input.orgId});
              cache.invalidate('Query', 'myOrgs');
            },
            setOrgMemberRole: (result, args: MutationSetOrgMemberRoleArgs, cache, _info) => {
              cache.invalidate({__typename: 'OrgById', id: args.input.orgId});
            },
            leaveProject: (result, args: MutationLeaveProjectArgs, cache, _info) => {
              cache.invalidate({__typename: 'Project', id: args.input.projectId});
              cache.invalidate('Query', 'myProjects');
            },
            addProjectToOrg: (result, args: MutationAddProjectToOrgArgs, cache, _info) => {
              cache.invalidate({__typename: 'Project', id: args.input.projectId}, 'organizations');
              cache.invalidate({__typename: 'OrgById', id: args.input.orgId}, 'projects');
            },
            removeProjectFromOrg: (result, args: MutationRemoveProjectFromOrgArgs, cache, _info) => {
              cache.invalidate({__typename: 'Project', id: args.input.projectId}, 'organizations');
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

    const error = this.squashErrors(result.error);

    if (delayThrow && !isRedirect(error)) { // SvelteKit handles Redirects, so we don't want to delay them
      // We can't throw errors here, because errors thrown in wonka/an exchange kill the frontend.
      setTimeout(() => { throw error; });
    } else {
      throw error;
    }
  }

  private squashErrors(error: CombinedError): Error {
    // Various status codes are handled in the fetch hooks (see hooks.shared.ts).
    // throws there (e.g. SvelteKit redirects and 500's) turn into networkErrors that land here
    if (error.networkError) return error.networkError;
    // These are errors from urql. urql doesn't throw errors, it just sticks them on the result.
    // An error's stacktrace points to where it was instantiated (i.e. in urql),
    // but it's far more interesting (particularly when debugging how errors affect our app) to know when and where errors are getting thrown, namely HERE.
    // So, we new up our own error to get the more useful stacktrace.
    const squashedError = new AggregateError(error.graphQLErrors, error.message ?? error.cause);
    tryCopyTraceContext(error, squashedError);
    return squashedError;
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
