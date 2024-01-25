import type {
  $OpResult,
  ChangeUserAccountDataInput,
  ChangeUserAccountDataMutation,
} from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

import { EmailResult } from '$lib/email';
import type { PageLoadEvent } from './$types';
import { error } from '@sveltejs/kit';
import { hasValue } from '$lib/util/store';

const EMAIL_RESULTS = Object.values(EmailResult);

export async function load(event: PageLoadEvent) {
  const emailResult = event.url.searchParams.get('emailResult') as EmailResult | null;
  if (emailResult) {
    if (!EMAIL_RESULTS.includes(emailResult)) throw new Error(`Invalid emailResult: ${emailResult}.`);
  }

  const userResult = await getClient().awaitedQueryStore(event.fetch, graphql(`
    query userPage {
      me {
        id
        name
        email
        locale
      }
    }`), {});

  if (!hasValue(userResult.me)) throw error(404);

  return { emailResult, account: userResult.me };
}

export async function _changeUserAccountData(input: ChangeUserAccountDataInput): $OpResult<ChangeUserAccountDataMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation ChangeUserAccountData($input: ChangeUserAccountDataInput!) {
          changeUserAccountData(input: $input) {
            meDto {
              id
              name
              email
              locale
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
