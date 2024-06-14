import {getClient, graphql} from '$lib/gql';
import type {CreateOrganizationInput, CreateOrgMutation} from '$lib/gql/generated/graphql';
import type {$OpResult} from '$lib/gql/types';

export async function _createOrg(input: CreateOrganizationInput): $OpResult<CreateOrgMutation> {
  const result = await getClient().mutation(
    graphql(`
        mutation createOrg($input: CreateOrganizationInput!) {
          createOrganization(input: $input) {
                organization {
                  id
                }
                errors {
                    ... on DbError {
                        code
                    }
                }
            }
        }
        `), {input});
  return result;
}
