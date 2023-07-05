import type {
  $OpResult,
  ChangeUserAccountByAdminInput,
  ChangeUserAccountByAdminMutation,
  DeleteUserByAdminInput,
  DeleteUserByAdminMutation,
} from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';
import { isAdmin, type LexAuthUser } from '$lib/user';
import { redirect } from '@sveltejs/kit';

function requireAdmin(user: LexAuthUser | null): void {
  if (!isAdmin(user)) {
    throw redirect(307, '/');
  }
}

export async function load(event: PageLoadEvent) {
  const parentData = await event.parent();
  requireAdmin(parentData.user);

  const client = getClient();
  //language=GraphQL
  const results = await client.query(graphql(`
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
    `), {}, { fetch: event.fetch });

  return {
    projects: results.data?.projects ?? [],
    users: results.data?.users ?? []
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
            }
            errors {
              ... on Error {
                message
              }
            }
          }
        }
      `),
      { input: input },
      //invalidates the graphql user cache, but who knows
      { additionalTypenames: ['Users'] },
    )
    return result;
}
export async function _deleteUserByAdmin(input: DeleteUserByAdminInput): $OpResult<DeleteUserByAdminMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation DeleteUserByAdmin($input: DeleteUserByAdminInput!) {
          deleteUserByAdmin(input: $input) {
            user {
              id
            }
            errors {
              ... on Error {
                message
              }
            }
          }
        }
      `),
      { input: input },
      //invalidates the graphql user cache, but who knows
      { additionalTypenames: ['Users'] },
    );
  return result;
}
