import type {
  $OpResult,
  ChangeUserAccountByAdminInput,
  ChangeUserAccountByAdminMutation,
} from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';
import { isAdmin, type LexAuthUser } from '$lib/user';
import { redirect } from '@sveltejs/kit';

export async function load(event: PageLoadEvent) {
  const parentData = await event.parent();
  requireAdmin(parentData.user);

  const client = getClient();
  //language=GraphQL
  const results = await client.queryStore(event.fetch, graphql(`
        query loadAdminDashboard {
            projects(orderBy: [
                {lastCommit: ASC},
                {name: ASC}
            ]) {
                code
                id
                name
                lastCommit
                type
            }
            users(orderBy: {name: ASC}) {
                id
                name
                email
                isAdmin
                createdDate
            }
        }
    `), {});

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
