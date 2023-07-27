import type {
  $OpResult,
  ChangeUserAccountDataMutation,
  ChangeUserAccountDataInput,

} from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';
import { goto } from '$app/navigation';
import type { PageLoad } from './$types';
import { browser } from '$app/environment';
import { EmailResult } from '$lib/email';

const EMAIL_RESULTS = Object.values(EmailResult);

export const load = (async ({ url }) => {
  const emailResult = url.searchParams.get('emailResult') as EmailResult | null;
  if (emailResult) {
    if (!EMAIL_RESULTS.includes(emailResult)) throw new Error(`Invalid emailResult: ${emailResult}.`);
    if (browser) await goto(`${url.pathname}`, { replaceState: true });
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
  return result;
}
