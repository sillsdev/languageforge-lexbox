import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';
import { isAdmin, type LexAuthUser } from '$lib/user';
import { redirect } from '@sveltejs/kit';
import { getBoolSearchParam, getSearchParam } from '$lib/util/query-params';
import type { $OpResult, ChangeUserAccountByAdminInput, ChangeUserAccountByAdminMutation, ProjectFilterInput, ProjectType } from '$lib/gql/types';
import type { LoadAdminDashboardProjectsQuery, LoadAdminDashboardUsersQuery } from '$lib/gql/types';

export const _FILTER_PAGE_SIZE = 100;

export type AdminSearchParams = {
  userSearch: string,
  showDeletedProjects: boolean,
  projectType: ProjectType | undefined,
  userEmail: string | undefined,
  projectSearch: string,
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
              type
              deletedDate
              userCount
            }
        }
    `), { withDeletedProjects, filter: projectFilter });

  const userResultsPromise = client.awaitedQueryStore(event.fetch, graphql(`
        query loadAdminDashboardUsers($userSearch: String, $take: Int!) {
            users(
              where: {or: [
                {name: {icontains: $userSearch}},
                {email: {icontains: $userSearch}}
            ]}, orderBy: {name: ASC}, take: $take) {
              totalCount
              items {
                id
                name
                email
                isAdmin
                createdDate
                emailVerified
                projects {
                    id
                    projectId
                }
              }
            }
        }
    `), { userSearch, take: _FILTER_PAGE_SIZE });

  const [projectResults, userResults] = await Promise.all([projectResultsPromise, userResultsPromise]);

  return {
    ...projectResults,
    ...userResults,
  }
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
