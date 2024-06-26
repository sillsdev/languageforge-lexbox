import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';
import { type LexAuthUser } from '$lib/user';
import { redirect } from '@sveltejs/kit';
import {getBoolSearchParam, getSearchParam} from '$lib/util/query-params';
import { isGuid } from '$lib/util/guid';
import type {
  $OpResult,
  ChangeUserAccountByAdminInput,
  ChangeUserAccountByAdminMutation,
  CreateGuestUserByAdminInput,
  CreateGuestUserByAdminMutation,
  DraftProjectFilterInput,
  ProjectFilterInput,
  SetUserLockedInput,
  SetUserLockedMutation,
  UserFilterInput,
} from '$lib/gql/types';
import type {LoadAdminDashboardProjectsQuery, LoadAdminDashboardUsersQuery} from '$lib/gql/types';
import type { ProjectFilters } from '$lib/components/Projects';
import { DEFAULT_PAGE_SIZE } from '$lib/components/Paging';
import type { AdminTabId } from './AdminTabs.svelte';
import { derived, readable } from 'svelte/store';

// eslint-disable-next-line @typescript-eslint/no-redundant-type-constituents -- false positive?
export type AdminSearchParams = ProjectFilters & {
  userSearch: string
  tab: AdminTabId
  usersICreated: boolean
};

export type Project = NonNullable<LoadAdminDashboardProjectsQuery['projects']>[number];
export type DraftProject = NonNullable<LoadAdminDashboardProjectsQuery['draftProjects']>[number];
export type User = NonNullable<NonNullable<LoadAdminDashboardUsersQuery['users']>['items']>[number];

export async function load(event: PageLoadEvent) {
  const parentData = await event.parent();
  requireAdmin(parentData.user);

  const withDeletedProjects = getBoolSearchParam<AdminSearchParams>('showDeletedProjects', event.url.searchParams);
  const userSearch = getSearchParam<AdminSearchParams>('userSearch', event.url.searchParams) ?? '';
  const memberSearch = getSearchParam<AdminSearchParams>('memberSearch', event.url.searchParams);

  const client = getClient();

  const projectFilter: ProjectFilterInput = {
    ...(memberSearch ? { users: { some: { user: { or: [ { email: { eq: memberSearch } }, { username: { eq: memberSearch } } ] } } } }: {})
  };
  const draftFilter: DraftProjectFilterInput = {
    ...(memberSearch ? { projectManager: { or: [ { email: { eq: memberSearch } }, { username: { eq: memberSearch } } ] } } : {})
  };



  //language=GraphQL
  const projectResultsPromise = client.awaitedQueryStore(event.fetch, graphql(`
        query loadAdminDashboardProjects($withDeletedProjects: Boolean!, $projectFilter: ProjectFilterInput, $draftFilter: DraftProjectFilterInput) {
            projects(
              where: $projectFilter,
              orderBy: [
                {createdDate: DESC},
                {name: ASC}
            ], withDeleted: $withDeletedProjects) {
              code
              id
              name
              lastCommit
              type
              isConfidential
              deletedDate
              createdDate
              userCount
            }
            draftProjects(
              where: $draftFilter
            ) {
              code
              id
              name
              type
              createdDate
              description
              retentionPolicy
              isConfidential
              projectManagerId
            }
        }
    `), { withDeletedProjects, projectFilter, draftFilter });

  const userFilter: UserFilterInput = isGuid(userSearch) ? {id: {eq: userSearch}} : {
    or: [
      {name: {icontains: userSearch}},
      {email: {icontains: userSearch}},
      {username: {icontains: userSearch}}
    ]
  };
  const userResultsPromise = client.awaitedQueryStore(event.fetch, graphql(`
        query loadAdminDashboardUsers($filter: UserFilterInput, $take: Int!) {
            users(
              where: $filter, orderBy: {name: ASC}, take: $take) {
              totalCount
              items {
                id
                name
                email
                username
                isAdmin
                emailVerified
                createdDate
                locked
                localizationCode
                updatedDate
                lastActive
                canCreateProjects
                createdById
                createdBy {
                  id
                  name
                }
                projects {
                    id
                    projectId
                }
              }
            }
        }
    `), { filter: userFilter, take: DEFAULT_PAGE_SIZE });

  const [projectResults, userResults] = await Promise.all([projectResultsPromise, userResultsPromise]);

  return {
    projects: derived(projectResults.projects ?? readable([]), (projects) => projects ?? []),
    draftProjects: derived(projectResults.draftProjects ?? readable([]), (draftProjects) => draftProjects ?? []),
    ...userResults,
  }
}

function requireAdmin(user: LexAuthUser | null): void {
  if (!user?.isAdmin) {
    redirect(307, '/');
  }
}

export async function _changeUserAccountByAdmin(input: ChangeUserAccountByAdminInput): $OpResult<ChangeUserAccountByAdminMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation ChangeUserAccountByAdmin($input: ChangeUserAccountByAdminInput!) {
          changeUserAccountByAdmin(input: $input) {
            user {
              id
              name
              email
              isAdmin
            }
            errors {
                __typename
              ... on Error {
                message
              }
            }
          }
        }
      `),
      { input: input }
    )
  return result;
}

export async function _createGuestUserByAdmin(input: CreateGuestUserByAdminInput): $OpResult<CreateGuestUserByAdminMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation CreateGuestUserByAdmin($input: CreateGuestUserByAdminInput!) {
          createGuestUserByAdmin(input: $input) {
            lexAuthUser {
              id
              name
              email
              username
              role
              isAdmin
              locked
              emailVerificationRequired
              canCreateProjects
              createdByAdmin
              locale
              projects {
                projectId
                role
              }
            }
            errors {
                __typename
              ... on Error {
                message
              }
            }
          }
        }
      `),
      { input: input }
    )
  return result;
}

export async function _setUserLocked(input: SetUserLockedInput): $OpResult<SetUserLockedMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation SetUserLocked($input: SetUserLockedInput!) {
          setUserLocked(input: $input) {
            user {
              id
              locked
            }
            errors {
                __typename
              ... on Error {
                message
              }
            }
          }
        }
      `),
      { input: input }
    )
  return result;
}
