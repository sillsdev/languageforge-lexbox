import type {
  $OpResult,
  AddProjectMemberInput,
  AddProjectMemberMutation,
  BulkAddProjectMembersInput,
  BulkAddProjectMembersMutation,
  ChangeProjectDescriptionInput,
  ChangeProjectDescriptionMutation,
  ChangeProjectMemberRoleInput,
  ChangeProjectMemberRoleMutation,
  ChangeProjectNameInput,
  ChangeProjectNameMutation,
  DeleteProjectUserMutation,
  LeaveProjectMutation,
  ProjectPageQuery,
  SetProjectConfidentialityInput,
  SetProjectConfidentialityMutation,
} from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';
import { derived } from 'svelte/store';
import { error } from '@sveltejs/kit';
import { tryMakeNonNullable } from '$lib/util/store';

export type Project = NonNullable<ProjectPageQuery['projectByCode']>;
export type ProjectUser = Project['users'][number];

export async function load(event: PageLoadEvent) {
  const client = getClient();
  const userIsAdmin = (await event.parent()).user.isAdmin;
  const projectCode = event.params.project_code;
  const projectResult = await client
    .awaitedQueryStore(event.fetch,
      graphql(`
				query projectPage($projectCode: String!, $userIsAdmin: Boolean!) {
					projectByCode(code: $projectCode) {
						id
						name
						code
						description
						type
            resetStatus
						lastCommit
						createdDate
						retentionPolicy
						isConfidential
            isLanguageForgeProject
						users {
							id
							role
							user {
								id
								name
                ... on User @include(if: $userIsAdmin) {
                  locked
                  username
                  createdDate
                  updatedDate
                  email
                  localizationCode
                  lastActive
                  canCreateProjects
                  isAdmin
                  emailVerified
                }
							}
						}
						flexProjectMetadata {
							lexEntryCount
						}
					}
				}
			`),
      { projectCode, userIsAdmin }
    );
  const changesetResultStore = client
    .queryStore(event.fetch,
      graphql(`
        query projectChangesets($projectCode: String!) {
          projectByCode(code: $projectCode) {
            id
            code
            changesets {
              node
              rev
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

  const nonNullableProject = tryMakeNonNullable(projectResult.projectByCode);
  if (!nonNullableProject) {
    error(404);
  }

  event.depends(`project:${projectCode}`);

  return {
    project: nonNullableProject,
    changesets: {
      //this is to ensure that the store is pausable
      ...changesetResultStore,
      ...derived(changesetResultStore, result => ({
        fetching: result.fetching,
        changesets: result.data?.projectByCode?.changesets ?? [],
      })),
    },
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
              ... on Error {
                message
              }
              ... on InvalidEmailError {
                address
              }
            }
          }
        }
      `),
      { input: input }
    );
  return result;
}

// public record BulkAddProjectMembersInput(Guid ProjectId, string[] Usernames, ProjectRole Role, string PasswordHash);

export async function _bulkAddProjectMembers(input: BulkAddProjectMembersInput): $OpResult<BulkAddProjectMembersMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation BulkAddProjectMembers($input: BulkAddProjectMembersInput!) {
          bulkAddProjectMembers(input: $input) {
            bulkAddProjectMembersResult {
              addedMembers {
                username
                role
              }
              createdMembers {
                username
                role
              }
              existingMembers {
                username
                role
              }
              invitedMembers {
                username
                role
              }
            }
            errors {
              __typename
              ... on InvalidEmailError {
                message
                address
              }
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
              __typename
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

export async function _setProjectConfidentiality(input: SetProjectConfidentialityInput): $OpResult<SetProjectConfidentialityMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation SetProjectConfidentiality($input: SetProjectConfidentialityInput!) {
          setProjectConfidentiality(input: $input) {
            project {
              id
              isConfidential
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
          }
        }
      `),
      { input: { projectId: projectId, userId: userId } }
    );
  return result;
}

export async function _refreshProjectRepoInfo(projectCode: string): Promise<void> {
  const result = await getClient().query(graphql(`
        query refreshProjectStatus($projectCode: String!) {
            projectByCode(code: $projectCode) {
                id
                resetStatus
                lastCommit
                changesets {
                  node
                  parents
                  date
                  user
                  desc
                }
            }
        }
    `), { projectCode }, { requestPolicy: 'network-only' });

  if (result.error) {
    // this should be meaningless, but just in case and it makes the linter happy
    throw result.error;
  }
}


export async function _leaveProject(projectId: string): $OpResult<LeaveProjectMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
      mutation LeaveProject($input: LeaveProjectInput!) {
        leaveProject(input: $input) {
          project {
            id
          }
          errors {
            __typename
          }
        }
      }
    `),
      { input: { projectId } },
      //disable invalidate otherwise the page will reload
      //and the user will be shown that they don't have permission for this project
      { fetchOptions: { lexboxResponseHandlingConfig: { invalidateUserOnJwtRefresh: false } } }
    );

  return result;
}
