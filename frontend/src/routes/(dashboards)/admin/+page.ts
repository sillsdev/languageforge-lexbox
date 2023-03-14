import type {PageLoadEvent} from "./$types";
import {getClient} from "$lib/graphQLClient";
import { graphql } from "$lib/gql";

export async function load(event: PageLoadEvent) {
    const client = getClient(event);
    //language=GraphQL
    const results = await client.query(graphql(`
        query loadAdminProjects {
            projects(orderBy: [
                {lastCommit: ASC_NULLS_FIRST},
                {name: ASC}
            ]) {
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
    `), {}).toPromise();
    if (results.error) throw new Error(results.error.message);
    return {
        projects: results.data?.projects ?? []
    }
}