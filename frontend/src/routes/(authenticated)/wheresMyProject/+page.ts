import { getClient, graphql } from '$lib/gql';

// import type {PageLoadEvent} from './$types';
// import {type LexAuthUser} from '$lib/user';
// import {redirect} from '@sveltejs/kit';
// import {getBoolSearchParam, getSearchParam} from '$lib/util/query-params';
// import {isGuid} from '$lib/util/guid';
import type {
  $OpResult,
  SendFwLiteBetaRequestEmailPayload,
  // SendFwLiteBetaRequestEmailMutation, // TODO Why isn't this one being created?
} from '$lib/gql/types';
// import type {LoadAdminDashboardProjectsQuery, LoadAdminDashboardUsersQuery} from '$lib/gql/types';
// import type {ProjectFilters} from '$lib/components/Projects';
// import {DEFAULT_PAGE_SIZE} from '$lib/components/Paging';
// import type {AdminTabId} from './AdminTabs.svelte';
// import {derived, readable} from 'svelte/store';
// import type {UserType} from '$lib/components/Users/UserFilter.svelte';
import type { UUID } from 'crypto';

export async function _sendFWLiteBetaRequestEmail(userId: UUID, name: string): $OpResult<unknown> { // SendFwLiteBetaRequestEmailPayload
  //language=GraphQL
  const result = await getClient()
    .mutation(
      graphql(`
        mutation SendFWLiteBetaRequestEmail($input: SendFwLiteBetaRequestEmailInput!) {
          sendFWLiteBetaRequestEmail(input: $input)
            user {
              id
              email
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
      { input: { userId, name } }
    )
  return result;
}
