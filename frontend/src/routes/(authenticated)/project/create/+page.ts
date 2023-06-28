import type { $OpResult, CreateProjectInput, CreateProjectMutation } from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

export async function _createProject(input: CreateProjectInput): $OpResult<CreateProjectMutation> {
  const result = await getClient().mutation(
    graphql(`
        mutation createProject($input: CreateProjectInput!) {
            createProject(input: $input) {
                project {
                    id
                }
                errors {
                    ... on DbError {
                        code
                    }
                }
            }

        }
        `),
    { input });
  return result;
}
