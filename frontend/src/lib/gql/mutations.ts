import type { $OpResult, DeleteUserByAdminOrSelfInput, DeleteUserByAdminOrSelfMutation, SoftDeleteProjectMutation } from './types';

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

export async function _deleteProject(projectId: string): $OpResult<SoftDeleteProjectMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation SoftDeleteProject($input: SoftDeleteProjectInput!) {
          softDeleteProject(input: $input) {
            project {
              id,
              deletedDate
            }
            errors {
              ... on Error {
                message
              }
            }
          }
        }
      `),
      {
        input: { projectId }
      },
      { additionalTypenames: ['Projects'] },
    );

  return result;
}
