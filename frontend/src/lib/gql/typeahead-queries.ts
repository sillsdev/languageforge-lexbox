import type { LoadAdminUsersTypeaheadQuery, LoadOrgMembersTypeaheadQuery, UserFilterInput } from './types';

import { getClient } from './gql-client';
import { graphql } from './generated';

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
        }
      }
    }
  `), { filter: userFilter(userSearch), take: limit });
  // NOTE: If more properties are needed, copy from loadAdminDashboardUsers to save time

  const users = result.then(users => {
    // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
    const count = users.data?.users?.totalCount ?? 0;
    if (0 < count && count <= limit) {
      // eslint-disable-next-line @typescript-eslint/no-unsafe-return
      return users.data?.users?.items ?? [];
    } else {
      return [];
    }
  });

  return users;
}

export type UsersInMyOrgTypeaheadResult = NonNullable<NonNullable<LoadOrgMembersTypeaheadQuery['usersInMyOrg']>['items']>;
export type SingleUserInMyOrgTypeaheadResult = UsersInMyOrgTypeaheadResult[number];

export async function _orgMemberTypeaheadSearch(orgMemberSearch: string, limit = 10): Promise<UsersInMyOrgTypeaheadResult> {
  if (!orgMemberSearch) return Promise.resolve([]);
  const client = getClient();
  const result = client.query(graphql(`
    query loadOrgMembersTypeahead($filter: UserFilterInput, $take: Int!) {
      usersInMyOrg(where: $filter, orderBy: {name: ASC}, take: $take) {
        totalCount
        items {
          id
          name
        }
      }
    }
  `), { filter: userFilter(orgMemberSearch), take: limit });

  const users = result.then(members => {
    // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
    const count = members.data?.usersInMyOrg?.totalCount ?? 0;
    if (0 < count && count <= limit) {
      // eslint-disable-next-line @typescript-eslint/no-unsafe-return
      return members.data?.usersInMyOrg?.items ?? [];
    } else {
      return [];
    }
  });

  return users;
}
