import type {
  $OpResult,
  ChangeUserAccountBySelfInput,
  ChangeUserAccountBySelfMutation,
} from '$lib/gql/types';
import { getClient, graphql } from '$lib/gql';

import { EmailResult } from '$lib/email';
import type { PageLoadEvent } from './$types';
import { error } from '@sveltejs/kit';
import { tryMakeNonNullable } from '$lib/util/store';

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

  const nonNullableMe = tryMakeNonNullable(userResult.me);
  if (!nonNullableMe) error(404);

  return { emailResult, account: nonNullableMe };
}

export async function _changeUserAccountData(input: ChangeUserAccountBySelfInput): $OpResult<ChangeUserAccountBySelfMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation ChangeUserAccountBySelf($input: ChangeUserAccountBySelfInput!) {
          changeUserAccountBySelf(input: $input) {
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
