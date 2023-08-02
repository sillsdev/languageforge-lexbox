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
import { EmailResult } from '$lib/email';

const EMAIL_RESULTS = Object.values(EmailResult);

export const load = (({ url }) => {
  const emailResult = url.searchParams.get('emailResult') as EmailResult | null;
  if (emailResult) {
    if (!EMAIL_RESULTS.includes(emailResult)) throw new Error(`Invalid emailResult: ${emailResult}.`);
    if (browser) void goto(`${url.pathname}`, { replaceState: true });
  }
  return { emailResult };
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
