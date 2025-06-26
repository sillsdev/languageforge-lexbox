import {getClient, graphql} from '$lib/gql';

import type {PageLoadEvent} from './$types';
import {derived} from 'svelte/store';

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
            projectCount
          }
          myOrgs {
            id
          }
        }
      `),
      {}
    );

  const myOrgsMap = derived(orgQueryResult.myOrgs, myOrgs => new Map(myOrgs.map(org => [org.id, org])));
  return {
    orgs: orgQueryResult.orgs,
    myOrgsMap,
  };
}
