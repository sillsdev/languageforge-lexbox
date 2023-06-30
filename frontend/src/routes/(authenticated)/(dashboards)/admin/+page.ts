import { getClient, graphql } from '$lib/gql';
import type { PageLoadEvent } from './$types';
import type {
  $OpResult,
  ChangeUserAccountByAdminInput,
  ChangeUserAccountByAdminMutation,
  DeleteUserByAdminInput,
  DeleteUserByAdminMutation
} from '$lib/gql/types';
export async function load(event: PageLoadEvent) {
  const client = getClient();
  //language=GraphQL
  const results = await client.query(graphql(`
        query loadAdminDashboard {
            projects(orderBy: [
                {lastCommit: ASC_NULLS_FIRST},
                {name: ASC}
            ]) {
                code
                id
                name
                lastCommit
                type
                projectUsersAggregate {
                    aggregate {
                        count
                    }
                }
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
export async function _deleteUserByAdmin(input: DeleteUserByAdminInput): $OpResult<DeleteUserAccountByAdminMutation> {
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
