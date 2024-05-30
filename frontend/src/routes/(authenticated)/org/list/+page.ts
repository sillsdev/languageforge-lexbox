import { getClient, graphql } from '$lib/gql';
import { tryMakeNonNullable } from '$lib/util/store';
import type {PageLoadEvent} from './$types';

export async function load(event: PageLoadEvent) {
  const client = getClient();
  const userIsAdmin = (await event.parent()).user.isAdmin;
  const orgQueryResult = await client
    .awaitedQueryStore(event.fetch,
      graphql(`
        query orgListPage($userIsAdmin: Boolean!) {
          orgs {
            id
            name
            createdDate
            members {
              id
              role
              user {
                id
                name
                ... on User @include(if: $userIsAdmin) {
                  username
                  email
                }
              }
            }
          }
        }
      `),
      { userIsAdmin }
    );
  const nonNullableOrgs = tryMakeNonNullable(orgQueryResult.orgs);
  return {orgs: nonNullableOrgs};
  // return {orgs: orgQueryResult};
}
