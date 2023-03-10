import type {PageLoadEvent} from "./$types";
import type {Project} from "$lib/project";
import {Client, createClient} from "@urql/svelte";
import {getClient} from "$lib/graphQLClient";

export const ssr = false;
export async function load(event: PageLoadEvent): Promise<{ projects: Project[] }> {
    const client = getClient(event);
    //language=GraphQL
    const results = await client.query(`
        query loadProjects {
            projects {
                code
                id
                name
            }
        }
    `, {}).toPromise();
    if (results.error) throw new Error(results.error.message);
    return {
        projects: results.data.projects
    }
}