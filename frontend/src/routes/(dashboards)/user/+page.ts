﻿import type {PageLoadEvent} from "./$types";
import type {Project} from "$lib/project";
import {Client, createClient} from "@urql/svelte";

export const ssr = false;
export async function load(event: PageLoadEvent): Promise<{ projects: Project[] }> {
    //todo figure out how to share this.
    const client = createClient({
        url: "/api/graphql",
        fetch: event.fetch,
    });
    //language=GraphQL
    const results = await client.query(`
        query loadProjects {
            myProjects {
                code
                id
                name
            }
        }
    `, {}).toPromise();
    if (results.error) throw new Error(results.error.message);
    return {
        projects: results.data.myProjects
    }
}