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
  ProjectPageQuery,
} from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';
import { invalidate } from '$app/navigation';

type Project = ProjectPageQuery['projects'][0];
export type ProjectUser = Project['ProjectUsers'][number];

export async function load(event: PageLoadEvent) {
  const client = getClient();
  const projectCode = event.params.project_code;
  const result = await client
    .query(
      graphql(`
				query projectPage($projectCode: String!) {
					projects(where: { code: { _eq: $projectCode } }) {
						id
						name
						code
						description
						type
						lastCommit
						createdDate
						retentionPolicy
						ProjectUsers {
							id
							role
							User {
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
      { projectCode }, { fetch: event.fetch },
    );

  const projectId = result.data?.projects[0]?.id as string;
  event.depends(`project:${projectId}`);
  return {
    project: result.data?.projects[0],
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
      //invalidates the graphql project cache
      { additionalTypenames: ['Projects'] },
    );
  if (!result.error) void invalidate(`project:${input.projectId}`);
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
      //invalidates the graphql project cache
      { additionalTypenames: ['Projects'] },
    );
  if (!result.error) void invalidate(`project:${input.projectId}`);
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
      { input: input },
      //invalidates the graphql project cache
      { additionalTypenames: ['Projects'] },
    );
  if (!result.error) void invalidate(`project:${input.projectId}`);
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
      { input: input },
      //invalidates the graphql project cache
      { additionalTypenames: ['Projects'] },
    );
  if (!result.error) void invalidate(`project:${input.projectId}`);
  return result;
}

export async function _deleteProjectUser(projectId: string, userId: string): Promise<void> {
  const result = await getClient()
    .mutation(
      graphql(`
        mutation deleteProjectUser($input: RemoveProjectMemberInput!) {
          removeProjectMember(input: $input) {
            code
          }
        }
      `),
      { input: { projectId: projectId, userId: userId } },
      // invalidates the cached project so invalidate below will actually reload the project
      { additionalTypenames: ['Projects'] },
    );
  if (!result.error) {
    void invalidate(`project:${projectId}`);
  }
}
