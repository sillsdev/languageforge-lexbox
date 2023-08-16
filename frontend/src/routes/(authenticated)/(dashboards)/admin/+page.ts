import type {
  $OpResult,
  ChangeUserAccountByAdminInput,
  ChangeUserAccountByAdminMutation,
} from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';
import { isAdmin, type LexAuthUser } from '$lib/user';
import { redirect } from '@sveltejs/kit';
import { getBoolSearchParam } from '$lib/util/urls';

export type AdminSearchParams = {
  showDeletedProjects: boolean,
};

export async function load(event: PageLoadEvent) {
  const parentData = await event.parent();
  requireAdmin(parentData.user);

  const withDeletedProjects = getBoolSearchParam<AdminSearchParams>('showDeletedProjects', event.url.searchParams);

  const client = getClient();
  //language=GraphQL
  const results = await client.queryStore(event.fetch, graphql(`
        query loadAdminDashboard($withDeletedProjects: Boolean) {
            projects(orderBy: [
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
            users(orderBy: {name: ASC}) {
                id
                name
                email
                isAdmin
                createdDate
                emailVerified
            }
        }
    `), { withDeletedProjects });

  return {
    ...results
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
