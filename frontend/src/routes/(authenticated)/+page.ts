import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';

export async function load(event: PageLoadEvent) {
  // TODO: Invalidate this load() when user ID changes, so that logging out and logging in fetches a different project list
  // Currently Svelte-Kit is skipping re-running this load if you log out and back in, which results in stale project lists
  const client = getClient();
  //language=GraphQL
  const results = await client.query(graphql(`
        query loadProjects {
            myProjects {
                code
                id
                name
                lastCommit
            }
        }
    `), { }, { fetch: event.fetch });
  if (!results.data) throw new Error('No data returned');
  return {
    projects: results.data.myProjects.map(p => ({
      id: p.id,
      name: p.name,
      code: p.code,
      lastCommit: p.lastCommit,
      userCount: 0, // TODO
    }))
  }
}
