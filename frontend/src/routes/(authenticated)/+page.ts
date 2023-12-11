import { getClient, graphql } from '$lib/gql';
import Cookies from 'js-cookie';

import type { PageLoadEvent } from './$types';
import { browser } from '$app/environment';
import { STORAGE_VIEW_MODE_KEY, type ViewMode } from './shared';

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

  let projectViewMode = event.data.projectViewMode;
  if (!projectViewMode && browser) {
    projectViewMode = Cookies.get(STORAGE_VIEW_MODE_KEY) as ViewMode | undefined;
  }

  return {
    projects: results.myProjects,
    projectViewMode,
  }
}
