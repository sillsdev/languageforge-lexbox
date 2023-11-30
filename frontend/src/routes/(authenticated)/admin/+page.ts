import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';
import { isAdmin, type LexAuthUser } from '$lib/user';
import { redirect } from '@sveltejs/kit';
import {getBoolSearchParam, getSearchParam} from '$lib/util/query-params';
import type {
  $OpResult,
  ChangeUserAccountByAdminInput,
  ChangeUserAccountByAdminMutation,
  ProjectFilterInput,
  UserFilterInput,
  ProjectType,
  ProjectMigrationStatus
} from '$lib/gql/types';
import type {LoadAdminDashboardProjectsQuery, LoadAdminDashboardUsersQuery} from '$lib/gql/types';

export const _FILTER_PAGE_SIZE = 100;

export type AdminSearchParams = {
  userSearch: string,
  showDeletedProjects: boolean,
  projectType: ProjectType | undefined,
  userEmail: string | undefined,
  projectSearch: string,
  migrationStatus: ProjectMigrationStatus | 'UNMIGRATED' | undefined,
};

export type Project = LoadAdminDashboardProjectsQuery['projects'][number];
export type User = NonNullable<NonNullable<LoadAdminDashboardUsersQuery['users']>['items']>[number];

export async function load(event: PageLoadEvent) {
  const parentData = await event.parent();
  requireAdmin(parentData.user);

  const withDeletedProjects = getBoolSearchParam<AdminSearchParams>('showDeletedProjects', event.url.searchParams);
  const userSearch = getSearchParam<AdminSearchParams>('userSearch', event.url.searchParams) ?? '';
  const userEmail = getSearchParam<AdminSearchParams>('userEmail', event.url.searchParams);

  const client = getClient();

  const projectFilter: ProjectFilterInput = {
    ...(userEmail ? { users: { some: { user: { email: { icontains: userEmail } } } } } : {})
  };

  //language=GraphQL
  const projectResultsPromise = client.awaitedQueryStore(event.fetch, graphql(`
        query loadAdminDashboardProjects($withDeletedProjects: Boolean, $filter: ProjectFilterInput) {
            projects(
              where: $filter,
              orderBy: [
                {lastCommit: ASC},
                {name: ASC}
            ], withDeleted: $withDeletedProjects) {
              code
              id
              name
              lastCommit
                migrationStatus
              type
              deletedDate
              userCount
            }
        }
    `), { withDeletedProjects, filter: projectFilter });

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
                projects {
                    id
                    projectId
                }
              }
            }
        }
    `), { filter: userFilter, take: _FILTER_PAGE_SIZE });

  const [projectResults, userResults] = await Promise.all([projectResultsPromise, userResultsPromise]);

  return {
    ...projectResults,
    ...userResults,
  }
}

const guidRegex = /^[0-9a-f]{8}-?[0-9a-f]{4}-?[0-5][0-9a-f]{3}-?[089ab][0-9a-f]{3}-?[0-9a-f]{12}$/i;

function isGuid(val: string): boolean {
  // only match strings of the exact length of a GUID, with or without dashes
  return (val.length == 32 || val.length == 36) && guidRegex.test(val);
}

function requireAdmin(user: LexAuthUser | null): void {
  if (!isAdmin(user)) {
    throw redirect(307, '/');
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
