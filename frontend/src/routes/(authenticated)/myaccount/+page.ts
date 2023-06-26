import type {
  $OpResult,
  ChangeUserAccountDataInput,
  ChangeUserAccountDataMutation,
} from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

import { invalidate } from '$app/navigation';

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
    )
    .toPromise();
  if (!result.error) void invalidate(`user:${input.userId}`);
  return result;
}
