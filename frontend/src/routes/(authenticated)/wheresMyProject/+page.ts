import type {$OpResult, SendFwLiteBetaRequestEmailMutation} from '$lib/gql/types';
import {getClient, graphql} from '$lib/gql';

import type {UUID} from 'crypto';

export async function _sendFWLiteBetaRequestEmail(userId: UUID, name: string): $OpResult<SendFwLiteBetaRequestEmailMutation> {
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation SendFWLiteBetaRequestEmail($input: SendFWLiteBetaRequestEmailInput!) {
          sendFWLiteBetaRequestEmail(input: $input) {
            sendFWLiteBetaRequestEmailResult
            errors {
                __typename
              ... on Error {
                message
              }
            }
          }
        }
      `),
      { input: { userId, name } }
    )
  return result;
}
