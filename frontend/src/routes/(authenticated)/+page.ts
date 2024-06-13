import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';
import {getViewMode} from './shared';

export async function load(event: PageLoadEvent) {
  const client = getClient();
  //language=GraphQL
  const results = await client.awaitedQueryStore(event.fetch, graphql(`
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

            myDraftProjects(orderBy: [
                {code: ASC }
            ]) {
              code
              createdDate
              id
              name
              type
              description
              retentionPolicy
              isConfidential
              projectManagerId
            }
        }
  `), {});

  const projectViewMode = getViewMode(event);

  return {
    projects: results.myProjects,
    draftProjects: results.myDraftProjects,
    projectViewMode,
  }
}
