import type {
  $OpResult,
  AddOrgMemberMutation,
  ChangeOrgMemberRoleMutation,
  ChangeOrgNameInput,
  ChangeOrgNameMutation,
  DeleteOrgMutation,
  DeleteOrgUserMutation,
  OrgPageQuery,
  OrgRole,
} from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

import type { OrgTabId } from './OrgTabs.svelte';
import type { PageLoadEvent } from './$types';
import type { UUID } from 'crypto';
import { error } from '@sveltejs/kit';
import { tryMakeNonNullable } from '$lib/util/store';

export type Org = NonNullable<OrgPageQuery['orgById']>;
export type OrgUser = Org['members'][number];
export type User = OrgUser['user'];

export async function load(event: PageLoadEvent) {
  const client = getClient();
  const user = (await event.parent()).user;
  const userIsAdmin = user.isAdmin;
  const orgId = event.params.org_id as UUID;
  const orgResult = await client
    .awaitedQueryStore(event.fetch,
      graphql(`
        query orgPage($orgId: UUID!, $userIsAdmin: Boolean!) {
          orgById(orgId: $orgId) {
            id
            createdDate
            updatedDate
            name
            projects {
              id
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
                    name
                  }
                }
              }
            }
          }
        }
      `),
      { orgId, userIsAdmin }
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
              members {
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
      { input: { orgId, userId, role: null } }
    );
  return result;
}

export async function _addOrgMember(orgId: UUID, emailOrUsername: string, role: OrgRole): $OpResult<AddOrgMemberMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation AddOrgMember($input: SetOrgMemberRoleInput!) {
          setOrgMemberRole(input: $input) {
            organization {
              id
              members {
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
            }
          }
        }
      `),
      { input: { orgId, emailOrUsername, role} },
    );
  return result;
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
              members {
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
