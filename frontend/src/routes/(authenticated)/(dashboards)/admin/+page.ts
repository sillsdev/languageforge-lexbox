import { getClient, graphql } from '$lib/gql';
import type { PageLoadEvent } from './$types';
import type {
  $OpResult,
  ChangeUserAccountByAdminInput,
  ChangeUserAccountByAdminMutation,
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
    `), {}, { fetch: event.fetch }).toPromise();
  if (results.error) throw new Error(results.error.message);
  return {
    projects: results.data?.projects ?? [],
    users: results.data?.users ?? []
  }
}
export async function _changeUserAccountData(input: ChangeUserAccountByAdminInput): $OpResult<ChangeUserAccountByAdminMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation ChangeUserAccountByAdmin($input: ChangeUserAccountByAdminInput!) {
          changeUserAccountByAdmin(input: $input) {
            user {
              id
              name
              username
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
      { input: input },
      //invalidates the graphql user cache, but who knows
      { additionalTypenames: ['Users'] },
    )
    .toPromise();
  return result;
}
