import type {
  $OpResult,
  AddProjectMemberInput,
  AddProjectMemberMutation,
  ChangeProjectDescriptionInput,
  ChangeProjectDescriptionMutation,
  ChangeProjectMemberRoleInput,
  ChangeProjectMemberRoleMutation,
  ChangeProjectNameInput,
  ChangeProjectNameMutation,
  DeleteProjectUserMutation,
  ProjectPageQuery,
} from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';
import { get } from 'svelte/store';

type Project = NonNullable<ProjectPageQuery['projectByCode']>;
export type ProjectUser = Project['users'][number];

export async function load(event: PageLoadEvent) {
  const client = getClient();
  const projectCode = event.params.project_code;
  const result = await client
    .queryStore(event.fetch,
      graphql(`
				query projectPage($projectCode: String!) {
					projectByCode(code: $projectCode) {
						id
						name
						code
						description
						type
            migrationStatus
						lastCommit
						createdDate
						retentionPolicy
						users {
							id
							role
							user {
								id
								name
							}
						}
						changesets {
							node
							parents
							date
							user
							desc
						}
					}
				}
			`),
      { projectCode }
    );

  const projectId = get(result.projectByCode)?.id as string;
  event.depends(`project:${projectId}`);
  return {
    project: result.projectByCode,
    code: projectCode,
  };
}

export async function _addProjectMember(input: AddProjectMemberInput): $OpResult<AddProjectMemberMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation AddProjectMember($input: AddProjectMemberInput!) {
          addProjectMember(input: $input) {
            project {
              id
              users {
                id
                role
                user {
                  id
                  name
                }
              }
            }
            errors {
              __typename
            }
          }
        }
      `),
      { input: input }
  );
  return result;
}

export async function _changeProjectMemberRole(input: ChangeProjectMemberRoleInput): $OpResult<ChangeProjectMemberRoleMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation ChangeProjectMemberRole($input: ChangeProjectMemberRoleInput!) {
          changeProjectMemberRole(input: $input) {
            projectUsers {
              id
              role
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
    );
  return result;
}

export async function _changeProjectName(input: ChangeProjectNameInput): $OpResult<ChangeProjectNameMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation ChangeProjectName($input: ChangeProjectNameInput!) {
          changeProjectName(input: $input) {
            project {
              id
              name
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
    );
  return result;
}

export async function _changeProjectDescription(input: ChangeProjectDescriptionInput): $OpResult<ChangeProjectDescriptionMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation ChangeProjectDescription($input: ChangeProjectDescriptionInput!) {
          changeProjectDescription(input: $input) {
            project {
              id
              description
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
    );
  return result;
}

export async function _deleteProjectUser(projectId: string, userId: string): $OpResult<DeleteProjectUserMutation> {
  const result = await getClient()
    .mutation(
      graphql(`
        mutation deleteProjectUser($input: RemoveProjectMemberInput!) {
          removeProjectMember(input: $input) {
            code
          }
        }
      `),
      { input: { projectId: projectId, userId: userId } }
    );
  return result;
}
