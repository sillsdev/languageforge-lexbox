import type { PageLoadEvent } from "./$types";
import type { Project } from "$lib/project";
import { getClient } from "$lib/graphQLClient";
import { graphql } from "$lib/gql";
import { browser } from '$app/environment';

export async function load(event: PageLoadEvent): Promise<{ projects: Project[] }> {
  // TODO: Invalidate this load() when user ID changes, so that logging out and logging in fetches a different project list
  // Currently Svelte-Kit is skipping re-running this load if you log out and back in, which results in stale project lists
  const userId = (await event.parent()).userId;
    if (!userId) return {projects: []};
    const client = getClient();
    //language=GraphQL
    const results = await client.query(graphql(`
        query loadProjects($userId: uuid) {
            projects(where: {ProjectUsers: {userId: {_eq: $userId}}}){
                code
                id
                name
                lastCommit
                projectUsersAggregate {
                    aggregate {
                        count
                    }
                }
            }
        }
    `), {userId}, {fetch: event.fetch}).toPromise();
    if (results.error) throw new Error(results.error.message);
    if (!results.data) throw new Error("No data returned");
    return {
        projects: results.data.projects.map(p => ({
            id: p.id,
            name: p.name,
            code: p.code,
            userCount: p.projectUsersAggregate?.aggregate?.count ?? 0,
            lastCommit: p.lastCommit
        }))
    }
}
