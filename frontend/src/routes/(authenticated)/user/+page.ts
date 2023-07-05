import type {
  $OpResult,
  ChangeUserAccountDataMutation,
  ChangeUserAccountDataInput,
  DeleteUserByUserMutation,
  DeleteUserByUserInput

} from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';
import { invalidate } from '$app/navigation';
import {refreshJwt} from '$lib/user';

export async function _changeUserAccountData(input: ChangeUserAccountDataInput): $OpResult<ChangeUserAccountDataMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation ChangeUserAccountData($input: ChangeUserAccountDataInput!) {
          changeUserAccountData(input: $input) {
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
    );
  if (!result.error) {
    await refreshJwt();
    await invalidate(`user:${input.userId}`);
  }
  return result;
}
export async function _deleteUserByUser(input: DeleteUserByUserInput): $OpResult<DeleteUserByUserMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation DeleteUserByUser($input: DeleteUserByUserInput!) {
          deleteUserByUser(input: $input) {
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
