import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';
import {getViewMode} from './shared';

export async function load(event: PageLoadEvent) {
  const client = getClient();
  //language=GraphQL
  const myProjectPromise = await client.awaitedQueryStore(event.fetch, graphql(`
        query loadProjects {
            myProjects(orderBy: [
                {code: ASC }
            ]) {
                code
                id
                name
                lastCommit
                userCount
                type
                isConfidential
            }
        }
  `), {});

  const myDraftProjectPromise = await client.awaitedQueryStore(event.fetch, graphql(`
        query loadDraftProjects {
            myDraftProjects(orderBy: [
                {code: ASC }
            ]) {
                name
                description
                isConfidential
            }
        }
  `), {});

  const [myProjectResults, myDraftProjectResults] = await Promise.all([myProjectPromise, myDraftProjectPromise]);

  const projectViewMode = getViewMode(event);

  return {
    projects: myProjectResults.myProjects,
    draftProjects: myDraftProjectResults.myDraftProjects,
    projectViewMode,
  }
}
