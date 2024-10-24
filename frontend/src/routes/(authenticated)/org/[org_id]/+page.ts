import type {
  $OpResult,
  AddOrgMemberMutation,
  AddProjectsToOrgMutation,
  BulkAddOrgMembersMutation,
  ChangeOrgMemberRoleMutation,
  ChangeOrgNameInput,
  ChangeOrgNameMutation,
  DeleteOrgMutation,
  DeleteOrgUserMutation,
  LoadMyProjectsQuery,
  OrgMemberDto,
  OrgPageQuery,
  OrgRole,
  RemoveProjectFromOrgMutation,
} from '$lib/gql/types';
import {getClient, graphql} from '$lib/gql';

import type {OrgTabId} from './OrgTabs.svelte';
import type {PageLoadEvent} from './$types';
import type {UUID} from 'crypto';
import {error} from '@sveltejs/kit';
import {tryMakeNonNullable} from '$lib/util/store';

export type Org = NonNullable<OrgPageQuery['orgById']>;
export type OrgUser = Org['members'][number];
export type User = OrgUser['user'];

export async function load(event: PageLoadEvent) {
  const client = getClient();
  const user = (await event.parent()).user;
  const orgId = event.params.org_id as UUID;
  const orgResult = await client
    .awaitedQueryStore(event.fetch,
      graphql(`
        query orgPage($orgId: UUID!) {
          orgById(orgId: $orgId) {
            id
            createdDate
            updatedDate
            name
            projects {
              id
              isConfidential
              code
              name
              type
              userCount
            }
            members {
              id
              role
              user {
                id
                name
                username
                email
              }
            }
          }
        }
      `),
      { orgId }
    );

  const nonNullableOrg = tryMakeNonNullable(orgResult.orgById);
  if (!nonNullableOrg) {
    error(404);
  }

  event.depends(`org:${orgId}`);

  return {
    org: nonNullableOrg,
    id: orgId,
    user,
  };
}

export type OrgSearchParams = {
  tab: OrgTabId
};

export async function _changeOrgName(input: ChangeOrgNameInput): $OpResult<ChangeOrgNameMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation ChangeOrgName($input: ChangeOrgNameInput!) {
          changeOrgName(input: $input) {
            organization {
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

export async function _deleteOrgUser(orgId: string, userId: string): $OpResult<DeleteOrgUserMutation> {
  const result = await getClient()
    .mutation(
      graphql(`
        mutation deleteOrgUser($input: ChangeOrgMemberRoleInput!) {
          changeOrgMemberRole(input: $input) {
            organization {
              id
            }
          }
        }
      `),
      { input: { orgId, userId, role: null } }
    );
  return result;
}

export async function _addOrgMember(orgId: UUID, emailOrUsername: string, role: OrgRole, canInvite: boolean, projectIds: string[]): $OpResult<AddOrgMemberMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation AddOrgMember($memberInput: SetOrgMemberRoleInput!, $projectsInput: AddProjectsToOrgInput!) {
          setOrgMemberRole(input: $memberInput) {
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
          addProjectsToOrg(input: $projectsInput) {
            organization {
              id
              projects {
                id
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
      { memberInput: { orgId, emailOrUsername, role, canInvite }, projectsInput: { orgId, projectIds } }
    );
  return result;
}

export async function _bulkAddOrgMembers(orgId: UUID, usernames: string[], role: OrgRole): $OpResult<BulkAddOrgMembersMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation BulkAddOrgMembers($input: BulkAddOrgMembersInput!) {
          bulkAddOrgMembers(input: $input) {
            bulkAddOrgMembersResult {
              addedMembers {
                username
                role
              }
              existingMembers {
                username
                role
              }
              notFoundMembers {
                username
                role
              }
            }
            errors {
              __typename
            }
          }
        }
      `),
      { input: { orgId, usernames, role } }
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

export async function _orgMemberById(orgId: UUID, userId: UUID): Promise<OrgMemberDto> {
  //language=GraphQL
  const result = await getClient()
    .query(
      graphql(`
        query OrgMemberById($orgId: UUID!, $userId: UUID!) {
          orgMemberById(orgId: $orgId, userId: $userId) {
            id
            name
            email
            emailVerified
            isAdmin
            createdDate
            username
            locked
            localizationCode
            updatedDate
            lastActive
            canCreateProjects
            createdBy {
              id
              name
            }
          }
        }
      `),
      { orgId, userId },
  );

  if (!result.data?.orgMemberById) error(404);

  return result.data.orgMemberById;
}

export async function _changeOrgMemberRole(orgId: string, userId: string, role: OrgRole): $OpResult<ChangeOrgMemberRoleMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation ChangeOrgMemberRole($input: ChangeOrgMemberRoleInput!) {
          changeOrgMemberRole(input: $input) {
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
      { input: { orgId, userId, role} },
    );
  return result;
}

export async function _deleteOrg(orgId: string): $OpResult<DeleteOrgMutation> {
  const result = await getClient()
    .mutation(
      graphql(`
        mutation deleteOrg($input: DeleteOrgInput!) {
          deleteOrg(input: $input) {
            organization {
              id
              name
            }
          }
        }
      `),
      { input: { orgId } }
    );
  return result;
}

export async function _getMyProjects(): Promise<LoadMyProjectsQuery['myProjects']> {
  const client = getClient();
  //language=GraphQL
  const results = await client.query(graphql(`
        query loadMyProjects {
            myProjects(orderBy: [
                {name: ASC }
            ]) {
                code
                id
                name
                users {
                  id
                  userId
                  role
                }
            }
        }
  `), {});
  return results.data?.myProjects ?? [];
}

export async function _addProjectsToOrg(orgId: UUID, projectIds: string[]): $OpResult<AddProjectsToOrgMutation> {
  //language=GraphQL
  return await getClient()
    .mutation(
      graphql(`
        mutation AddProjectsToOrg($input: AddProjectsToOrgInput!) {
          addProjectsToOrg(input: $input) {
            organization {
              id
              projects {
                id
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
      { input: { orgId, projectIds } }
    );
}
