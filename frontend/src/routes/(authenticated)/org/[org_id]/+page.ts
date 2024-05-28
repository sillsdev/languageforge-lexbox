import type {
  $OpResult,
  AddOrgMemberMutation,
  // ChangeOrgDescriptionInput,
  // ChangeOrgDescriptionMutation,
  ChangeOrgMemberRoleMutation,
  ChangeOrgNameInput,
  ChangeOrgNameMutation,
  DeleteOrgUserMutation,
  // LeaveOrgMutation,
  OrgPageQuery,
  OrgRole,
} from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';
import { error } from '@sveltejs/kit';
import { tryMakeNonNullable } from '$lib/util/store';
import type { UUID } from 'crypto';
import type { OrgTabId } from './OrgTabs.svelte';

export type Org = NonNullable<OrgPageQuery['orgById']>;
export type OrgUser = Org['members'][number];

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
            name
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

export type OrgSearchParams = /*ProjectFilters &*/ { // TODO: Edit once we determine what filters we need
  tab: OrgTabId
};

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

// TODO: Implement
// export async function _bulkAddOrgMembers(input: BulkAddOrgMembersInput): $OpResult<BulkAddOrgMembersMutation> {
//   //language=GraphQL
//   const result = await getClient()
//     .mutation(
//       graphql(`
//         mutation BulkAddOrgMembers($input: BulkAddOrgMembersInput!) {
//           bulkAddOrgMembers(input: $input) {
//             bulkAddOrgMembersResult {
//               addedMembers {
//                 username
//                 role
//               }
//               createdMembers {
//                 username
//                 role
//               }
//               existingMembers {
//                 username
//                 role
//               }
//             }
//             errors {
//               __typename
//               ... on InvalidEmailError {
//                 message
//                 address
//               }
//             }
//           }
//         }
//       `),
//       { input: input }
//     );
//   return result;
// }

export async function _changeOrgMemberRole(orgId: UUID, userId: UUID, role: OrgRole): $OpResult<ChangeOrgMemberRoleMutation> {
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

// TODO: Uncomment if we decide orgs should hvae a description
// export async function _changeOrgDescription(input: ChangeOrgDescriptionInput): $OpResult<ChangeOrgDescriptionMutation> {
//   //language=GraphQL
//   const result = await getClient()
//     .mutation(
//       graphql(`
//         mutation ChangeOrgDescription($input: ChangeOrgDescriptionInput!) {
//           changeOrgDescription(input: $input) {
//             organization {
//               id
//               description
//             }
//             errors {
//               ... on Error {
//                 message
//               }
//             }
//           }
//         }
//       `),
//       { input: input }
//     );
//   return result;
// }

export async function _deleteOrgUser(orgId: UUID, emailOrUsername: string): $OpResult<DeleteOrgUserMutation> {
  const result = await getClient()
    .mutation(
      graphql(`
        mutation deleteOrgUser($input: SetOrgMemberRoleInput!) {
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
          }
        }
      `),
      { input: { orgId, emailOrUsername, role: null } }
    );
  return result;
}

// TODO: Implement as duplicate of _deleteOrgUser? Or uncomment and make separate GQL mutation for this one?
// export async function _leaveOrg(orgId: string): $OpResult<LeaveOrgMutation> {
//   //language=GraphQL
//   const result = await getClient()
//     .mutation(
//       graphql(`
//       mutation LeaveOrg($input: LeaveOrgInput!) {
//         leaveOrg(input: $input) {
//           organization {
//             id
//           }
//           errors {
//             __typename
//           }
//         }
//       }
//     `),
//       { input: { orgId } },
//       //disable invalidate otherwise the page will reload
//       //and the user will be shown that they don't have permission for this org
//       { fetchOptions: { lexboxResponseHandlingConfig: { invalidateUserOnJwtRefresh: false } } }
//     );

//   return result;
// }
