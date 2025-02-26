import type {LoadAdminUsersTypeaheadQuery, LoadUsersICanSeeTypeaheadQuery, UserFilterInput} from './types';

import {getClient} from './gql-client';
import {graphql} from './generated';

export function userFilter(userSearch: string): UserFilterInput {
  return {
    or: [
      {name: {icontains: userSearch}},
      {email: {icontains: userSearch}},
      {username: {icontains: userSearch}}
    ]
  };
}

export type UserTypeaheadResult = NonNullable<NonNullable<LoadAdminUsersTypeaheadQuery['users']>['items']>;
export type SingleUserTypeaheadResult = UserTypeaheadResult[number];

export async function _userTypeaheadSearch(userSearch: string, limit = 10): Promise<UserTypeaheadResult> {
  if (!userSearch) return Promise.resolve([]);
  const client = getClient();
  const result = client.query(graphql(`
    query loadAdminUsersTypeahead($filter: UserFilterInput, $take: Int!) {
      users(
        where: $filter, orderBy: {name: ASC}, take: $take) {
        totalCount
        items {
          id
          name
          email
          username
          projects {
            id
            role
            project {
              id
              code
              name
            }
          }
        }
      }
    }
  `), { filter: userFilter(userSearch), take: limit });
  // NOTE: If more properties are needed, copy from loadAdminDashboardUsers to save time

  const users = result.then(users => {
    const count = users.data?.users?.totalCount ?? 0;
    if (0 < count && count <= limit) {
      return users.data?.users?.items ?? [];
    } else {
      return [];
    }
  });

  return users;
}

export type UsersICanSeeTypeaheadResult = NonNullable<NonNullable<LoadUsersICanSeeTypeaheadQuery['usersICanSee']>['items']>;
export type SingleUserICanSeeTypeaheadResult = UsersICanSeeTypeaheadResult[number];

export async function _usersTypeaheadSearch(orgMemberSearch: string, limit = 10): Promise<UsersICanSeeTypeaheadResult> {
  if (!orgMemberSearch) return [];
  const client = getClient();
  const users = await client.query(graphql(`
    query loadUsersICanSeeTypeahead($filter: UserFilterInput, $take: Int!) {
      usersICanSee(where: $filter, orderBy: {name: ASC}, take: $take) {
        totalCount
        items {
          id
          name
        }
      }
    }
  `), { filter: userFilter(orgMemberSearch), take: limit });

  return users.data?.usersICanSee?.items ?? [];
}
