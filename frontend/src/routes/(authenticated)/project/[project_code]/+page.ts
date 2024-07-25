import type {
  $OpResult,
  AddProjectMemberInput,
  AddProjectMemberMutation,
  AddProjectToOrgInput,
  AddProjectToOrgMutation,
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
  Organization,
  ProjectPageQuery,
  RemoveProjectFromOrgMutation,
  SetProjectConfidentialityInput,
  SetProjectConfidentialityMutation,
  SetRetentionPolicyInput,
  SetRetentionPolicyMutation,
  UpdateLangProjectIdMutation,
  UpdateProjectLanguageListMutation,
  UpdateProjectLexEntryCountMutation,
} from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';
import { derived } from 'svelte/store';
import { error } from '@sveltejs/kit';
import { tryMakeNonNullable } from '$lib/util/store';

export type Project = NonNullable<ProjectPageQuery['projectByCode']>;
export type ProjectUser = NonNullable<Project['users']>[number];
export type User = ProjectUser['user'];
export type Org = Pick<Organization, 'id' | 'name'>;

export async function load(event: PageLoadEvent) {
  const client = getClient();
  const user = (await event.parent()).user;
  const projectCode = event.params.project_code;
  const projectId = event.url.searchParams.get('id') ?? '';
  //projectId is not required, so if it's not there we assume the user is a member, if we're wrong there will be an error
  const userIsMember = projectId === '' ? true : (user.isAdmin || user.projects.some(p => p.projectId === projectId));
  const projectResult = await client
    .awaitedQueryStore(event.fetch,
      graphql(`
				query projectPage($projectCode: String!, $userIsAdmin: Boolean!, $userIsMember: Boolean!) {
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
						organizations {
							id
						}
            ... on Project @include(if: $userIsMember) {
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
                    createdBy {
                      id
                      name
                    }
                  }
                }
              }
            }
            flexProjectMetadata {
              lexEntryCount
              writingSystems {
                vernacularWss {
                  tag
                  isActive
                  isDefault
                }
                analysisWss {
                  tag
                  isActive
                  isDefault
                }
              }
            }
            organizations {
              id
              name
            }
					}
				}
			`),
      { projectCode, userIsAdmin: user.isAdmin, userIsMember }
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

export async function _getOrgs(userIsAdmin: boolean): Promise<Org[]> {
  const client = getClient();
  if (userIsAdmin) {
    const orgsResult = await client.query(graphql(`
          query loadOrgs {
              orgs {
                  id
                  name
              }
          }
      `), {}, {});
    return orgsResult.data?.orgs ?? [];
  } else {
    const myOrgsResult = await client.query(graphql(`
          query loadMyOrgs {
              myOrgs {
                  id
                  name
              }
          }
        `), {}, {});
    return myOrgsResult.data?.myOrgs ?? [];
  }
}

export async function _addProjectToOrg(input: AddProjectToOrgInput): $OpResult<AddProjectToOrgMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation AddProjectToOrg($input: AddProjectToOrgInput!) {
          addProjectToOrg(input: $input) {
            organization {
              id
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
      { input: input }
    );
  return result;
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

export async function _updateProjectLexEntryCount(code: string): $OpResult<UpdateProjectLexEntryCountMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation UpdateProjectLexEntryCount($input: UpdateProjectLexEntryCountInput!) {
          updateProjectLexEntryCount(input: $input) {
            project {
              id
              flexProjectMetadata {
                lexEntryCount
              }
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
      { input: { code } },
    );
  return result;
}

export async function _updateLangProjectId(code: string): $OpResult<UpdateLangProjectIdMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation UpdateLangProjectId($input: UpdateLangProjectIdInput!) {
          updateLangProjectId(input: $input) {
            project {
              id
              flexProjectMetadata {
                langProjectId
              }
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
      { input: { code } },
    );
  return result;
}

export async function _updateProjectLanguageList(code: string): $OpResult<UpdateProjectLanguageListMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation UpdateProjectLanguageList($input: UpdateProjectLanguageListInput!) {
          updateProjectLanguageList(input: $input) {
            project {
              id
              flexProjectMetadata {
                writingSystems {
                  analysisWss {
                    tag
                    isActive
                    isDefault
                  }
                  vernacularWss {
                    tag
                    isActive
                    isDefault
                  }
                }
              }
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
      { input: { code } },
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

export async function _removeProjectFromOrg(projectId: string, orgId: string): $OpResult<RemoveProjectFromOrgMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation RemoveProjectFromOrg($input: RemoveProjectFromOrgInput!) {
          removeProjectFromOrg(input: $input) {
            organization {
              id
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
      { input: { projectId: projectId, orgId: orgId } }
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

export async function _setRetentionPolicy(input: SetRetentionPolicyInput): $OpResult<SetRetentionPolicyMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation SetRetentionPolicy($input: SetRetentionPolicyInput!) {
          setRetentionPolicy(input: $input) {
            project {
              id
              retentionPolicy
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
