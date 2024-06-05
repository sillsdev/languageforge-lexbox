import type { $OpResult, AddOrgMemberMutation, OrgRole } from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

import type { UUID } from 'crypto';

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
