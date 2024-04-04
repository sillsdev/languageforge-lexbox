import type { LoadAdminUsersTypeaheadQuery, UserFilterInput } from './types';

import { getClient } from './gql-client';
import { graphql } from './generated';
import { DEFAULT_PAGE_SIZE } from '$lib/components/Paging';

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

export async function _typeaheadSearch(userSearch: string, limit = DEFAULT_PAGE_SIZE): Promise<UserTypeaheadResult> {
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
  `), { filter: userFilter(userSearch), take: limit+1 });
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
