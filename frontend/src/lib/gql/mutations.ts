import type { $OpResult, DeleteUserByAdminOrSelfInput, DeleteUserByAdminOrSelfMutation } from './types';

import { getClient } from './gql-client';
import { graphql } from './generated';

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
