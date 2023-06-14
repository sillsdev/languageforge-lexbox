import type { PageLoad } from './$types';
import type {
  $OpResult,
  ChangeUserAccountData,
  ChangeUserAccountDataInput,
} from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

export async function _changeUserAccountData(input: ChangeUserAccountDataInput): $OpResult<ChangeUserAccountData> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation ChangeUserAccountData($input: ChangeUserAccountDataInput!) {
          changeUserAccountData(input: $input) {
            user {
              userid
              name
              username
              email
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
    )
    .toPromise();
  if (!result.error) void invalidate(`project:${input.projectId}`);
  return result;
}
export const load = (async () => {
    return {};
}) satisfies PageLoad;
