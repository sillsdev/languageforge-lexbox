import { getClient, graphql } from '$lib/gql';

import type {PageLoadEvent} from './$types';
import { tryMakeNonNullable } from '$lib/util/store';

export type OrgListSearchParams = {
  search: string,
};

export async function load(event: PageLoadEvent) {
  const client = getClient();
  const orgQueryResult = await client
    .awaitedQueryStore(event.fetch,
      graphql(`
        query orgListPage {
          orgs {
            id
            name
            createdDate
            memberCount
          }
        }
      `),
    {}
    );
  const nonNullableOrgs = tryMakeNonNullable(orgQueryResult.orgs);
  return {orgs: nonNullableOrgs};
}
