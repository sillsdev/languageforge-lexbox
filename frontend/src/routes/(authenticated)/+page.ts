import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';

export async function load(event: PageLoadEvent) {
  // TODO: Invalidate this load() when user ID changes, so that logging out and logging in fetches a different project list
  // Currently Svelte-Kit is skipping re-running this load if you log out and back in, which results in stale project lists
  const client = getClient();
  //language=GraphQL
  const results = await client.awaitedQueryStore(event.fetch, graphql(`
        query loadProjects {
            myProjects {
                code
                id
                name
                lastCommit
                userCount
                type
            }
        }
  `), {});
  return {
    projects: results.myProjects,
  }
}
