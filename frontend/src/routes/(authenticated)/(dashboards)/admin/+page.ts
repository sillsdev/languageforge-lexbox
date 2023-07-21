import type {
  $OpResult,
  ChangeUserAccountByAdminMutation,
  ChangeUserAccountDataInput,
  DeleteUserByAdminOrSelfInput,
  DeleteUserByAdminOrSelfMutation,
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


export async function _changeUserAccountByAdmin(input: ChangeUserAccountDataInput): $OpResult<ChangeUserAccountByAdminMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation ChangeUserAccountByAdmin($input: ChangeUserAccountDataInput!) {
          changeUserAccountByAdmin(input: $input) {
            user {
              id
              name
              email
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
export async function _deleteUserByAdminOrSelf(input: DeleteUserByAdminOrSelfInput): $OpResult<DeleteUserByAdminOrSelfMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation DeleteUserByAdminOrSelf($input: DeleteUserByAdminOrSelfInput!) {
          deleteUserByAdminOrSelf(input: $input) {
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
