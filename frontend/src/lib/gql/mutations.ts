import type { $OpResult, DeleteUserByAdminOrSelfInput, DeleteUserByAdminOrSelfMutation, SoftDeleteProjectMutation, DeleteDraftProjectMutation } from './types';

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

export async function _deleteDraftProject(draftProjectId: string): $OpResult<DeleteDraftProjectMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation DeleteDraftProject($input: DeleteDraftProjectInput!) {
          deleteDraftProject(input: $input) {
            draftProject {
              id,
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
        input: { draftProjectId }
      },
      { additionalTypenames: ['Projects'] },
    );

  return result;
}
