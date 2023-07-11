import type {
  $OpResult,
  ChangeUserAccountDataMutation,
  ChangeUserAccountDataInput,

} from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';
import { goto, invalidate } from '$app/navigation';
import { refreshJwt } from '$lib/user';
import type { PageLoad } from './$types';
import { browser } from '$app/environment';

export const load = (async ({ url }) => {
  if (url.searchParams.has('verifiedEmail')) {
    if (browser) await goto(`${url.pathname}`, { replaceState: true });
    return { verifiedEmail: true };
  } else if (url.searchParams.has('changedEmail')) {
    if (browser) await goto(`${url.pathname}`, { replaceState: true });
    return { changedEmail: true };
  }
}) satisfies PageLoad

export async function _changeUserAccountData(input: ChangeUserAccountDataInput): $OpResult<ChangeUserAccountDataMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation ChangeUserAccountData($input: ChangeUserAccountDataInput!) {
          changeUserAccountData(input: $input) {
            user {
              id
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
      //invalidates the graphql user cache, but who knows
      { additionalTypenames: ['Users'] },
    );
  if (!result.error) {
    await refreshJwt();
    await invalidate(`user:${input.userId}`);
  }
  return result;
}
