import type { PageLoadEvent } from "./$types";
import type { Project } from "$lib/project";
import { getClient } from "$lib/graphQLClient";
import { user } from "$lib/user";
import { get } from "svelte/store";
import { graphql } from "$lib/gql";


export async function load(event: PageLoadEvent): Promise<{ projects: Project[] }> {
    const userId = get(user)?.id;
    if (!userId) return {projects: []};
    const client = getClient(event);
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
    `), {userId}).toPromise();
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