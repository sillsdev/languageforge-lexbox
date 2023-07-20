import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';
import {derived} from 'svelte/store';

export async function load(event: PageLoadEvent) {
  // TODO: Invalidate this load() when user ID changes, so that logging out and logging in fetches a different project list
  // Currently Svelte-Kit is skipping re-running this load if you log out and back in, which results in stale project lists
  const client = getClient();
  //language=GraphQL
  const results = await client.queryStore(event.fetch, graphql(`
        query loadProjects {
            myProjects {
                code
                id
                name
                lastCommit
            }
        }
  `), {});
  return {
    projects: derived(results.myProjects, projects => projects.map(p => ({
      id: p.id,
      name: p.name,
      code: p.code,
      lastCommit: p.lastCommit,
      userCount: 0, // TODO
    })))
  }
}
