import { getClient, graphql } from '$lib/gql';

import type { PageLoadEvent } from './$types';

export async function load(event: PageLoadEvent) {
  const client = getClient();

  //language=GraphQL
  const results = await client.query(graphql(`
        query loadAdminDashboard {
            projects(orderBy: [
                {lastCommit: ASC},
                {name: ASC}
            ]) {
                code
                id
                name
                lastCommit
                type
            }
            users(orderBy: {name: ASC}) {
                id
                name
                email
                isAdmin
                createdDate
            }
        }
    `), {}, { fetch: event.fetch });

  return {
    projects: results.data?.projects ?? [],
    users: results.data?.users ?? []
  }
}
